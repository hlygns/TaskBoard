using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Application.Columns;

public record CreateColumnRequest([Required, MaxLength(100)] string Name);

public record RenameColumnRequest([Required, MaxLength(100)] string Name);

public record MoveColumnRequest([Range(0, int.MaxValue)] int Index);
