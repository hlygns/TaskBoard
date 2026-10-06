using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Boards;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Columns;

// Sütun işlemlerini panonun her üyesi yapabilir; sadece pano silme ve davet sahibe özel.
public class ColumnService(IAppDbContext db, ICurrentUser currentUser) : IColumnService
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
        await db.SaveChangesAsync(ct);

        return new ColumnDto(column.Id, column.Name, column.Position, []);
    }

    public async Task RenameAsync(Guid columnId, RenameColumnRequest request, CancellationToken ct = default)
    {
        var column = await GetForMemberAsync(columnId, ct);
        column.Name = request.Name.Trim();
        await db.SaveChangesAsync(ct);
    }

    public async Task MoveAsync(Guid columnId, MoveColumnRequest request, CancellationToken ct = default)
    {
        var column = await GetForMemberAsync(columnId, ct);

        var siblings = await db.Columns
            .Where(c => c.BoardId == column.BoardId && c.Id != columnId)
            .OrderBy(c => c.Position).ThenBy(c => c.Id)
            .ToListAsync(ct);

        column.Position = Positioning.PlaceAt(siblings, request.Index, c => c.Position, (c, p) => c.Position = p);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid columnId, CancellationToken ct = default)
    {
        await GetForMemberAsync(columnId, ct);

        // İçindeki kartlar ve yorumlar ON DELETE CASCADE ile silinir.
        await db.Columns.Where(c => c.Id == columnId).ExecuteDeleteAsync(ct);
    }

    private async Task<Column> GetForMemberAsync(Guid columnId, CancellationToken ct)
    {
        var column = await db.Columns.SingleOrDefaultAsync(c => c.Id == columnId, ct)
            ?? throw new NotFoundException("Sütun bulunamadı.");

        await db.EnsureMemberAsync(column.BoardId, currentUser.Id, ct);
        return column;
    }
}
