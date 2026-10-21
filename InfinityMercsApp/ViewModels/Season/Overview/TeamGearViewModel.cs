using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Presentation.Company.Viewer;
using InfinityMercsApp.Presentation.Season.Overview;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Season.Peripherals;
using InfinityMercsApp.ViewModels.Company.Viewer.Items;
using InfinityMercsApp.ViewModels.Season.Overview.Items;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>
/// The Team tab's gear: a picker per gear slot for the season gear the company owns, for the trooper shown.
/// Gear choices are saved to the season file. An augment needs its requirement met by what the trooper has,
/// its other gear included; a change that would leave an equipped augment without it takes that augment off,
/// once confirmed. The Peripheral (Controller) augment brings a peripheral with it, picked when it is equipped,
/// and leaves the peripheral uncontrolled when it comes off. The SWC of the gear equipped across the company
/// may not go over the company's SWC.
/// </summary>
public sealed partial class TeamGearViewModel : ObservableObject
{
    private readonly ISeasonFileService _seasonFiles;
    private readonly IDialogService _dialogs;
    private readonly ITeamChanges _changes;
    private readonly Func<Task> _seasonChanged;
    private readonly ObservableCollection<CompanyViewerUnitListItem> _troopers;
    private CompanyViewerUnitListItem? _shown;
    private CompanyLoadouts _loadouts = new();
    private SeasonInventory _inventory = SeasonInventory.Empty;
    private double _companySwc;
    private string? _seasonFilePath;

    /// <param name="troopers">The company's troopers, who can hold the gear a picker offers.</param>
    /// <param name="seasonChanged">Re-reads the season after gear is saved: armour changes a trooper's line, and gear counts toward renown.</param>
    public TeamGearViewModel(
        ISeasonFileService seasonFiles,
        IDialogService dialogs,
        ITeamChanges changes,
        Func<Task> seasonChanged,
        ObservableCollection<CompanyViewerUnitListItem> troopers)
    {
        _seasonFiles = seasonFiles;
        _dialogs = dialogs;
        _changes = changes;
        _seasonChanged = seasonChanged;
        _troopers = troopers;
        Slots = Enum.GetValues<GearSlot>().Select(slot => new GearSlotPicker(slot, OnGearPicked)).ToList();
    }

    /// <summary>A picker for each gear slot, in slot order.</summary>
    public IReadOnlyList<GearSlotPicker> Slots { get; }

    /// <summary>The company's equipped SWC against its SWC, e.g. "SWC EQUIPPED 1.5 / 2".</summary>
    [ObservableProperty]
    public partial string SwcText { get; set; } = string.Empty;

    /// <summary>What the company's gear is: each item's slot, SWC, requirement and armour.</summary>
    public InventoryCatalog Catalog { get; private set; } = InventoryCatalog.Empty;

    /// <summary>Takes in the season's loadouts, as saved in <paramref name="seasonFilePath"/>.</summary>
    public void Load(string? seasonFilePath, IReadOnlyDictionary<int, List<SeasonUnitGear>> unitGear)
    {
        _seasonFilePath = seasonFilePath;
        _loadouts = CompanyLoadouts.From(unitGear);
    }

    /// <summary>
    /// Takes in what the company now owns for the pickers, with the catalog that knows each item's SWC and the
    /// company's SWC that equipped gear must stay within.
    /// </summary>
    public void ShowOwned(SeasonInventory inventory, InventoryCatalog catalog, double companySwc)
    {
        _inventory = inventory;
        Catalog = catalog;
        _companySwc = companySwc;
        Refresh();
    }

    /// <summary>Shows the pickers for <paramref name="trooper"/>.</summary>
    public void Show(CompanyViewerUnitListItem trooper)
    {
        _shown = trooper;
        Refresh();
    }

    /// <summary>The gear a trooper carries, with what each item is.</summary>
    public IReadOnlyList<EquippedItem> CarriedBy(int entryIndex) => EquippedItem.In(_loadouts.For(entryIndex).ToSeasonGear(), Catalog);

