using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Boards;

public class BoardService(IAppDbContext db, ICurrentUser currentUser) : IBoardService
{
    private static readonly string[] DefaultColumns = ["Yapılacak", "Yapılıyor", "Bitti"];

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
        var myRole = await db.EnsureMemberAsync(boardId, currentUser.Id, ct);

        return await db.Boards
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
                b.Columns
                    .OrderBy(c => c.Position)
                    .Select(c => new ColumnDto(c.Id, c.Name, c.Position))
                    .ToList()))
            .SingleAsync(ct);
    }

    public async Task<BoardDetailDto> CreateAsync(CreateBoardRequest request, CancellationToken ct = default)
    {
        var board = new Board
        {
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description)
        };

        // Oluşturan kişi otomatik olarak sahip olur.
        board.Members.Add(new BoardMember { UserId = currentUser.Id, Role = BoardRole.Owner });

        for (var i = 0; i < DefaultColumns.Length; i++)
            board.Columns.Add(new Column { Name = DefaultColumns[i], Position = i + 1 });

        // Pano, üyelik ve sütunlar tek SaveChanges ile tek transaction'da yazılır.
        db.Boards.Add(board);
        await db.SaveChangesAsync(ct);

        return await GetAsync(board.Id, ct);
    }

    public async Task UpdateAsync(Guid boardId, UpdateBoardRequest request, CancellationToken ct = default)
    {
        await db.EnsureOwnerAsync(boardId, currentUser.Id, ct);

        var board = await db.Boards.SingleAsync(b => b.Id == boardId, ct);
        board.Name = request.Name.Trim();
        board.Description = NormalizeDescription(request.Description);

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid boardId, CancellationToken ct = default)
    {
        await db.EnsureOwnerAsync(boardId, currentUser.Id, ct);

        // Sütunlar, kartlar, yorumlar, üyelikler ve davetler veritabanındaki
        // ON DELETE CASCADE kuralıyla birlikte silinir.
        await db.Boards.Where(b => b.Id == boardId).ExecuteDeleteAsync(ct);
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
            .SingleOrDefaultAsync(m => m.BoardId == boardId && m.UserId == userId, ct)
            ?? throw new NotFoundException("Üye bulunamadı.");

        // Çıkan kişinin bu panodaki kart atamaları kaldırılır.
        await db.Cards
            .Where(c => c.Column.BoardId == boardId && c.AssigneeId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.AssigneeId, (Guid?)null), ct);

        db.BoardMembers.Remove(membership);
        await db.SaveChangesAsync(ct);
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
