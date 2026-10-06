using TaskBoard.Application.Common;

namespace TaskBoard.Tests;

public class PositioningTests
{
    private class Item(double position)
    {
        public double Position { get; set; } = position;
    }

    private static double Place(List<Item> siblings, int index) =>
        Positioning.PlaceAt(siblings, index, i => i.Position, (i, p) => i.Position = p);

    [Fact]
    public void Empty_list_gets_position_one()
    {
        Assert.Equal(1, Place([], 0));
    }

    [Fact]
    public void Dropping_between_two_items_uses_midpoint()
    {
        var siblings = new List<Item> { new(1), new(2), new(3) };

        Assert.Equal(1.5, Place(siblings, 1));
    }

    [Fact]
    public void Dropping_at_start_goes_before_first()
    {
        var siblings = new List<Item> { new(1), new(2) };

        Assert.Equal(0, Place(siblings, 0));
    }

    [Fact]
    public void Dropping_at_end_goes_after_last()
    {
        var siblings = new List<Item> { new(1), new(2) };

        Assert.Equal(3, Place(siblings, 2));
    }

    [Fact]
    public void Out_of_range_index_is_clamped_to_end()
    {
        var siblings = new List<Item> { new(1), new(2) };

        Assert.Equal(3, Place(siblings, 99));
    }

    [Fact]
    public void Moving_only_changes_the_moved_item()
    {
        var siblings = new List<Item> { new(1), new(2), new(3) };

        Place(siblings, 2);

        Assert.Equal([1, 2, 3], siblings.Select(s => s.Position));
    }

    [Fact]
    public void Rebalances_when_gap_becomes_too_small()
    {
        var siblings = new List<Item> { new(1), new(1 + Positioning.MinGap / 2), new(5) };

        var position = Place(siblings, 1);

        // Kardeşler yeniden numaralandı, taşınan öğe araya yerleşti: 1, [2], 3, 4
        Assert.Equal(2, position);
        Assert.Equal([1, 3, 4], siblings.Select(s => s.Position));
    }

    [Fact]
    public void Repeated_inserts_into_same_gap_keep_order()
    {
        // Hep aynı yere (ilk iki kartın arasına) ekleme: hassasiyet tükense bile sıra bozulmamalı.
        var list = new List<Item> { new(1), new(2) };

        for (var n = 0; n < 200; n++)
        {
            var item = new Item(0);
            item.Position = Place(list, 1);
            list.Insert(1, item);
        }

        var positions = list.Select(i => i.Position).ToList();
        Assert.Equal(positions.OrderBy(p => p), positions);
        Assert.Equal(positions.Count, positions.Distinct().Count());
    }
}
