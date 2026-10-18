using System.Globalization;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.CompanySelection.Common;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// A company built around a generated faction (Airborne, Inspiring): the captain comes from a sectorial in
/// the left slot, and everyone else from the generated faction in the fixed right slot. Each slot lists its
/// own units; the left slot shows lieutenants only and is done once its captain is in the company.
/// </summary>
public abstract class GeneratedFactionCompanySelectionViewModel : CompanySelectionViewModel
{
    private bool _hasLoadedFactions;

    protected GeneratedFactionCompanySelectionViewModel(CompanySelectionServices services)
        : base(services, ArmySourceSelectionMode.Sectorials)
    {
        IsUnitFilterActive = true;
        ShowLieutenantsOnly(true);
    }

    public override string Heading => "Choose your sectorial";

    public override bool ShowRightSlot => true;

    /// <summary>The generated faction's id.</summary>
    protected abstract int CompanyFactionId { get; }

    /// <summary>The generated faction's name in the right slot.</summary>
    protected abstract string CompanyFactionName { get; }

    /// <summary>The generated faction's packaged logo.</summary>
    protected abstract string CompanyLogoPath { get; }

    protected override bool AllowsTeamsFilter => false;

    protected override bool SavesRightSlot => false;

    protected override void OnFirstAppearing()
    {
        RightSlotFaction = new ArmyFactionSelectionItem
        {
            Id = CompanyFactionId,
            ParentId = CompanyFactionId,
            Name = CompanyFactionName,
            PackagedLogoPath = CompanyLogoPath
        };
        RightSlotLogoSource = CompanyLogoPath;
    }

    /// <summary>The sectorials that have a unit able to be lieutenant.</summary>
    protected override async Task<IReadOnlyList<FactionRecord>> LoadFactionRecordsAsync(CancellationToken cancellationToken)
    {
        var factions = await LoadModeFactionRecordsAsync(cancellationToken);
        _hasLoadedFactions = true;
        return factions.Where(faction => HasLieutenant(faction.Id, cancellationToken)).ToList();
    }

    private bool HasLieutenant(int factionId, CancellationToken cancellationToken)
    {
        var skills = CompanySelectionSharedUtilities.BuildIdNameLookup(
            Services.ArmyData.GetFactionSnapshot(factionId, cancellationToken)?.Filters, ArmyFilterSection.Skills);
        return Services.ArmyData
            .GetMergedMercsArmyList([factionId], cancellationToken)
            .Any(entry => CompanySelectionSharedUtilities.UnitHasLieutenantOption(entry.ProfileGroups, skills));
    }

    /// <summary>The picked sectorial always goes in the left slot, whose units then show.</summary>
    protected override async Task AssignFactionAsync(ArmyFactionSelectionItem item)
    {
        if (!TryAssignFaction(0, item, out var factionChanged))
        {
            return;
        }

        ActiveSlotIndex = 0;
        ShowLieutenantsOnly(true);
        if (factionChanged)
        {
            ResetCompany();
        }

        TeamsView = false;
        ShowFactionStrip = false;
        await LoadUnitsAsync();
    }

    /// <summary>A recruiting company already has its captain, so recruits come from the generated faction.</summary>
    protected override void PrepareRecruitingSlots()
    {
        ActiveSlotIndex = 1;
        ShowLieutenantsOnly(false);
    }

    /// <summary>The left slot lists lieutenants only; switching slots reloads the list.</summary>
    protected override void SetActiveSlot(int slot)
    {
        var previous = ActiveSlotIndex;
        ActiveSlotIndex = FactionSlots.Active(slot, ShowRightSlot);
        ShowLieutenantsOnly(ActiveSlotIndex == 0);
        if (previous != ActiveSlotIndex && _hasLoadedFactions)
        {
            _ = LoadUnitsAsync();
        }
    }

    private void ShowLieutenantsOnly(bool lieutenantsOnly)
    {
        ActiveUnitFilter = new UnitFilterCriteria { LieutenantOnlyUnits = lieutenantsOnly };
        LieutenantOnlyUnits = lieutenantsOnly;
        IsUnitFilterActive = ActiveUnitFilter.IsActive;
    }

