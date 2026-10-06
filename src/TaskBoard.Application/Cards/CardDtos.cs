using System.ComponentModel.DataAnnotations;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Cards;

public record CreateCardRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(5000)] string? Description = null,
    DateTime? DueDate = null,
    CardPriority? Priority = null,
    Guid? AssigneeId = null);

// Tam güncelleme (PUT): gönderilmeyen alan boş kabul edilir.
public record UpdateCardRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(5000)] string? Description,
    DateTime? DueDate,
    CardPriority Priority,
    Guid? AssigneeId);

// Kartı hangi sütunun kaçıncı sırasına bıraktığımız.
public record MoveCardRequest(Guid ColumnId, [Range(0, int.MaxValue)] int Index);

public record AddCommentRequest([Required, MaxLength(2000)] string Content);

public record MemberRefDto(Guid UserId, string FullName);

// Pano ekranındaki kart yüzü: sadece listede görünen bilgiler.
public record CardSummaryDto(
    Guid Id,
    string Title,
    double Position,
    CardPriority Priority,
    DateTime? DueDate,
    MemberRefDto? Assignee,
    int CommentCount,
    bool HasDescription);

public record CommentDto(Guid Id, string Content, MemberRefDto Author, DateTime CreatedAt);

// Karta tıklayınca açılan detay.
public record CardDetailDto(
    Guid Id,
    Guid ColumnId,
    string ColumnName,
    string Title,
    string? Description,
    CardPriority Priority,
    DateTime? DueDate,
    MemberRefDto? Assignee,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<CommentDto> Comments);
