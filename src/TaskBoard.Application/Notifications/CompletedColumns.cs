namespace TaskBoard.Application.Notifications;

// Bu adlardaki sütunlardaki kartlar "bitmiş" sayılır; hatırlatma ve özet maillerine girmez.
// Basit bir kural: ileride sütuna "tamamlandı sütunu" işareti eklenerek kullanıcıya bırakılabilir.
public static class CompletedColumns
{
    public static readonly string[] Names = ["bitti", "tamamlandı", "done"];
}
