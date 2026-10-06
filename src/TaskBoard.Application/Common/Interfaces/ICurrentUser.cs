namespace TaskBoard.Application.Common.Interfaces;

// İsteği yapan kullanıcının kimliği. API katmanı bunu JWT'deki "sub" claim'inden okur.
public interface ICurrentUser
{
    Guid Id { get; }
}
