using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Activities;
using TaskBoard.Application.Cards;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Labels;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Boards;

public class BoardService(IAppDbContext db, ICurrentUser currentUser, IBoardNotifier notifier, IBoardCache cache)
    : IBoardService
{
    public async Task<IReadOnlyList<BoardSummaryDto>> GetMyBoardsAsync(CancellationToken ct = default)
    {
        // Select ile projeksiyon: EF sadece gereken kolonları çeker ve MemberCount'u
        // SQL'de COUNT olarak hesaplar; üyeleri belleğe yüklemez.
        return await db.BoardMembers
            .Where(m => m.UserId == currentUser.Id)
            .OrderByDescending(m => m.Board.CreatedAt)
            .Select(m => new BoardSummaryDto(
                m.BoardId,
                m.Board.Name,
                m.Board.Description,
                m.Role,
                m.Board.Members.Count,
                m.Board.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<BoardDetailDto> GetAsync(Guid boardId, CancellationToken ct = default)
    {
        // Yetki kontrolü her zaman veritabanından: cache'te pano olsa bile üye olmayan göremez.
        var myRole = await db.EnsureMemberAsync(boardId, currentUser.Id, ct);

        // Cache'teki kopya herkes için aynı; sadece "benim rolüm" isteği yapana göre değişir.
        if (await cache.GetAsync(boardId, ct) is { } cached)
            return cached with { MyRole = myRole };

        var board = await db.Boards
            .Where(b => b.Id == boardId)
            .Select(b => new BoardDetailDto(
                b.Id,
                b.Name,
                b.Description,
                myRole,
                b.Members
                    .OrderBy(m => m.JoinedAt)
                    .Select(m => new BoardMemberDto(m.UserId, m.User.FullName, m.User.Email, m.Role, m.JoinedAt))
                    .ToList(),
                // Pano ekranı tek istekle açılır: sütunlar ve içindeki kart özetleri birlikte gelir.
                // Eşit sıra numarasında (iki kişi aynı anda aynı yere bırakırsa) Id ile kararlı sıralama.
                b.Columns
                    .OrderBy(c => c.Position).ThenBy(c => c.Id)
                    .Select(c => new ColumnDto(
                        c.Id,
                        c.Name,
                        c.Position,
                        // Arşivlenen kartlar panoda görünmez.
                        c.Cards
                            .Where(card => card.ArchivedAt == null)
                            .OrderBy(card => card.Position).ThenBy(card => card.Id)
                            .Select(card => new CardSummaryDto(
                                card.Id,
                                card.Title,
                                card.Position,
                                card.Priority,
                                card.DueDate,
                                card.Assignee == null ? null : new MemberRefDto(card.Assignee.Id, card.Assignee.FullName),
                                card.Comments.Count,
                                card.Description != null,
                                card.CompletedAt != null,
                                card.ChecklistItems.Count(i => i.IsDone),
                                card.ChecklistItems.Count,
                                card.Labels.Select(l => l.LabelId).ToList()))
                            .ToList()))
                    .ToList(),
                b.Labels
                    .OrderBy(l => l.Name)
                    .Select(l => new LabelDto(l.Id, l.Name, l.Color))
                    .ToList()))
            .SingleAsync(ct);

        await cache.SetAsync(boardId, board, ct);
        return board;
    }

    public async Task<IReadOnlyList<ArchivedCardDto>> GetArchivedCardsAsync(Guid boardId, CancellationToken ct = default)
    {
        await db.EnsureMemberAsync(boardId, currentUser.Id, ct);

        return await db.Cards
            .Where(c => c.Column.BoardId == boardId && c.ArchivedAt != null)
            .OrderByDescending(c => c.ArchivedAt)
            .Select(c => new ArchivedCardDto(c.Id, c.Title, c.Column.Name, c.ArchivedAt!.Value))
            .ToListAsync(ct);
    }

    public async Task<BoardDetailDto> CreateAsync(CreateBoardRequest request, CancellationToken ct = default)
    {
        var template = BoardTemplates.Find(request.Template);
        var board = new Board
        {
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description)
        };

        // Oluşturan kişi otomatik olarak sahip olur.
        board.Members.Add(new BoardMember { UserId = currentUser.Id, Role = BoardRole.Owner });

        for (var i = 0; i < template.Columns.Count; i++)
            board.Columns.Add(new Column { Name = template.Columns[i], Position = i + 1 });

        foreach (var label in template.Labels)
            board.Labels.Add(new Label { Name = label.Name, Color = label.Color });

        // Pano, üyelik, sütunlar ve etiketler tek SaveChanges ile tek transaction'da yazılır.
        db.Boards.Add(board);
        db.LogActivity(board.Id, currentUser.Id, ActivityType.BoardCreated, board.Id, new { boardName = board.Name });
        await db.SaveChangesAsync(ct);

        return await GetAsync(board.Id, ct);
    }

    public async Task UpdateAsync(Guid boardId, UpdateBoardRequest request, CancellationToken ct = default)
    {
        await db.EnsureOwnerAsync(boardId, currentUser.Id, ct);

        var board = await db.Boards.SingleAsync(b => b.Id == boardId, ct);
        board.Name = request.Name.Trim();
        board.Description = NormalizeDescription(request.Description);

        db.LogActivity(boardId, currentUser.Id, ActivityType.BoardUpdated, boardId, new { boardName = board.Name });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.BoardUpdated), ct);
    }

    public async Task DeleteAsync(Guid boardId, CancellationToken ct = default)
    {
        await db.EnsureOwnerAsync(boardId, currentUser.Id, ct);

        // Sütunlar, kartlar, yorumlar, üyelikler ve davetler veritabanındaki
        // ON DELETE CASCADE kuralıyla birlikte silinir.
        await db.Boards.Where(b => b.Id == boardId).ExecuteDeleteAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.BoardDeleted), ct);
    }

    public async Task RemoveMemberAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        var myRole = await db.EnsureMemberAsync(boardId, currentUser.Id, ct);
        var isSelf = userId == currentUser.Id;

        if (isSelf && myRole == BoardRole.Owner)
            throw new ConflictException("Pano sahibi panodan ayrılamaz. Panoyu silebilirsiniz.");

        if (!isSelf && myRole != BoardRole.Owner)
            throw new ForbiddenException("Üye çıkarmak için pano sahibi olmalısınız.");

        var membership = await db.BoardMembers
            .Include(m => m.User)
            .SingleOrDefaultAsync(m => m.BoardId == boardId && m.UserId == userId, ct)
            ?? throw new NotFoundException("Üye bulunamadı.");

        // Çıkan kişinin bu panodaki kart atamaları kaldırılır.
        await db.Cards
            .Where(c => c.Column.BoardId == boardId && c.AssigneeId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.AssigneeId, (Guid?)null), ct);

        db.BoardMembers.Remove(membership);
        db.LogActivity(boardId, currentUser.Id, isSelf ? ActivityType.MemberLeft : ActivityType.MemberRemoved, userId,
            new { memberName = membership.User.FullName });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.MembersChanged), ct);
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
