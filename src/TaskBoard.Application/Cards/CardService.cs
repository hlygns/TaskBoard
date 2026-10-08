using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Activities;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Cards;

public class CardService(IAppDbContext db, ICurrentUser currentUser, IBoardNotifier notifier) : ICardService
{
    public async Task<CardSummaryDto> CreateAsync(Guid columnId, CreateCardRequest request, CancellationToken ct = default)
    {
        var column = await db.Columns.Where(c => c.Id == columnId).Select(c => new { c.BoardId, c.Name }).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Sütun bulunamadı.");
        var boardId = column.BoardId;
        await db.EnsureMemberAsync(boardId, currentUser.Id, ct);
        await EnsureAssigneeIsMemberAsync(boardId, request.AssigneeId, ct);

        // Yeni kart sütunun en altına eklenir.
        var last = await db.Cards.Where(c => c.ColumnId == columnId).MaxAsync(c => (double?)c.Position, ct);
        var card = new Card
        {
            ColumnId = columnId,
            Title = request.Title.Trim(),
            Description = NormalizeText(request.Description),
            DueDate = NormalizeDate(request.DueDate),
            Priority = request.Priority ?? CardPriority.Medium,
            AssigneeId = request.AssigneeId,
            Position = Positioning.After(last)
        };

        db.Cards.Add(card);
        db.LogActivity(boardId, currentUser.Id, ActivityType.CardCreated, card.Id,
            new { cardTitle = card.Title, columnName = column.Name });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardCreated, card.Id, columnId), ct);

        var assignee = card.AssigneeId is { } id
            ? await db.Users.Where(u => u.Id == id).Select(u => new MemberRefDto(u.Id, u.FullName)).SingleAsync(ct)
            : null;

        return new CardSummaryDto(card.Id, card.Title, card.Position, card.Priority, card.DueDate, assignee, 0,
            card.Description != null, IsCompleted: false, ChecklistDone: 0, ChecklistTotal: 0, LabelIds: []);
    }

    public async Task<CardDetailDto> GetAsync(Guid cardId, CancellationToken ct = default)
    {
        await GetForMemberAsync(cardId, ct);

        return await db.Cards
            .Where(c => c.Id == cardId)
            .Select(c => new CardDetailDto(
                c.Id,
                c.ColumnId,
                c.Column.Name,
                c.Title,
                c.Description,
                c.Priority,
                c.DueDate,
                c.Assignee == null ? null : new MemberRefDto(c.Assignee.Id, c.Assignee.FullName),
                c.CreatedAt,
                c.UpdatedAt,
                c.Comments
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new CommentDto(m.Id, m.Content, new MemberRefDto(m.Author.Id, m.Author.FullName), m.CreatedAt))
                    .ToList(),
                c.CompletedAt,
                c.ArchivedAt,
                c.Labels.Select(l => l.LabelId).ToList(),
                c.ChecklistItems
                    .OrderBy(i => i.Position)
                    .Select(i => new ChecklistItemDto(i.Id, i.Text, i.IsDone))
                    .ToList()))
            .SingleAsync(ct);
    }

    public async Task<CardDetailDto> UpdateAsync(Guid cardId, UpdateCardRequest request, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);
        await EnsureAssigneeIsMemberAsync(boardId, request.AssigneeId, ct);

        var title = request.Title.Trim();
        var description = NormalizeText(request.Description);
        var dueDate = NormalizeDate(request.DueDate);
        var assigneeChanged = card.AssigneeId != request.AssigneeId;
        var detailsChanged = card.Title != title || card.Description != description
            || card.DueDate != dueDate || card.Priority != request.Priority;

        // Son tarih değiştiyse yeni tarih için tekrar hatırlatma gönderilebilsin.
        if (card.DueDate != dueDate)
            card.DueReminderSentAt = null;

        card.Title = title;
        card.Description = description;
        card.DueDate = dueDate;
        card.Priority = request.Priority;
        card.AssigneeId = request.AssigneeId;

        if (detailsChanged)
            db.LogActivity(boardId, currentUser.Id, ActivityType.CardUpdated, cardId, new { cardTitle = title });

        if (assigneeChanged)
        {
            var assigneeName = request.AssigneeId is { } assigneeId
                ? await db.Users.Where(u => u.Id == assigneeId).Select(u => u.FullName).SingleAsync(ct)
                : null;
            db.LogActivity(boardId, currentUser.Id, ActivityType.CardAssigned, cardId,
                new { cardTitle = title, assigneeName });
        }

        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardUpdated, cardId, card.ColumnId), ct);
        return await GetAsync(cardId, ct);
    }

    public async Task MoveAsync(Guid cardId, MoveCardRequest request, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);

        // Kart sadece aynı panonun sütunlarına taşınabilir.
        var targetColumnName = await db.Columns
            .Where(c => c.Id == request.ColumnId && c.BoardId == boardId)
            .Select(c => c.Name)
            .SingleOrDefaultAsync(ct)
            ?? throw new BadRequestException("Hedef sütun bu panoya ait değil.");
        var fromColumnName = card.Column.Name;
        var columnChanged = card.ColumnId != request.ColumnId;

        var siblings = await db.Cards
            .Where(c => c.ColumnId == request.ColumnId && c.Id != cardId && c.ArchivedAt == null)
            .OrderBy(c => c.Position).ThenBy(c => c.Id)
            .ToListAsync(ct);

        card.ColumnId = request.ColumnId;
        card.Position = Positioning.PlaceAt(siblings, request.Index, c => c.Position, (c, p) => c.Position = p);

        // Aynı sütun içinde sıra değiştirmek geçmişte gürültü olur; sadece sütun değişince kaydediyoruz.
        if (columnChanged)
            db.LogActivity(boardId, currentUser.Id, ActivityType.CardMoved, cardId,
                new { cardTitle = card.Title, fromColumn = fromColumnName, toColumn = targetColumnName });

        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardMoved, cardId, request.ColumnId), ct);
    }

    public async Task DeleteAsync(Guid cardId, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);

        // Silme ve aktivite kaydı aynı SaveChanges'ta. Yorumlar veritabanında ON DELETE CASCADE ile gider.
        db.Cards.Remove(card);
        db.LogActivity(boardId, currentUser.Id, ActivityType.CardDeleted, cardId,
            new { cardTitle = card.Title, columnName = card.Column.Name });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardDeleted, cardId, card.ColumnId), ct);
    }

    public async Task SetCompletedAsync(Guid cardId, bool completed, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);
        if ((card.CompletedAt != null) == completed) return;

        card.CompletedAt = completed ? DateTime.UtcNow : null;
        db.LogActivity(boardId, currentUser.Id, completed ? ActivityType.CardCompleted : ActivityType.CardReopened, cardId,
            new { cardTitle = card.Title });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardUpdated, cardId, card.ColumnId), ct);
    }

    public async Task SetArchivedAsync(Guid cardId, bool archived, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);
        if ((card.ArchivedAt != null) == archived) return;

        if (archived)
        {
            card.ArchivedAt = DateTime.UtcNow;
        }
        else
        {
            // Geri alınan kart sütunun en altına döner; arşivdeyken araya giren kartlarla karışmasın.
            var last = await db.Cards
                .Where(c => c.ColumnId == card.ColumnId && c.ArchivedAt == null)
                .MaxAsync(c => (double?)c.Position, ct);
            card.ArchivedAt = null;
            card.Position = Positioning.After(last);
        }

        db.LogActivity(boardId, currentUser.Id, archived ? ActivityType.CardArchived : ActivityType.CardRestored, cardId,
            new { cardTitle = card.Title, columnName = card.Column.Name });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId,
            new BoardEvent(archived ? BoardEvent.CardDeleted : BoardEvent.CardCreated, cardId, card.ColumnId), ct);
    }

    public async Task SetLabelsAsync(Guid cardId, IReadOnlyList<Guid> labelIds, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);
        var wanted = labelIds.Distinct().ToList();

        // Sadece bu panonun etiketleri takılabilir.
        var validCount = await db.Labels.CountAsync(l => l.BoardId == boardId && wanted.Contains(l.Id), ct);
        if (validCount != wanted.Count)
            throw new BadRequestException("Etiketlerden biri bu panoya ait değil.");

        var current = await db.CardLabels.Where(cl => cl.CardId == cardId).ToListAsync(ct);
        db.CardLabels.RemoveRange(current.Where(cl => !wanted.Contains(cl.LabelId)));
        db.CardLabels.AddRange(wanted
            .Where(id => current.All(cl => cl.LabelId != id))
            .Select(id => new CardLabel { CardId = cardId, LabelId = id }));

        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardUpdated, cardId, card.ColumnId), ct);
    }

    public async Task<ChecklistItemDto> AddChecklistItemAsync(Guid cardId, AddChecklistItemRequest request, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);

        var last = await db.ChecklistItems.Where(i => i.CardId == cardId).MaxAsync(i => (double?)i.Position, ct);
        var item = new ChecklistItem { CardId = cardId, Text = request.Text.Trim(), Position = Positioning.After(last) };

        db.ChecklistItems.Add(item);
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardUpdated, cardId, card.ColumnId), ct);

        return new ChecklistItemDto(item.Id, item.Text, item.IsDone);
    }

    public async Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid itemId, UpdateChecklistItemRequest request, CancellationToken ct = default)
    {
        var (item, card, boardId) = await GetChecklistItemForMemberAsync(itemId, ct);

        if (request.Text is { } text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new BadRequestException("Alt görev metni boş olamaz.");
            item.Text = text.Trim();
        }
        if (request.IsDone is { } isDone)
            item.IsDone = isDone;

        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardUpdated, card.Id, card.ColumnId), ct);

        return new ChecklistItemDto(item.Id, item.Text, item.IsDone);
    }

    public async Task DeleteChecklistItemAsync(Guid itemId, CancellationToken ct = default)
    {
        var (item, card, boardId) = await GetChecklistItemForMemberAsync(itemId, ct);

        db.ChecklistItems.Remove(item);
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardUpdated, card.Id, card.ColumnId), ct);
    }

    private async Task<(ChecklistItem Item, Card Card, Guid BoardId)> GetChecklistItemForMemberAsync(Guid itemId, CancellationToken ct)
    {
        var item = await db.ChecklistItems
            .Include(i => i.Card).ThenInclude(c => c.Column)
            .SingleOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new NotFoundException("Alt görev bulunamadı.");

        await db.EnsureMemberAsync(item.Card.Column.BoardId, currentUser.Id, ct);
        return (item, item.Card, item.Card.Column.BoardId);
    }

    public async Task<CommentDto> AddCommentAsync(Guid cardId, AddCommentRequest request, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);

        var comment = new Comment
        {
            CardId = cardId,
            AuthorId = currentUser.Id,
            Content = request.Content.Trim()
        };

        db.Comments.Add(comment);
        db.LogActivity(boardId, currentUser.Id, ActivityType.CommentAdded, cardId,
            new { cardTitle = card.Title, excerpt = Excerpt(comment.Content) });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CommentAdded, cardId), ct);

        var author = await db.Users.Where(u => u.Id == currentUser.Id)
            .Select(u => new MemberRefDto(u.Id, u.FullName))
            .SingleAsync(ct);

        return new CommentDto(comment.Id, comment.Content, author, comment.CreatedAt);
    }

    public async Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default)
    {
        var comment = await db.Comments
            .Where(c => c.Id == commentId)
            .Select(c => new { c.AuthorId, c.CardId, c.Card.Column.BoardId })
            .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Yorum bulunamadı.");

        var role = await db.EnsureMemberAsync(comment.BoardId, currentUser.Id, ct);

        // Yorumu yazan kişi ya da pano sahibi silebilir.
        if (comment.AuthorId != currentUser.Id && role != BoardRole.Owner)
            throw new ForbiddenException("Sadece kendi yorumlarınızı silebilirsiniz.");

        await db.Comments.Where(c => c.Id == commentId).ExecuteDeleteAsync(ct);
        await notifier.NotifyAsync(comment.BoardId, new BoardEvent(BoardEvent.CommentDeleted, comment.CardId), ct);
    }

    private async Task<(Card Card, Guid BoardId)> GetForMemberAsync(Guid cardId, CancellationToken ct)
    {
        var card = await db.Cards.Include(c => c.Column).SingleOrDefaultAsync(c => c.Id == cardId, ct)
            ?? throw new NotFoundException("Kart bulunamadı.");

        await db.EnsureMemberAsync(card.Column.BoardId, currentUser.Id, ct);
        return (card, card.Column.BoardId);
    }

    // Karta sadece panonun üyesi atanabilir.
    private async Task EnsureAssigneeIsMemberAsync(Guid boardId, Guid? assigneeId, CancellationToken ct)
    {
        if (assigneeId is not { } id) return;

        var isMember = await db.BoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == id, ct);
        if (!isMember)
            throw new BadRequestException("Atanan kişi bu panonun üyesi değil.");
    }

    private static string Excerpt(string text) => text.Length <= 80 ? text : text[..80] + "…";

    private static string? NormalizeText(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    // PostgreSQL'in "timestamp with time zone" kolonu UTC ister. İstemci saat dilimi belirtmeden
    // gönderirse ("2026-10-10T00:00:00") onu UTC kabul ediyoruz.
    private static DateTime? NormalizeDate(DateTime? date) => date switch
    {
        null => null,
        { Kind: DateTimeKind.Utc } d => d,
        { Kind: DateTimeKind.Local } d => d.ToUniversalTime(),
        { } d => DateTime.SpecifyKind(d, DateTimeKind.Utc)
    };
}
