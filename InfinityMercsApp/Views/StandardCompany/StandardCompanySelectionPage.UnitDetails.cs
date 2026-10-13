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

namespace InfinityMercsApp.Views.StandardCompany;

/// <summary>
/// Selected-unit detail pipeline: profile parsing, stat projection, and peripheral/trait extraction.
/// </summary>
public partial class StandardCompanySelectionPage
{
    /// <summary>
    /// Handles load selected unit details async.
    /// </summary>
    private async Task LoadSelectedUnitDetailsAsync(CancellationToken cancellationToken = default)
    {
        ResetUnitDetails(clearLogo: false, resetHeaderColors: false);
        if (_selectedUnit is null)
        {
            Console.Error.WriteLine("CompanySelectionPage LoadSelectedUnitDetailsAsync aborted: selected unit or accessor missing.");
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
                    GetSpecopsUnitsByFactionAsync = (factionId, token) => _specOpsProvider.GetSpecopsUnitsByFactionAsync(factionId, token),
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
                        UnitDisplayConfigurationsView.SelectedUnitCommonEquipment = equipment.ToList();
                        UnitDisplayConfigurationsView.SelectedUnitCommonSkills = skills.ToList();
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
                    SetSelectedUnitProfileGroups = value => UnitDisplayConfigurationsView.SelectedUnitProfileGroups = value,
                    SetSelectedUnitFilters = value => UnitDisplayConfigurationsView.SelectedUnitFilters = value,
                    SetUnitNameHeading = value => UnitNameHeading = value,
                    SetSummaryHighlightLieutenant = value => _summaryHighlightLieutenant = value,
                    LogInfo = Console.WriteLine,
                    LogError = Console.Error.WriteLine
                },
                cancellationToken);
            Console.WriteLine($"CompanySelectionPage LoadSelectedUnitDetailsAsync completed: heading='{UnitNameHeading}', MOV='{UnitMov}', equipment='{EquipmentSummary}'.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanySelectionPage LoadSelectedUnitDetailsAsync failed: {ex}");
        }
    }

    /// <summary>
    /// Handles load selected unit logo async.
    /// </summary>
    private async Task LoadSelectedUnitLogoAsync(ArmyUnitSelectionItem item)
    {
        await LoadSelectedUnitLogoCoreAsync(
            item,
            UnitDisplayConfigurationsView,
            () => OpenBestUnitLogoStreamAsync(item));
    }

    private void PopulateProfilesFromProfileGroups(UnitProfileGroups profileGroups, ArmyFilterCatalog? filters, bool forceLieutenant = false)
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
                Filters = filters ?? ArmyFilterCatalog.Empty,
                ForceLieutenant = forceLieutenant,
                ShowTacticalAwarenessIcon = ShowTacticalAwarenessIcon,
                ShowUnitsInInches = ShowUnitsInInches,
                ShouldIncludeOption = (_, _, optionName) => ShouldIncludeOption(optionName),
                FormatMoveValue = FormatMoveValue
            });

        foreach (var profileItem in profiles)
        {
            Profiles.Add(profileItem);
        }

        ApplyLieutenantVisualStates();
    }

    private static bool ShouldIncludeOption(string? optionName)
    {
        return true;
    }

    /// <summary>
    /// Handles get ordered id display names from entries.
    /// </summary>
    /// <summary>
    /// Handles get counted display names from entries.
    /// </summary>
    /// <summary>
    /// Handles open best unit logo stream async.
    /// </summary>
    private async Task<Stream?> OpenBestUnitLogoStreamAsync(ArmyUnitSelectionItem item)
    {
        return await CompanyUnitDetailsShared.OpenBestUnitLogoStreamAsync(
            item.Name,
            item.Id,
            item.SourceFactionId,
            BuildUnitCachedPathCandidates(item),
            BuildUnitPackagedPathCandidates(item));
    }

    /// <summary>
    /// Handles build unit cached path candidates.
    /// </summary>
    private IEnumerable<string?> BuildUnitCachedPathCandidates(ArmyUnitSelectionItem item)
    {
        return BuildUnitCachedPathCandidatesCore(
            item,
            _factionSelectionState.LeftSlotFaction?.Id,
            _factionSelectionState.RightSlotFaction?.Id,
            _factionLogoCacheService is null ? null : (factionId, unitId) => _factionLogoCacheService.GetCachedUnitLogoPath(factionId, unitId),
            _factionLogoCacheService is null ? null : factionId => _factionLogoCacheService.GetCachedLogoPath(factionId));
    }

    /// <summary>
    /// Handles build unit packaged path candidates.
    /// </summary>
    private IEnumerable<string?> BuildUnitPackagedPathCandidates(ArmyUnitSelectionItem item)
    {
        return BuildUnitPackagedPathCandidatesCore(
            item,
            _factionSelectionState.LeftSlotFaction?.Id,
            _factionSelectionState.RightSlotFaction?.Id,
            _factionLogoCacheService is null ? null : (factionId, unitId) => _factionLogoCacheService.GetPackagedUnitLogoPath(factionId, unitId),
            _factionLogoCacheService is null ? null : factionId => _factionLogoCacheService.GetPackagedFactionLogoPath(factionId));
    }


    /// <summary>
    /// Handles build unit subtitle.
    /// </summary>
    private static string BuildUnitSubtitle(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        return BuildUnitSubtitleCore(unit, typeLookup, categoryLookup);
    }

    /// <summary>
    /// Handles is character category.
    /// </summary>
    private static bool IsCharacterCategory(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        return IsCharacterCategoryCore(unit, categoryLookup);
    }

    /// <summary>
    /// Handles reset unit details.
    /// </summary>
    private void ResetUnitDetails(bool clearLogo = true, bool resetHeaderColors = true)
    {
        UnitNameHeading = "Select a unit";
        if (resetHeaderColors)
        {
            ApplyUnitHeaderColorsByVanillaFactionName(null);
        }
        if (clearLogo)
        {
            ClearSelectedUnitLogoCore(
                UnitDisplayConfigurationsView,
                "CompanySelectionPage ResetUnitDetails: clearing selected unit logo.");
        }
        UnitDisplayConfigurationsView.SelectedUnitProfileGroups = UnitProfileGroups.Empty;
        UnitDisplayConfigurationsView.SelectedUnitFilters = ArmyFilterCatalog.Empty;
        ResetUnitStatsOnly();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        UnitDisplayConfigurationsView.SelectedUnitCommonEquipment = [];
        UnitDisplayConfigurationsView.SelectedUnitCommonSkills = [];
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

    /// <summary>
    /// Handles reset unit stats only.
    /// </summary>
    private void ResetUnitStatsOnly()
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

    /// <summary>
    /// Handles reset peripheral stats only.
    /// </summary>
    private void ResetPeripheralStatsOnly()
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
    private void PopulateUnitStatsFromFirstProfile(UnitProfileGroups profileGroups)
    {
        CompanyUnitDetailsShared.PopulateUnitStatsFromFirstProfile(profileGroups, ResetUnitStatsOnly, PopulateUnitStatsFromProfile);
    }

    /// <summary>
    /// Shows a statline in the unit stat block.
    /// </summary>
    private void PopulateUnitStatsFromProfile(UnitProfile profile)
    {
        var projection = CompanyUnitDetailsShared.BuildUnitStatProjection(
            profile,
            _armyDataService.FormatMoveValue);
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

    /// <summary>
    /// Handles format move value.
    /// </summary>
    private string FormatMoveValue(int? firstCm, int? secondCm)
    {
        return _armyDataService.FormatMoveValue(firstCm, secondCm);
    }

    /// <summary>
    /// Handles update unit move display.
    /// </summary>
    private void UpdateUnitMoveDisplay()
    {
        UnitMov = _armyDataService.FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm);
    }

    /// <summary>
    /// Handles update peripheral move display.
    /// </summary>
    private void UpdatePeripheralMoveDisplay()
    {
        PeripheralMov = _armyDataService.FormatMoveValue(PeripheralMoveFirstCm, PeripheralMoveSecondCm);
    }

    /// <summary>
    /// Shows the stat block of a peripheral statline.
    /// </summary>
    private void PopulatePeripheralStatsFromProfile(UnitProfile peripheralProfile, string peripheralName)
    {
        var peripheralStats = BuildPeripheralStatBlock(peripheralName, peripheralProfile, UnitDisplayConfigurationsView.SelectedUnitFilters);
        if (peripheralStats is null)
        {
            return;
        }

        ApplyPeripheralStatBlock(peripheralStats);
    }

    /// <summary>
    /// Handles update peripheral stat block from visible profiles.
    /// </summary>
    private void UpdatePeripheralStatBlockFromVisibleProfiles()
    {
        CompanyUnitDetailsShared.UpdatePeripheralStatBlockFromVisibleProfiles(
            UnitDisplayConfigurationsView.SelectedUnitProfileGroups,
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
    private PeripheralMercsCompanyStats? BuildPeripheralStatBlock(string peripheralName, UnitProfile peripheralProfile, ArmyFilterCatalog? filters)
    {
        return CompanyUnitDetailsShared.BuildPeripheralStatBlock(peripheralName, peripheralProfile, filters, ShowUnitsInInches);
    }

    /// <summary>
    /// Handles apply peripheral stat block.
    /// </summary>
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