    // Each slot offers nothing or an owned item that fits it, noting who else carries it.
    private void Refresh()
    {
        if (_shown is not { } item)
        {
            return;
        }

        SwcText = $"SWC EQUIPPED {SeasonEconomy.SwcText((decimal)EquippedSwc.Of(_loadouts, Catalog))} / {SeasonEconomy.SwcText((decimal)_companySwc)}";
        var loadout = _loadouts.For(item.EntryIndex);
        var perkSlots = PerkSlots.Filled(item.Progress?.Perks ?? []);
        foreach (var picker in Slots)
        {
            picker.FilledByPerk = perkSlots.GetValueOrDefault(picker.Slot) ?? string.Empty;
            picker.IsAvailable = picker.Slot switch
            {
                GearSlot.Extra => HasExtraSlot(loadout),
                // Cuckoo Virus Infusion leaves no Primary slot; one already there stays listed so it can come off.
                GearSlot.Primary => !HasNoPrimary(loadout) || loadout[GearSlot.Primary] is not null,
                // TAGs wear no armour; one already on stays listed so it can come off.
                GearSlot.Armor => !UnitTypeCodes.IsTag(item.UnitTypeCode) || loadout[GearSlot.Armor] is not null,
                _ => true
            };
            List<GearOption> options = [GearOption.None];
            var selected = GearOption.None;
            var held = HeldBy(item, loadout, picker.Slot);
            foreach (var itemName in _inventory.ItemsFor(picker.Slot))
            {
                var unmet = BannedFor(item, itemName) is { } banned ? $"A TAG can't have {banned}"
                    : Catalog.Find(itemName)?.Requirement is { } requirement && !requirement.IsMetBy(held) ? requirement.Unmet
                    : null;
                var itemOptions = OptionsFor(itemName, picker.Slot, item.EntryIndex, unmet);
                options.AddRange(itemOptions);
                if (string.Equals(itemName, loadout[picker.Slot], StringComparison.OrdinalIgnoreCase))
                {
                    selected = itemOptions[0];
                }
            }

            picker.Show(options, selected);
        }
    }

