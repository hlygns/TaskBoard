namespace TaskBoard.Application.Notifications;

// Mail şablonlarına giden veriler. HTML'i Infrastructure'daki şablonlar üretir.

public record EmailCardItem(Guid BoardId, string BoardName, string ColumnName, string CardTitle, DateTime DueDate);

public record DueDateReminderEmail(string ToEmail, string ToName, IReadOnlyList<EmailCardItem> Cards);

public record DailyDigestEmail(
    string ToEmail,
    string ToName,
    IReadOnlyList<EmailCardItem> Overdue,
    IReadOnlyList<EmailCardItem> DueSoon,
    int ActivityCountLast24h);