    protected override IReadOnlyList<ArmyFactionSelectionItem> UnitSourceFactions() =>
        CompanyUnitDetailsShared.BuildUnitSourceFactionsForActiveSlot(ActiveSlotIndex, LeftSlotFaction, RightSlotFaction);

    /// <summary>The generated faction's units borrow their source units' logos, so none are cached for it.</summary>
    protected override Task CacheUnitLogosAsync(int factionId, IReadOnlyList<InfinityMercsApp.Domain.Models.Army.Resume> units, CancellationToken cancellationToken) =>
        factionId == CompanyFactionId ? Task.CompletedTask : base.CacheUnitLogosAsync(factionId, units, cancellationToken);

    /// <summary>A generated unit's logo field holds its source unit as "{factionId}-{unitId}".</summary>
    protected override UnitLogoIds LogoIdsFor(int factionId, int unitId, string? logoField)
    {
        if (factionId != CompanyFactionId || string.IsNullOrWhiteSpace(logoField))
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

    protected override string? CachedFactionLogoPath(int factionId) =>
        factionId == CompanyFactionId ? null : base.CachedFactionLogoPath(factionId);

    protected override string? PackagedFactionLogoPath(int factionId) =>
        factionId == CompanyFactionId ? CompanyLogoPath : base.PackagedFactionLogoPath(factionId);

    /// <summary>When the generated faction has no units (its data is missing), the left slot's list shows instead.</summary>
    protected override async Task<bool> TryRecoverEmptyUnitListAsync(CancellationToken cancellationToken)
    {
        if (Units.Count > 0 || ActiveSlotIndex != 1 || LeftSlotFaction is null)
        {
            return false;
        }

        ActiveSlotIndex = 0;
        ShowLieutenantsOnly(true);
        await LoadUnitsAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Hides the left slot's units once its captain is in, drops "lieutenants only" when it would empty the list,
    /// and never leaves the list blank.
    /// </summary>
    protected override void AdjustUnitVisibility(CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken)
    {
        HideLeftSlotUnitsOnceCaptainChosen();
        if (!Units.Any(unit => unit.IsVisible) && LieutenantOnlyUnits)
        {
            ActiveUnitFilter = ActiveUnitFilter with { LieutenantOnlyUnits = false, TeamsView = false };
            LieutenantOnlyUnits = false;
            IsUnitFilterActive = ActiveUnitFilter.IsActive;
            ApplyUnitVisibility(context, cancellationToken);
            HideLeftSlotUnitsOnceCaptainChosen();
        }

        if (!Units.Any(unit => unit.IsVisible))
        {
            var hasLeftSlotEntry = HasLeftSlotEntry();
            foreach (var unit in Units)
            {
                unit.IsVisible = !hasLeftSlotEntry || !IsLeftSlotUnit(unit);
            }
        }
    }

    private void HideLeftSlotUnitsOnceCaptainChosen()
    {
        if (!HasLeftSlotEntry())
        {
            return;
        }

        foreach (var unit in Units.Where(IsLeftSlotUnit))
        {
            unit.IsVisible = false;
        }
    }

    protected bool IsLeftSlotUnit(ArmyUnitSelectionItem? unit) => unit is not null && unit.SourceFactionId != CompanyFactionId;

    private bool HasLeftSlotEntry() => MercsCompanyEntries.Any(entry => entry.SourceFactionId != CompanyFactionId);

    /// <summary>The left slot gives only the captain.</summary>
    protected override bool CanAddProfile(ArmyUnitSelectionItem unit) => !(IsLeftSlotUnit(unit) && HasLeftSlotEntry());

    /// <summary>Once the captain is in, the generated faction's units show.</summary>
    protected override Task AfterEntryAddedAsync(ArmyUnitSelectionItem unit, ViewerProfileItem profile, MercsCompanyEntry entry)
    {
        if (IsLeftSlotUnit(unit))
        {
            SetActiveSlot(1);
        }

        return Task.CompletedTask;
    }

    /// <summary>With the captain in, the left slot's profiles are all blocked.</summary>
    protected override int AdjustLieutenantVisualStates(int visibleProfiles)
    {
        if (!IsLeftSlotUnit(SelectedUnit) || !HasLeftSlotEntry())
        {
            return visibleProfiles;
        }

        foreach (var profile in Profiles)
        {
            profile.IsLieutenantBlocked = true;
        }

        return 0;
    }
}
