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

public record SetCardCompletedRequest(bool Completed);

public record SetCardArchivedRequest(bool Archived);

// Kartın etiketlerinin tamamı (eklenenler + kalanlar); listede olmayanlar çıkarılır.
public record SetCardLabelsRequest([Required] IReadOnlyList<Guid> LabelIds);

public record AddCommentRequest([Required, MaxLength(2000)] string Content);

public record AddChecklistItemRequest([Required, MaxLength(300)] string Text);

// Kısmi güncelleme: sadece gönderilen alan değişir (ör. sadece IsDone).
public record UpdateChecklistItemRequest([MaxLength(300)] string? Text = null, bool? IsDone = null);

public record MemberRefDto(Guid UserId, string FullName);

public record ChecklistItemDto(Guid Id, string Text, bool IsDone);

// Pano ekranındaki kart yüzü: sadece listede görünen bilgiler.
public record CardSummaryDto(
    Guid Id,
    string Title,
    double Position,
    CardPriority Priority,
    DateTime? DueDate,
    MemberRefDto? Assignee,
    int CommentCount,
    bool HasDescription,
    bool IsCompleted,
    int ChecklistDone,
    int ChecklistTotal,
    IReadOnlyList<Guid> LabelIds);

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
    IReadOnlyList<CommentDto> Comments,
    DateTime? CompletedAt,
    DateTime? ArchivedAt,
    IReadOnlyList<Guid> LabelIds,
    IReadOnlyList<ChecklistItemDto> Checklist);

public record ArchivedCardDto(Guid Id, string Title, string ColumnName, DateTime ArchivedAt);
