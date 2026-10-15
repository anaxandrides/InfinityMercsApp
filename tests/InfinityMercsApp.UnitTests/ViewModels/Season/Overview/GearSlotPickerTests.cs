using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.ViewModels.Season.Overview;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class GearSlotPickerTests
{
    private readonly List<GearOption> _picked = [];
    private readonly GearSlotPicker _picker;

    public GearSlotPickerTests() => _picker = new GearSlotPicker(GearSlot.Sidearm, (_, option) => _picked.Add(option));

    [Fact]
    public void IsTitledBySlot()
    {
        Assert.Equal("SIDEARM", _picker.Title);
        Assert.Equal([GearOption.None], _picker.Options);
    }

    [Fact]
    public void RefillingDoesNotCountAsAPick()
    {
        var pistol = GearOption.For("Pistol", 0, null);

        _picker.Show([GearOption.None, pistol], pistol);

        Assert.Equal(pistol, _picker.SelectedOption);
        Assert.Empty(_picked);
    }

    [Fact]
    public void ChoosingReportsThePick()
    {
        var pistol = GearOption.For("Pistol", 0, "Bolt");
        _picker.Show([GearOption.None, pistol], GearOption.None);

        _picker.SelectedOption = pistol;

        Assert.Equal([pistol], _picked);
        Assert.Equal("Pistol  [Attached: Bolt]", pistol.Display);
    }
}
