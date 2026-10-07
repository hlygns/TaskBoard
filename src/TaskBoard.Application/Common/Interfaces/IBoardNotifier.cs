namespace TaskBoard.Application.Common.Interfaces;

// Panoda bir şey değiştiğinde o panoya bakan herkese haber verir.
// Application katmanı bunun SignalR ile yapıldığını bilmez; uygulaması API katmanında.
public interface IBoardNotifier
{
    Task NotifyAsync(Guid boardId, BoardEvent boardEvent, CancellationToken ct = default);
}

// İstemcinin neyin değiştiğini anlaması için yeterli, hassas veri içermeyen küçük bir mesaj.
// İstemci ayrıntıyı API'den kendi yetkisiyle çeker.
public record BoardEvent(string Type, Guid? CardId = null, Guid? ColumnId = null)
{
    public const string BoardUpdated = nameof(BoardUpdated);
    public const string BoardDeleted = nameof(BoardDeleted);
    public const string MembersChanged = nameof(MembersChanged);
    public const string ColumnCreated = nameof(ColumnCreated);
    public const string ColumnUpdated = nameof(ColumnUpdated);
    public const string ColumnMoved = nameof(ColumnMoved);
    public const string ColumnDeleted = nameof(ColumnDeleted);
    public const string CardCreated = nameof(CardCreated);
    public const string CardUpdated = nameof(CardUpdated);
    public const string CardMoved = nameof(CardMoved);
    public const string CardDeleted = nameof(CardDeleted);
    public const string CommentAdded = nameof(CommentAdded);
    public const string CommentDeleted = nameof(CommentDeleted);
}
