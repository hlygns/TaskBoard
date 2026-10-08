using System.ComponentModel.DataAnnotations;
using TaskBoard.Application.Cards;
using TaskBoard.Application.Labels;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Boards;

public record CreateBoardRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(1000)] string? Description,
    // Hazır sütun/etiket seti (bkz. BoardTemplates); boşsa "basic".
    string? Template = null);

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

public record ColumnDto(Guid Id, string Name, double Position, IReadOnlyList<CardSummaryDto> Cards);

public record BoardDetailDto(
    Guid Id,
    string Name,
    string? Description,
    BoardRole MyRole,
    IReadOnlyList<BoardMemberDto> Members,
    IReadOnlyList<ColumnDto> Columns,
    IReadOnlyList<LabelDto> Labels);
