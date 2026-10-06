namespace TaskBoard.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7(); //v4 ler rastgele dağıldığı için bunu tercih ettik(v7 nin başında zaman dalgası olsuüu için sıralı)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
