using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class FactionSlotsViewModelTests
{
    private static ArmyFactionSelectionItem Faction(int id) => new() { Id = id, ParentId = id, Name = $"Faction {id}", PackagedLogoPath = $"SVGCache/factions/{id}.svg" };

    [Fact]
    public void TryAssign_EmptySlot_FillsItWithItsLogo()
    {
        var slots = new FactionSlotsViewModel(hasRightSlot: true);

        Assert.True(slots.TryAssign(1, Faction(5), out var changed));

        Assert.True(changed);
        Assert.Equal(5, slots.Right?.Id);
        Assert.Equal("SVGCache/factions/5.svg", slots.RightLogoSource);
        Assert.False(slots.AreFilled);
    }

    [Fact]
    public void TryAssign_FactionInTheOtherSlot_IsRefused()
    {
        var slots = new FactionSlotsViewModel(hasRightSlot: true);
        slots.Set(0, Faction(5));

        Assert.False(slots.TryAssign(1, Faction(5), out _));

        Assert.Null(slots.Right);
    }

    [Fact]
    public void TryAssign_SameFactionAgain_IsNotAChange()
    {
        var slots = new FactionSlotsViewModel(hasRightSlot: false);
        slots.Set(0, Faction(5));

        Assert.True(slots.TryAssign(1, Faction(5), out var changed));

        Assert.False(changed);
        Assert.True(slots.AreFilled);
    }

    [Fact]
    public void Sources_BothSlots_ListsEachFactionOnce()
    {
        var slots = new FactionSlotsViewModel(hasRightSlot: true);
        slots.Set(0, Faction(5));
        slots.Set(1, Faction(6));

        Assert.Equal([5, 6], slots.Sources.Select(faction => faction.Id));
        slots.ActiveIndex = 1;
        Assert.Equal([6], slots.ActiveSource.Select(faction => faction.Id));
    }

    [Fact]
    public void Highlight_AnotherFaction_MovesTheSelection()
    {
        var slots = new FactionSlotsViewModel(hasRightSlot: false);
        var first = Faction(5);
        var second = Faction(6);

        slots.Highlight(first);
        slots.Highlight(second);

        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
    }
}
