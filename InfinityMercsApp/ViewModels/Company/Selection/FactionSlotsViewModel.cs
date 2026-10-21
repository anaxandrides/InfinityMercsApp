using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels.Collections;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// The factions a company's units come from: the picker's list, and the left slot and (for some companies) the
/// right slot, with the logos the title bar shows and which slot the next pick goes to.
/// </summary>
public sealed partial class FactionSlotsViewModel(bool hasRightSlot) : ObservableObject
{
    private ArmyFactionSelectionItem? _picked;

    /// <summary>True while the faction list is being loaded.</summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial ArmyFactionSelectionItem? Left { get; set; }

    [ObservableProperty]
    public partial ArmyFactionSelectionItem? Right { get; set; }

    [ObservableProperty]
    public partial string? LeftLogoSource { get; set; }

    [ObservableProperty]
    public partial string? RightLogoSource { get; set; }

    /// <summary>The slot the next faction pick goes to: 0 left, 1 right.</summary>
    [ObservableProperty]
    public partial int ActiveIndex { get; set; }

    /// <summary>Whether the company draws units from a second faction slot.</summary>
    public bool HasRightSlot { get; } = hasRightSlot;

    /// <summary>The factions the picker offers.</summary>
    public ObservableRangeCollection<ArmyFactionSelectionItem> Factions { get; } = [];

    /// <summary>Whether every slot the company has is filled.</summary>
    public bool AreFilled => Left is not null && (!HasRightSlot || Right is not null);

    /// <summary>The factions whose units are listed: the left slot's, and the right slot's when it has another.</summary>
    public IReadOnlyList<ArmyFactionSelectionItem> Sources => FactionSlots.SourceFactions(HasRightSlot, Left, Right);

    /// <summary>Only the active slot's faction, for companies whose slots each list their own units.</summary>
    public IReadOnlyList<ArmyFactionSelectionItem> ActiveSource => FactionSlots.ActiveSlotFaction(ActiveIndex, Left, Right);

    public bool IsEmpty(int slot) => (slot == 1 ? Right : Left) is null;

    /// <summary>Marks <paramref name="faction"/> as the one picked in the list.</summary>
    public void Highlight(ArmyFactionSelectionItem faction)
    {
        if (ReferenceEquals(_picked, faction))
        {
            return;
        }

        if (_picked is not null)
        {
            _picked.IsSelected = false;
        }

        _picked = faction;
        faction.IsSelected = true;
    }

    /// <summary>Puts <paramref name="faction"/> in slot <paramref name="slot"/> unless the other slot already has it.</summary>
    public bool TryAssign(int slot, ArmyFactionSelectionItem faction, out bool factionChanged)
    {
        if (FactionSlots.IsInOtherSlot(HasRightSlot, slot, Left?.Id, Right?.Id, faction.Id))
        {
            factionChanged = false;
            return false;
        }

        var target = slot == 0 || !HasRightSlot ? 0 : 1;
        factionChanged = (target == 0 ? Left : Right)?.Id != faction.Id;
        Set(target, faction);
        return true;
    }

    /// <summary>Fills a slot and shows its logo, the downloaded one when it exists.</summary>
    public void Set(int slot, ArmyFactionSelectionItem faction)
    {
        var logo = UnitLogo.Source(faction.CachedLogoPath, faction.PackagedLogoPath);
        if (slot == 0)
        {
            Left = faction;
            LeftLogoSource = logo;
        }
        else
        {
            Right = faction;
            RightLogoSource = logo;
        }
    }
}