    private void OnGearPicked(GearSlot slot, GearOption option)
    {
        if (_shown is not { } item ||
            string.Equals(_loadouts.For(item.EntryIndex)[slot], option.ItemName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (option.ItemName is { } bannedItem && BannedFor(item, bannedItem) is { } banned)
        {
            _ = RefuseAsync($"{item.Name} is a TAG, and a TAG may never have {banned}.");
            return;
        }

        if (option.ItemName is { } itemName && Catalog.Find(itemName)?.Requirement is { } requirement &&
            !requirement.IsMetBy(HeldBy(item, _loadouts.For(item.EntryIndex), slot)))
        {
            _ = RefuseAsync($"{item.Name} can't take {itemName}. {requirement.Unmet}.");
            return;
        }

        var equippedSwc = EquippedSwc.After(_loadouts, item.EntryIndex, slot, option.ItemName, Catalog, option.TakenFrom);
        if (equippedSwc > _companySwc && equippedSwc > EquippedSwc.Of(_loadouts, Catalog))
        {
            _ = RefuseAsync(
                $"Equipping {option.ItemName} would bring the company's equipped SWC to {SeasonEconomy.SwcText((decimal)equippedSwc)}, " +
                $"over its {SeasonEconomy.SwcText((decimal)_companySwc)} SWC.",
                "Not Enough SWC");
            return;
        }

        var after = CompanyLoadouts.From(_loadouts.ToSeasonGear());
        after.Assign(item.EntryIndex, slot, option.ItemName, option.TakenFrom);
        if (slot == GearSlot.Armor && !HasExtraSlot(after.For(item.EntryIndex)))
        {
            // Taking off Belts and Pouches empties its slot.
            after.Assign(item.EntryIndex, GearSlot.Extra, null);
        }

        if (slot == GearSlot.Armor && HasNoPrimary(after.For(item.EntryIndex)))
        {
            // Cuckoo Virus Infusion takes the Primary slot away; its weapon goes back to the inventory.
            after.Assign(item.EntryIndex, GearSlot.Primary, null);
        }

        var stranded = StrandedBy(after);
        var peripherals = PeripheralChangeTo(after, item);
        if (stranded.Count > 0 || !peripherals.IsEmpty)
        {
            _ = ApplyAsync(after, stranded, peripherals);
            return;
        }

        _ = SaveAsync(after);
    }

    // Who loses or gains the Peripheral (Controller) augment, in its slot or the extra one.
    private PeripheralChange PeripheralChangeTo(CompanyLoadouts after, CompanyViewerUnitListItem picking)
    {
        bool Controls(CompanyLoadouts loadouts, int entryIndex) =>
            new List<GearSlot> { GearSlot.Augments, GearSlot.Extra }.Any(slot =>
                string.Equals(loadouts.For(entryIndex)[slot], TohaaPeripherals.AugmentName, StringComparison.OrdinalIgnoreCase));

        var losing = _troopers.Select(unit => unit.EntryIndex).Where(index => Controls(_loadouts, index) && !Controls(after, index)).ToList();
        var gaining = !Controls(_loadouts, picking.EntryIndex) && Controls(after, picking.EntryIndex) ? picking.EntryIndex : (int?)null;
        return new PeripheralChange(losing, gaining, picking.Name);
    }

    // The augments, on any trooper, that meet their requirement now but wouldn't with the gear in <paramref name="after"/>.
    private List<StrandedAugment> StrandedBy(CompanyLoadouts after)
    {
        var stranded = new List<StrandedAugment>();
        foreach (var unit in _troopers)
        {
            foreach (var slot in new List<GearSlot> { GearSlot.Augments, GearSlot.Extra })
            {
                if (after.For(unit.EntryIndex)[slot] is { } itemName &&
                    Catalog.Find(itemName)?.Requirement is { } requirement &&
                    !requirement.IsMetBy(HeldBy(unit, after.For(unit.EntryIndex), slot)) &&
                    requirement.IsMetBy(HeldBy(unit, _loadouts.For(unit.EntryIndex), slot)))
                {
                    stranded.Add(new StrandedAugment(unit.EntryIndex, unit.Name, slot, itemName, requirement));
                }
            }
        }

        return stranded;
    }

    // A change that strands augments goes ahead once confirmed, the stranded augments going back to the inventory;
    // one that moves the Peripheral (Controller) augment goes ahead once the peripherals have moved. Otherwise the
    // pickers go back to what was equipped.
    private async Task ApplyAsync(CompanyLoadouts after, IReadOnlyList<StrandedAugment> stranded, PeripheralChange peripherals)
    {
        if (stranded.Count > 0)
        {
            var lines = string.Join(Environment.NewLine, stranded.Select(augment => augment.Describe()));
            if (!await _dialogs.ConfirmAsync(
                    "Augments Come Off",
                    $"This leaves an augment without its requirement, so it goes back to the inventory:{Environment.NewLine}{lines}",
                    "Continue",
                    "Cancel"))
            {
                Refresh();
                return;
            }

            foreach (var augment in stranded)
            {
                after.Assign(augment.EntryIndex, augment.Slot, null);
            }
        }

        if (!peripherals.IsEmpty && !await _changes.ChangePeripheralsAsync(peripherals))
        {
            Refresh();
            return;
        }

        await SaveAsync(after);
    }

    // A spare copy is offered as itself; with none spare, each copy another slot holds is offered, with whose it is.
    // The trooper's own copy in this slot is always offered first.
    private List<GearOption> OptionsFor(string itemName, GearSlot slot, int entryIndex, string? unmet)
    {
        var swc = EquippedSwc.Of(itemName, Catalog);
        var holdings = _loadouts.Holding(itemName);
        var here = new GearHolding(entryIndex, slot);
        if (holdings.Contains(here) || holdings.Count < _inventory.CountOf(itemName))
        {
            return [GearOption.For(itemName, swc, null, unmet: unmet)];
        }

        return holdings
            .Where(holding => slot.SharesItemsWith(holding.Slot))
            .Select(holding => holding.EntryIndex)
            .Distinct()
            .Select(holder => GearOption.For(itemName, swc, _troopers.FirstOrDefault(unit => unit.EntryIndex == holder)?.Name, holder, unmet))
            .ToList();
    }

    // The skill an item would give a TAG that it may never have, if the trooper is a TAG.
    private static string? BannedFor(CompanyViewerUnitListItem item, string itemName) =>
        UnitTypeCodes.IsTag(item.UnitTypeCode) ? TagRestrictions.BannedSkillIn(itemName) : null;

    // What the trooper has besides the slot being filled: its skills, equipment and characteristics, and the gear in
    // the loadout's other slots (a role brings its skill).
    private static List<string> HeldBy(CompanyViewerUnitListItem item, UnitLoadout loadout, GearSlot slot)
    {
        var profile = CompanyRoster.ProfileOf(item);
        return
        [
            .. ProfileText.Lines(profile.UniqueSkills),
            .. ProfileText.Lines(profile.UniqueEquipment),
            .. ProfileText.Lines(profile.Characteristics),
            .. Enum.GetValues<GearSlot>().Where(other => other != slot).Select(other => loadout[other]).OfType<string>()
        ];
    }

    // Belts and Pouches gives one more slot.
    private bool HasExtraSlot(UnitLoadout loadout) =>
        loadout[GearSlot.Armor] is { } armor && Catalog.Find(armor)?.Armor?.Effects.ExtraSlot == true;

    private bool HasNoPrimary(UnitLoadout loadout) =>
        loadout[GearSlot.Armor] is { } armor && Catalog.Find(armor)?.Armor?.Effects.NoPrimary == true;

    // The picker goes back to what the trooper had once the alert is shown.
    private async Task RefuseAsync(string message, string title = "Can't Equip")
    {
        await _dialogs.ShowAlertAsync(title, message);
        Refresh();
    }

    // The season is re-read once saved, after the picker that reported the choice has finished with it. When the
    // save fails, the gear stays as it was.
    private async Task SaveAsync(CompanyLoadouts after)
    {
        var gear = after.ToSeasonGear();
        if (!await _seasonFiles.UpdateAsync(_seasonFilePath, seasonFile => seasonFile.UnitGear = gear))
        {
            await RefuseAsync("The gear couldn't be saved to the season file.", "Gear Not Saved");
            return;
        }

        _loadouts = after;
        await _seasonChanged();
    }
}
