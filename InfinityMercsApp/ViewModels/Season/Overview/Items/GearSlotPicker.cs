using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Season.Gear;

namespace InfinityMercsApp.ViewModels.Season.Overview.Items;

/// <summary>
/// The picker for one gear slot of the selected trooper. Choosing an option reports it; refilling the
/// options (which makes a picker forget its choice) does not.
/// </summary>
public sealed partial class GearSlotPicker(GearSlot slot, Action<GearSlot, GearOption> picked) : ObservableObject
{
    private bool _refilling;

    public GearSlot Slot { get; } = slot;

    /// <summary>The slot's heading, e.g. "SIDEARM".</summary>
    public string Title { get; } = slot.Key().ToUpperInvariant();

    [ObservableProperty]
    public partial IReadOnlyList<GearOption> Options { get; set; } = [GearOption.None];

    [ObservableProperty]
    public partial GearOption? SelectedOption { get; set; } = GearOption.None;

    /// <summary>Replaces the options and shows <paramref name="selected"/> as chosen, without reporting a pick.</summary>
    public void Show(IReadOnlyList<GearOption> options, GearOption selected)
    {
        _refilling = true;
        try
        {
            Options = options;
            SelectedOption = selected;
        }
        finally
        {
            _refilling = false;
        }
    }

    // A picker clears its choice (to null) when its options change; only a real choice counts.
    partial void OnSelectedOptionChanged(GearOption? value)
    {
        if (!_refilling && value is not null)
        {
            picked(Slot, value);
        }
    }
}
