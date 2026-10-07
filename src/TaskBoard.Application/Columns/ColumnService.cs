using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Activities;
using TaskBoard.Application.Boards;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Columns;

// Sütun işlemlerini panonun her üyesi yapabilir; sadece pano silme ve davet sahibe özel.
public class ColumnService(IAppDbContext db, ICurrentUser currentUser, IBoardNotifier notifier) : IColumnService
{
    public async Task<ColumnDto> CreateAsync(Guid boardId, CreateColumnRequest request, CancellationToken ct = default)
    {
        await db.EnsureMemberAsync(boardId, currentUser.Id, ct);

        var last = await db.Columns.Where(c => c.BoardId == boardId).MaxAsync(c => (double?)c.Position, ct);
        var column = new Column
        {
            BoardId = boardId,
            Name = request.Name.Trim(),
            Position = Positioning.After(last)
        };

        db.Columns.Add(column);
        db.LogActivity(boardId, currentUser.Id, ActivityType.ColumnCreated, column.Id, new { columnName = column.Name });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(boardId, new BoardEvent(BoardEvent.ColumnCreated, ColumnId: column.Id), ct);

        return new ColumnDto(column.Id, column.Name, column.Position, []);
    }

    public async Task RenameAsync(Guid columnId, RenameColumnRequest request, CancellationToken ct = default)
    {
        var column = await GetForMemberAsync(columnId, ct);
        var oldName = column.Name;
        column.Name = request.Name.Trim();
        if (column.Name != oldName)
            db.LogActivity(column.BoardId, currentUser.Id, ActivityType.ColumnRenamed, columnId,
                new { oldName, newName = column.Name });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(column.BoardId, new BoardEvent(BoardEvent.ColumnUpdated, ColumnId: columnId), ct);
    }

    public async Task MoveAsync(Guid columnId, MoveColumnRequest request, CancellationToken ct = default)
    {
        var column = await GetForMemberAsync(columnId, ct);

        var siblings = await db.Columns
            .Where(c => c.BoardId == column.BoardId && c.Id != columnId)
            .OrderBy(c => c.Position).ThenBy(c => c.Id)
            .ToListAsync(ct);

        column.Position = Positioning.PlaceAt(siblings, request.Index, c => c.Position, (c, p) => c.Position = p);
        db.LogActivity(column.BoardId, currentUser.Id, ActivityType.ColumnMoved, columnId, new { columnName = column.Name });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(column.BoardId, new BoardEvent(BoardEvent.ColumnMoved, ColumnId: columnId), ct);
    }

    public async Task DeleteAsync(Guid columnId, CancellationToken ct = default)
    {
        var column = await GetForMemberAsync(columnId, ct);
        var cardCount = await db.Cards.CountAsync(c => c.ColumnId == columnId, ct);

        // Silme ve aktivite kaydı aynı SaveChanges'ta. İçindeki kartlar ve yorumlar ON DELETE CASCADE ile gider.
        db.Columns.Remove(column);
        db.LogActivity(column.BoardId, currentUser.Id, ActivityType.ColumnDeleted, columnId,
            new { columnName = column.Name, cardCount = cardCount.ToString() });
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(column.BoardId, new BoardEvent(BoardEvent.ColumnDeleted, ColumnId: columnId), ct);
    }

    private async Task<Column> GetForMemberAsync(Guid columnId, CancellationToken ct)
    {
        var column = await db.Columns.SingleOrDefaultAsync(c => c.Id == columnId, ct)
            ?? throw new NotFoundException("Sütun bulunamadı.");

        await db.EnsureMemberAsync(column.BoardId, currentUser.Id, ct);
        return column;
    }
}
