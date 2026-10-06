using System.ComponentModel.DataAnnotations;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Boards;

public record CreateBoardRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description);

public record UpdateBoardRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description);

public record BoardSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    BoardRole MyRole,
    int MemberCount,
    DateTime CreatedAt);

public record BoardMemberDto(Guid UserId, string FullName, string Email, BoardRole Role, DateTime JoinedAt);

public record ColumnDto(Guid Id, string Name, double Position);

public record BoardDetailDto(
    Guid Id,
    string Name,
    string? Description,
    BoardRole MyRole,
    IReadOnlyList<BoardMemberDto> Members,
    IReadOnlyList<ColumnDto> Columns);
