using System.Globalization;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.Company.Selection.Rules;

/// <summary>
/// A company built around a generated faction (Airborne, Inspiring): the captain comes from a sectorial in
/// the left slot, and everyone else from the generated faction in the fixed right slot. Each slot lists its
/// own units; the left slot shows lieutenants only and is done once its captain is in the company.
/// </summary>
/// <param name="companyFactionId">The generated faction's id.</param>
/// <param name="companyFactionName">The generated faction's name in the right slot.</param>
/// <param name="companyLogoPath">The generated faction's packaged logo.</param>
public abstract class GeneratedFactionCompanyRules(int companyFactionId, string companyFactionName, string companyLogoPath)
    : CompanyRules(ArmySourceSelectionMode.Sectorials)
{
    private bool _hasLoadedFactions;

    public override string Heading => "Choose your sectorial";

    public override bool HasRightSlot => true;

    public override bool SavesRightSlot => false;

    public override bool AllowsTeamsFilter => false;

    public override GeneratedFactionLogo GeneratedFaction { get; } = new(companyFactionId, companyLogoPath);

    /// <summary>The generated faction goes in the right slot, and the left slot's list starts on lieutenants.</summary>
    public override void Prepare(CompanySelectionViewModel screen)
    {
        screen.Slots.Set(1, new ArmyFactionSelectionItem
        {
            Id = companyFactionId,
            ParentId = companyFactionId,
            Name = companyFactionName,
            PackagedLogoPath = companyLogoPath
        });
        screen.UnitList.ShowLieutenantsOnly(true);
    }

    /// <summary>Only sectorials with a lieutenant for the captain are listed, so each army is read.</summary>
    public override bool ListsFromEveryArmy => true;

    /// <summary>The sectorials that have a unit able to be lieutenant.</summary>
    public override async Task<IReadOnlyList<FactionRecord>> LoadFactionsAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken)
    {
        var factions = await screen.Services.FactionCatalog.LoadAsync(Mode, cancellationToken);
        _hasLoadedFactions = true;
        return factions.Where(faction => HasLieutenant(screen, faction.Id, cancellationToken)).ToList();
    }

    private static bool HasLieutenant(CompanySelectionViewModel screen, int factionId, CancellationToken cancellationToken)
    {
        var armyData = screen.Services.ArmyData;
        var skills = (armyData.GetFactionSnapshot(factionId, cancellationToken)?.Filters ?? ArmyFilterCatalog.Empty)
            .Names(ArmyFilterSection.Skills);
        return armyData
            .GetMergedMercsArmyList([factionId], cancellationToken)
            .Any(entry => entry.ProfileGroups.HasLieutenantOption(skills));
    }

    /// <summary>The picked sectorial always goes in the left slot, whose units then show.</summary>
    public override async Task AssignFactionAsync(CompanySelectionViewModel screen, ArmyFactionSelectionItem faction)
    {
        if (!screen.Slots.TryAssign(0, faction, out var factionChanged))
        {
            return;
        }

        screen.Slots.ActiveIndex = 0;
        screen.UnitList.ShowLieutenantsOnly(true);
        if (factionChanged)
        {
            screen.ResetCompany();
        }

        screen.UnitList.TeamsView = false;
        screen.ShowFactionStrip = false;
        await screen.LoadUnitsAsync();
    }

    /// <summary>A recruiting company already has its captain, so recruits come from the generated faction.</summary>
    public override void PrepareRecruitingSlots(CompanySelectionViewModel screen)
    {
        screen.Slots.ActiveIndex = 1;
        screen.UnitList.ShowLieutenantsOnly(false);
    }

    /// <summary>The left slot lists lieutenants only; switching slots reloads the list.</summary>
    public override void SetActiveSlot(CompanySelectionViewModel screen, int slot)
    {
        var previous = screen.Slots.ActiveIndex;
        screen.Slots.ActiveIndex = FactionSlots.Active(slot, HasRightSlot);
        screen.UnitList.ShowLieutenantsOnly(screen.Slots.ActiveIndex == 0);
        if (previous != screen.Slots.ActiveIndex && _hasLoadedFactions)
        {
            _ = screen.LoadUnitsAsync();
        }
    }

    public override IReadOnlyList<ArmyFactionSelectionItem> UnitSources(CompanySelectionViewModel screen) => screen.Slots.ActiveSource;

    /// <summary>The generated faction's units borrow their source units' logos, so none are cached for it.</summary>
    public override Task CacheUnitLogosAsync(
        FactionLogoCacheService? logos, int factionId, IReadOnlyList<ArmyResumeRecord> units, CancellationToken cancellationToken) =>
        factionId == companyFactionId ? Task.CompletedTask : base.CacheUnitLogosAsync(logos, factionId, units, cancellationToken);

    /// <summary>A generated unit's logo field holds its source unit as "{factionId}-{unitId}".</summary>
    public override UnitLogoIds LogoIdsFor(int factionId, int unitId, string? logoField)
    {
        if (factionId != companyFactionId || string.IsNullOrWhiteSpace(logoField))
        {
            return new UnitLogoIds(factionId, unitId);
        }

        var dash = logoField.IndexOf('-');
        return dash > 0 && dash < logoField.Length - 1 &&
               int.TryParse(logoField.AsSpan(0, dash), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sourceFaction) &&
               int.TryParse(logoField.AsSpan(dash + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var sourceUnit)
            ? new UnitLogoIds(sourceFaction, sourceUnit)
            : new UnitLogoIds(factionId, unitId);
    }

    /// <summary>When the generated faction has no units (its data is missing), the left slot's list shows instead.</summary>
    public override async Task<bool> TryRecoverEmptyUnitListAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken)
    {
        if (screen.UnitList.Units.Count > 0 || screen.Slots.ActiveIndex != 1 || screen.Slots.Left is null)
        {
            return false;
        }

        screen.Slots.ActiveIndex = 0;
        screen.UnitList.ShowLieutenantsOnly(true);
        await screen.LoadUnitsAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Hides the left slot's units once its captain is in, drops "lieutenants only" when it would empty the list,
    /// and never leaves the list blank.
    /// </summary>
    public override void AdjustUnitVisibility(
        CompanySelectionViewModel screen, CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken)
    {
        var units = screen.UnitList.Units;
        HideLeftSlotUnitsOnceCaptainChosen(screen);
        if (!units.Any(unit => unit.IsVisible) && screen.UnitList.LieutenantOnlyUnits)
        {
            screen.UnitList.DropLieutenantsOnly();
            screen.ApplyUnitVisibility(context, cancellationToken);
            HideLeftSlotUnitsOnceCaptainChosen(screen);
        }

        if (!units.Any(unit => unit.IsVisible))
        {
            var hasLeftSlotEntry = HasLeftSlotEntry(screen);
            foreach (var unit in units)
            {
                unit.IsVisible = !hasLeftSlotEntry || !IsLeftSlotUnit(unit);
            }
        }
    }

    private void HideLeftSlotUnitsOnceCaptainChosen(CompanySelectionViewModel screen)
    {
        if (!HasLeftSlotEntry(screen))
        {
            return;
        }

        foreach (var unit in screen.UnitList.Units.Where(IsLeftSlotUnit))
        {
            unit.IsVisible = false;
        }
    }

    protected bool IsLeftSlotUnit(ArmyUnitSelectionItem? unit) => unit is not null && unit.SourceFactionId != companyFactionId;

    private bool HasLeftSlotEntry(CompanySelectionViewModel screen) =>
        screen.Roster.Entries.Any(entry => entry.SourceFactionId != companyFactionId);

    /// <summary>The left slot gives only the captain.</summary>
    public override bool CanAddProfile(CompanySelectionViewModel screen, ArmyUnitSelectionItem unit) =>
        !(IsLeftSlotUnit(unit) && HasLeftSlotEntry(screen));

    /// <summary>Once the captain is in, the generated faction's units show.</summary>
    public override Task AfterEntryAddedAsync(
        CompanySelectionViewModel screen, ArmyUnitSelectionItem unit, ViewerProfileItem profile, MercsCompanyEntry entry)
    {
        if (IsLeftSlotUnit(unit))
        {
            SetActiveSlot(screen, 1);
        }

        return Task.CompletedTask;
    }

    /// <summary>With the captain in, the left slot's profiles are all blocked.</summary>
    public override int AdjustProfileAvailability(CompanySelectionViewModel screen, int visibleProfiles)
    {
        if (!IsLeftSlotUnit(screen.Preview.SelectedUnit) || !HasLeftSlotEntry(screen))
        {
            return visibleProfiles;
        }

        screen.Preview.BlockAll();
        return 0;
    }
}
