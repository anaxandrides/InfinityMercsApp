using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Presentation.Season.Overview;
using InfinityMercsApp.ViewModels.Season.Overview.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview.Items;

public sealed class GearSlotPickerTests
{
    private readonly List<GearOption> _picked = [];
    private readonly GearSlotPicker _picker;

    public GearSlotPickerTests() => _picker = new GearSlotPicker(GearSlot.Sidearm, (_, option) => _picked.Add(option));

    [Fact]
    public void Title_Slot_IsTheSlotsName()
    {
        Assert.Equal("SIDEARM", _picker.Title);
        Assert.Equal([GearOption.None], _picker.Options);
    }

    [Fact]
    public void Show_SelectionRefilled_IsNotAPick()
    {
        var pistol = GearOption.For("Pistol", 0, null);

        _picker.Show([GearOption.None, pistol], pistol);

        Assert.Equal(pistol, _picker.SelectedOption);
        Assert.Empty(_picked);
    }

    [Fact]
    public void SelectedOption_Chosen_ReportsThePick()
    {
        var pistol = GearOption.For("Pistol", 0, "Bolt");
        _picker.Show([GearOption.None, pistol], GearOption.None);

        _picker.SelectedOption = pistol;

        Assert.Equal([pistol], _picked);
        Assert.Equal("Pistol  [Attached: Bolt]", pistol.Display);
    }
}
