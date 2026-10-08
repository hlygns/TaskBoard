namespace TaskBoard.Domain.Entities;

// Kart–etiket ara tablosu (çoka çok). Anahtar (CardId, LabelId): aynı etiket bir karta iki kez takılamaz.
public class CardLabel
{
    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;

    public Guid LabelId { get; set; }
    public Label Label { get; set; } = null!;
}
