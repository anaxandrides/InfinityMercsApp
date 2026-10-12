using System.Globalization;
using System.Text;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using System.Text.RegularExpressions;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Common;
using static InfinityMercsApp.Views.Common.CompanyUnitDetailsShared;
using Svg.Skia;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;

namespace InfinityMercsApp.Views.Common;

/// <summary>
/// Selected-unit detail pipeline: profile parsing, stat projection, and peripheral/trait extraction.
/// </summary>
public abstract partial class GeneratedFactionCompanySelectionPageBase
{
    protected async Task LoadSelectedUnitDetailsAsync(CancellationToken cancellationToken = default)
    {
        ResetUnitDetails(clearLogo: false, resetHeaderColors: false);
        if (_selectedUnit is null)
        {
            Console.Error.WriteLine("GeneratedFactionCompanyPage LoadSelectedUnitDetailsAsync aborted: selected unit or accessor missing.");
            return;
        }

        try
        {
            await CompanyUnitDetailsWorkflowService.LoadAsync(
                new CompanyUnitDetailsWorkflowRequest
                {
                    UnitName = _selectedUnit.Name,
                    UnitId = _selectedUnit.Id,
                    SourceFactionId = _selectedUnit.SourceFactionId,
                    IsSpecOps = _selectedUnit.IsSpecOps,
                    LieutenantOnlyUnits = LieutenantOnlyUnits,
                    ShowUnitsInInches = ShowUnitsInInches,
                    GetUnit = GetUnitFromProvider,
                    GetFactionSnapshot = GetFactionSnapshotFromProvider,
                    GetSpecopsUnitsByFactionAsync = (factionId, token) => SpecOpsProvider.GetSpecopsUnitsByFactionAsync(factionId, token),
                    ApplyUnitHeaderColorsAsync = ApplyUnitHeaderColorsAsync,
                    ApplyGlobalDisplayUnitsPreferenceAsync = token => CompanyUnitDetailsShared.ApplyGlobalDisplayUnitsPreferenceAsync(
                        () => GetShowUnitsInInchesFromProvider(token),
                        ShowUnitsInInches,
                        value => ShowUnitsInInches = value,
                        UpdateUnitMoveDisplay,
                        UpdatePeripheralMoveDisplay,
                        RefreshMercsCompanyEntryDistanceDisplays,
                        Console.Error.WriteLine),
                    PopulateUnitStatsFromFirstProfile = PopulateUnitStatsFromFirstProfile,
                    SetOrderTraits = (hasRegular, hasIrregular, hasImpetuous, hasTacticalAwareness) =>
                    {
                        ShowIrregularOrderIcon = hasIrregular;
                        ShowRegularOrderIcon = !hasIrregular && hasRegular;
                        ShowImpetuousIcon = hasImpetuous;
                        ShowTacticalAwarenessIcon = hasTacticalAwareness;
                    },
                    SetTechTraits = (hasCube, hasCube2, hasHackable) =>
                    {
                        ShowCubeIcon = hasCube;
                        ShowCube2Icon = hasCube2;
                        ShowHackableIcon = hasHackable;
                    },
                    EnsureLieutenantSkill = CompanyProfileTextService.EnsureLieutenantSkill,
                    SetCommonEquipmentSkills = (equipment, skills, highlightLieutenant) =>
                    {
                        UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonEquipment = equipment.ToList();
                        UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonSkills = skills.ToList();
                        _summaryHighlightLieutenant = highlightLieutenant;
                    },
                    SetSummaryText = (equipmentSummary, specialSkillsSummary) =>
                    {
                        EquipmentSummary = equipmentSummary;
                        SpecialSkillsSummary = specialSkillsSummary;
                    },
                    RefreshSummaryFormatted = RefreshSummaryFormatted,
                    PopulateProfilesFromProfileGroups = PopulateProfilesFromProfileGroups,
                    UpdatePeripheralStatBlockFromVisibleProfiles = UpdatePeripheralStatBlockFromVisibleProfiles,
                    SetSelectedUnitProfileGroupsJson = value => UnitDisplayConfigurationsViewForVisuals.SelectedUnitProfileGroupsJson = value,
                    SetSelectedUnitFiltersJson = value => UnitDisplayConfigurationsViewForVisuals.SelectedUnitFiltersJson = value,
                    SetUnitNameHeading = value => UnitNameHeading = value,
                    SetSummaryHighlightLieutenant = value => _summaryHighlightLieutenant = value,
                    LogInfo = Console.WriteLine,
                    LogError = Console.Error.WriteLine
                },
                cancellationToken);
            Console.WriteLine($"GeneratedFactionCompanyPage LoadSelectedUnitDetailsAsync completed: heading='{UnitNameHeading}', MOV='{UnitMov}', equipment='{EquipmentSummary}'.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"GeneratedFactionCompanyPage LoadSelectedUnitDetailsAsync failed: {ex}");
        }
    }

    protected async Task LoadSelectedUnitLogoAsync(ArmyUnitSelectionItem item)
    {
        await LoadSelectedUnitLogoCoreAsync(
            item,
            UnitDisplayConfigurationsViewForVisuals,
            () => OpenBestUnitLogoStreamAsync(item));
    }

    protected void PopulateProfilesFromProfileGroups(UnitProfileGroups profileGroups, string? filtersJson, bool forceLieutenant = false)
    {
        Profiles.Clear();
        ProfilesStatus = "Loading profiles...";

        if (profileGroups.IsEmpty)
        {
            ProfilesStatus = "No profiles found for this unit.";
            return;
        }

        var profiles = CompanyProfilePopulationWorkflowService.BuildProfiles(
            new CompanyProfilePopulationRequest
            {
                ProfileGroups = profileGroups,
                FiltersJson = filtersJson,
                ForceLieutenant = forceLieutenant,
                ShowTacticalAwarenessIcon = ShowTacticalAwarenessIcon,
                ShowUnitsInInches = ShowUnitsInInches,
                ShouldIncludeOption = (_, _, _) => true,
                FormatMoveValue = FormatMoveValue
            });

        foreach (var profileItem in profiles)
        {
            Profiles.Add(profileItem);
        }

        ApplyLieutenantVisualStates();
    }

    protected async Task<Stream?> OpenBestUnitLogoStreamAsync(ArmyUnitSelectionItem item)
    {
        return await CompanyUnitDetailsShared.OpenBestUnitLogoStreamAsync(
            item.Name, item.Id, item.SourceFactionId,
            BuildUnitCachedPathCandidates(item),
            BuildUnitPackagedPathCandidates(item));
    }

    protected IEnumerable<string?> BuildUnitCachedPathCandidates(ArmyUnitSelectionItem item)
    {
        return BuildUnitCachedPathCandidatesCore(
            item,
            _factionSelectionState.LeftSlotFaction?.Id,
            null,
            FactionLogoCacheService is null ? null : (factionId, unitId) => FactionLogoCacheService.GetCachedUnitLogoPath(factionId, unitId),
            FactionLogoCacheService is null
                ? null
                : factionId => factionId == CompanyFactionId
                    ? null
                    : FactionLogoCacheService.GetCachedLogoPath(factionId));
    }

    protected IEnumerable<string?> BuildUnitPackagedPathCandidates(ArmyUnitSelectionItem item)
    {
        return BuildUnitPackagedPathCandidatesCore(
            item,
            _factionSelectionState.LeftSlotFaction?.Id,
            null,
            FactionLogoCacheService is null ? null : (factionId, unitId) => FactionLogoCacheService.GetPackagedUnitLogoPath(factionId, unitId),
            FactionLogoCacheService is null
                ? null
                : factionId => factionId == CompanyFactionId
                    ? CompanyLogoPath
                    : FactionLogoCacheService.GetPackagedFactionLogoPath(factionId));
    }

    protected static void MergeFireteamEntries(
        string? fireteamChartJson,
        Dictionary<string, CompanyTeamAggregate> target)
    {
        MergeFireteamEntriesCore(fireteamChartJson, target);
    }

    protected static string BuildUnitSubtitle(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        return BuildUnitSubtitleCore(unit, typeLookup, categoryLookup);
    }

    protected static bool IsCharacterCategory(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        return IsCharacterCategoryCore(unit, categoryLookup);
    }

    protected void ResetUnitDetails(bool clearLogo = true, bool resetHeaderColors = true)
    {
        UnitNameHeading = "Select a unit";
        if (resetHeaderColors)
        {
            ApplyUnitHeaderColorsByVanillaFactionName(null);
        }
        if (clearLogo)
        {
            ClearSelectedUnitLogoCore(
                UnitDisplayConfigurationsViewForVisuals,
                "GeneratedFactionCompanyPage ResetUnitDetails: clearing selected unit logo.");
        }
        UnitDisplayConfigurationsViewForVisuals.SelectedUnitProfileGroupsJson = null;
        UnitDisplayConfigurationsViewForVisuals.SelectedUnitFiltersJson = null;
        ResetUnitStatsOnly();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonEquipment = [];
        UnitDisplayConfigurationsViewForVisuals.SelectedUnitCommonSkills = [];
        _summaryHighlightLieutenant = false;
        RefreshSummaryFormatted();
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";
        ShowRegularOrderIcon = false;
        ShowIrregularOrderIcon = false;
        ShowImpetuousIcon = false;
        ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = false;
        ShowCube2Icon = false;
        ShowHackableIcon = false;
    }

    protected void ResetUnitStatsOnly()
    {
        UnitMoveFirstCm = null;
        UnitMoveSecondCm = null;
        UnitMov = "-";
        UnitCc = "-";
        UnitBs = "-";
        UnitPh = "-";
        UnitWip = "-";
        UnitArm = "-";
        UnitBts = "-";
        UnitVitalityHeader = "VITA";
        UnitVitality = "-";
        UnitS = "-";
        UnitAva = "-";
        ResetPeripheralStatsOnly();
    }

    protected void ResetPeripheralStatsOnly()
    {
        PeripheralMoveFirstCm = null;
        PeripheralMoveSecondCm = null;
        HasPeripheralStatBlock = false;
        PeripheralNameHeading = string.Empty;
        PeripheralMov = "-";
        PeripheralCc = "-";
        PeripheralBs = "-";
        PeripheralPh = "-";
        PeripheralWip = "-";
        PeripheralArm = "-";
        PeripheralBts = "-";
        PeripheralVitalityHeader = "VITA";
        PeripheralVitality = "-";
        PeripheralS = "-";
        PeripheralAva = "-";
        PeripheralEquipment = "-";
        PeripheralSkills = "-";
        PeripheralEquipmentFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Equipment", Array.Empty<string>(), Color.FromArgb("#B5C0CE"));
        PeripheralSkillsFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Skills", Array.Empty<string>(), Color.FromArgb("#F59E0B"));
    }

    /// <summary>
    /// Shows the statline of the unit's first profile.
    /// </summary>
    protected void PopulateUnitStatsFromFirstProfile(UnitProfileGroups profileGroups)
    {
        CompanyUnitDetailsShared.PopulateUnitStatsFromFirstProfile(profileGroups, ResetUnitStatsOnly, PopulateUnitStatsFromProfile);
    }

    protected void PopulateUnitStatsFromProfile(UnitProfile profile)
    {
        var projection = CompanyUnitDetailsShared.BuildUnitStatProjection(
            profile,
            ArmyDataService.FormatMoveValue);
        UnitMoveFirstCm = projection.MoveFirstCm;
        UnitMoveSecondCm = projection.MoveSecondCm;
        UnitMov = projection.Mov;
        UnitCc = projection.Cc;
        UnitBs = projection.Bs;
        UnitPh = projection.Ph;
        UnitWip = projection.Wip;
        UnitArm = projection.Arm;
        UnitBts = projection.Bts;
        UnitS = projection.S;
        UnitAva = projection.Ava;
        UnitVitalityHeader = projection.VitalityHeader;
        UnitVitality = projection.Vitality;
    }

    protected string FormatMoveValue(int? firstCm, int? secondCm)
    {
        return ArmyDataService.FormatMoveValue(firstCm, secondCm);
    }

    protected void UpdateUnitMoveDisplay()
    {
        UnitMov = ArmyDataService.FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm);
    }

