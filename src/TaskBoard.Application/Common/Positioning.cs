namespace TaskBoard.Application.Common;

// Sürükle-bırak sıralaması için kesirli sıra numarası (fractional indexing).
//
// Bir öğe iki komşusunun arasına bırakılınca sıra numarası komşuların ortalaması olur:
//   [1, 2, 3] → 1 ile 2 arasına bırak → 1.5
// Böylece taşıma işlemi diğer satırlara dokunmadan TEK bir UPDATE ile biter.
//
// Sınırı: Aynı aralığa defalarca ekleme yapılırsa (1 → 1.5 → 1.25 → ...) double'ın hassasiyeti
// tükenir. Aralık MinGap'in altına düşünce tüm kardeşleri 1, 2, 3... diye yeniden numaralandırırız
// (rebalance). Bu nadir olur; normal kullanımda maliyet hep tek satırdır.
public static class Positioning
{
    public const double MinGap = 1e-6;

    /// <param name="siblings">Taşınan öğe HARİÇ, sıra numarasına göre sıralı kardeşler.</param>
    /// <param name="index">Öğenin bırakıldığı yer: önünde kaç kardeş olacağı.</param>
    /// <returns>Taşınan öğenin yeni sıra numarası. Gerekirse kardeşleri yeniden numaralandırır.</returns>
    public static double PlaceAt<T>(IList<T> siblings, int index, Func<T, double> getPosition, Action<T, double> setPosition)
    {
        index = Math.Clamp(index, 0, siblings.Count);

        double? before = index > 0 ? getPosition(siblings[index - 1]) : null;
        double? after = index < siblings.Count ? getPosition(siblings[index]) : null;

        var position = (before, after) switch
        {
            (null, null) => 1,
            (null, { } a) => a - 1,
            ({ } b, null) => b + 1,
            ({ } b, { } a) => (b + a) / 2
        };

        var tooClose = (before is { } lo && position - lo < MinGap) || (after is { } hi && hi - position < MinGap);
        if (!tooClose)
            return position;

        // Rebalance: önce gelenler 1..index, taşınan index+1, sonrakiler bir kaydırılmış.
        for (var i = 0; i < siblings.Count; i++)
            setPosition(siblings[i], i < index ? i + 1 : i + 2);

        return index + 1;
    }

    public static double After(double? lastPosition) => (lastPosition ?? 0) + 1;
}
