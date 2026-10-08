namespace TaskBoard.Application.Boards;

public record BoardTemplateLabel(string Name, string Color);

public record BoardTemplate(
    string Id,
    string Name,
    string Description,
    IReadOnlyList<string> Columns,
    IReadOnlyList<BoardTemplateLabel> Labels);

// Yeni pano açarken hazır sütun ve etiket setleri.
public static class BoardTemplates
{
    public const string DefaultId = "basic";

    public static readonly IReadOnlyList<BoardTemplate> All =
    [
        new(DefaultId, "Basit", "Her iş için üç sütun.",
            ["Yapılacak", "Yapılıyor", "Bitti"],
            []),

        new("software", "Yazılım projesi", "Fikirden teste kadar geliştirme akışı.",
            ["Backlog", "Yapılacak", "Yapılıyor", "Test", "Bitti"],
            [new("Bug", "red"), new("Özellik", "green"), new("İyileştirme", "sky"), new("Teknik borç", "amber")]),

        new("school", "Ders / Ödev", "Ödevleri, projeleri ve sınavları takip et.",
            ["Yapılacak", "Çalışılıyor", "Teslim edildi"],
            [new("Ödev", "indigo"), new("Proje", "violet"), new("Sınav", "red")]),

        new("personal", "Kişisel", "Günlük işler ve kişisel hedefler.",
            ["Fikirler", "Bu hafta", "Bugün", "Bitti"],
            [new("Ev", "teal"), new("Sağlık", "green"), new("Alışveriş", "amber"), new("İş", "slate")]),
    ];

    public static BoardTemplate Find(string? id) =>
        All.FirstOrDefault(t => t.Id == (id ?? DefaultId))
        ?? throw new Common.Exceptions.BadRequestException("Geçersiz pano şablonu.");
}