    protected void UpdatePeripheralMoveDisplay()
    {
        PeripheralMov = ArmyDataService.FormatMoveValue(PeripheralMoveFirstCm, PeripheralMoveSecondCm);
    }

    /// <summary>
    /// Shows the stat block of a peripheral statline.
    /// </summary>
    protected void PopulatePeripheralStatsFromProfile(UnitProfile peripheralProfile, string peripheralName)
    {
        var peripheralStats = BuildPeripheralStatBlock(peripheralName, peripheralProfile, UnitDisplayConfigurationsViewForVisuals.SelectedUnitFiltersJson);
        if (peripheralStats is null)
        {
            return;
        }

        ApplyPeripheralStatBlock(peripheralStats);
    }

    protected void UpdatePeripheralStatBlockFromVisibleProfiles()
    {
        CompanyUnitDetailsShared.UpdatePeripheralStatBlockFromVisibleProfiles(
            UnitDisplayConfigurationsViewForVisuals.SelectedUnitProfileGroupsJson,
            Profiles,
            profile => profile.IsVisible,
            profile => profile.HasPeripherals,
            profile => profile.Peripherals,
            ExtractFirstPeripheralName,
            ResetPeripheralStatsOnly,
            PopulatePeripheralStatsFromProfile);
    }

    /// <summary>
    /// Builds the stat block for a peripheral statline.
    /// </summary>
    private PeripheralMercsCompanyStats? BuildPeripheralStatBlock(string peripheralName, UnitProfile peripheralProfile, string? filtersJson)
    {
        return CompanyUnitDetailsShared.BuildPeripheralStatBlock(peripheralName, peripheralProfile, filtersJson, ShowUnitsInInches);
    }

