using TaskBoard.Application.Boards;

namespace TaskBoard.Application.Columns;

public interface IColumnService
{
    Task<ColumnDto> CreateAsync(Guid boardId, CreateColumnRequest request, CancellationToken ct = default);
    Task RenameAsync(Guid columnId, RenameColumnRequest request, CancellationToken ct = default);
    Task MoveAsync(Guid columnId, MoveColumnRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid columnId, CancellationToken ct = default);
}
