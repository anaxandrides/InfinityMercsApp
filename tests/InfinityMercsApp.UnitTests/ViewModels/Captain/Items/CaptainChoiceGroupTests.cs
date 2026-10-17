using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.ViewModels.Captain.Items;
namespace InfinityMercsApp.UnitTests.ViewModels.Captain.Items;

public sealed class CaptainChoiceGroupTests
{
    [Fact]
    public void ChangingAnySlotNotifiesTheOwner()
    {
        var changes = 0;
        var group = CaptainChoiceGroup.For("Skills", ["(2) - Courage"], () => changes++);

        group.First.SelectedChoice = "(2) - Courage";
        group.Third.SelectedChoice = "(2) - Courage";

        Assert.Equal(2, changes);
        Assert.Equal(4, group.Cost);
        Assert.Equal(["Courage"], group.ChosenNames);
    }

    [Fact]
    public void AClearedSlotCostsNothing()
    {
        var group = CaptainChoiceGroup.For("Weapons", ["(2) - Sniper Rifle"], () => { });

        group.First.SelectedChoice = null;

        Assert.Equal(0, group.First.Cost);
        Assert.Equal(string.Empty, group.First.ChosenName);
        Assert.Empty(group.ChosenNames);
    }

    [Fact]
    public void EverySlotStartsEmpty() =>
        Assert.All(
            CaptainChoiceGroup.For("Equipment", [], () => { }).Slots,
            slot => Assert.Equal([CaptainChoiceLabel.None], slot.Options));

    // A fresh database has no Spec-Ops lists since the API stopped sending them; the popup says so.
    [Fact]
    public void AGroupWithNothingToOfferSaysSo()
    {
        var empty = CaptainChoiceGroup.For("Weapons", [], () => { });
        var offered = CaptainChoiceGroup.For("Skills", ["(+2) - Courage"], () => { });

        Assert.False(empty.HasOptions);
        Assert.Equal("No weapons are stored for this army.", empty.NoOptionsText);
        Assert.True(offered.HasOptions);
    }
}