    private void ApplyPeripheralStatBlock(PeripheralMercsCompanyStats peripheralStats)
    {
        PeripheralMoveFirstCm = peripheralStats.MoveFirstCm;
        PeripheralMoveSecondCm = peripheralStats.MoveSecondCm;
        UpdatePeripheralMoveDisplay();
        PeripheralNameHeading = peripheralStats.NameHeading;
        PeripheralCc = peripheralStats.Cc;
        PeripheralBs = peripheralStats.Bs;
        PeripheralPh = peripheralStats.Ph;
        PeripheralWip = peripheralStats.Wip;
        PeripheralArm = peripheralStats.Arm;
        PeripheralBts = peripheralStats.Bts;
        PeripheralVitalityHeader = peripheralStats.VitalityHeader;
        PeripheralVitality = peripheralStats.Vitality;
        PeripheralS = peripheralStats.S;
        PeripheralAva = peripheralStats.Ava;
        PeripheralEquipment = peripheralStats.Equipment;
        PeripheralSkills = peripheralStats.Skills;
        PeripheralEquipmentFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Equipment", CompanyProfileTextService.SplitDisplayLine(PeripheralEquipment), Color.FromArgb("#B5C0CE"));
        PeripheralSkillsFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Skills", CompanyProfileTextService.SplitDisplayLine(PeripheralSkills), Color.FromArgb("#F59E0B"));
        HasPeripheralStatBlock = true;
    }
}
