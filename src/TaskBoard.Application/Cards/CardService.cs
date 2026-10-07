using Microsoft.EntityFrameworkCore;
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
        var boardId = await db.Columns.Where(c => c.Id == columnId).Select(c => (Guid?)c.BoardId).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Sütun bulunamadı.");
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
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardCreated, card.Id, columnId), ct);

        var assignee = card.AssigneeId is { } id
            ? await db.Users.Where(u => u.Id == id).Select(u => new MemberRefDto(u.Id, u.FullName)).SingleAsync(ct)
            : null;

        return new CardSummaryDto(card.Id, card.Title, card.Position, card.Priority, card.DueDate, assignee, 0,
            card.Description != null);
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
                    .ToList()))
            .SingleAsync(ct);
    }

    public async Task<CardDetailDto> UpdateAsync(Guid cardId, UpdateCardRequest request, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);
        await EnsureAssigneeIsMemberAsync(boardId, request.AssigneeId, ct);

        card.Title = request.Title.Trim();
        card.Description = NormalizeText(request.Description);
        card.DueDate = NormalizeDate(request.DueDate);
        card.Priority = request.Priority;
        card.AssigneeId = request.AssigneeId;

        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardUpdated, cardId, card.ColumnId), ct);
        return await GetAsync(cardId, ct);
    }

    public async Task MoveAsync(Guid cardId, MoveCardRequest request, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);

        // Kart sadece aynı panonun sütunlarına taşınabilir.
        var targetInSameBoard = await db.Columns.AnyAsync(c => c.Id == request.ColumnId && c.BoardId == boardId, ct);
        if (!targetInSameBoard)
            throw new BadRequestException("Hedef sütun bu panoya ait değil.");

        var siblings = await db.Cards
            .Where(c => c.ColumnId == request.ColumnId && c.Id != cardId)
            .OrderBy(c => c.Position).ThenBy(c => c.Id)
            .ToListAsync(ct);

        card.ColumnId = request.ColumnId;
        card.Position = Positioning.PlaceAt(siblings, request.Index, c => c.Position, (c, p) => c.Position = p);

        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardMoved, cardId, request.ColumnId), ct);
    }

    public async Task DeleteAsync(Guid cardId, CancellationToken ct = default)
    {
        var (card, boardId) = await GetForMemberAsync(cardId, ct);
        await db.Cards.Where(c => c.Id == cardId).ExecuteDeleteAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.CardDeleted, cardId, card.ColumnId), ct);
    }

    public async Task<CommentDto> AddCommentAsync(Guid cardId, AddCommentRequest request, CancellationToken ct = default)
    {
        var (_, boardId) = await GetForMemberAsync(cardId, ct);

        var comment = new Comment
        {
            CardId = cardId,
            AuthorId = currentUser.Id,
            Content = request.Content.Trim()
        };

        db.Comments.Add(comment);
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
