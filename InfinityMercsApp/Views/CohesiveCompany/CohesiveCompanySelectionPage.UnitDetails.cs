using System.Globalization;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using static InfinityMercsApp.Views.Common.CompanyUnitDetailsShared;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.Views.CohesiveCompany;
/// <summary>
/// Selected-unit detail pipeline: profile parsing, stat projection, and peripheral/trait extraction.
/// </summary>
public partial class CohesiveCompanySelectionPage
{
    private void LoadSelectedUnitDetails(CancellationToken cancellationToken = default)
    {
        ResetUnitDetails(clearLogo: false, resetHeaderColors: false);
        if (_selectedUnit is null)
        {
            Console.Error.WriteLine("CompanySelectionPage LoadSelectedUnitDetails aborted: selected unit or accessor missing.");
            return;
        }

        try
        {
            CompanyUnitDetailsWorkflowService.Load(
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
                    GetSpecopsUnitsByFaction = (factionId, token) => _specOpsProvider.GetSpecopsUnitsByFaction(factionId, token),
                    ApplyUnitHeaderColors = ApplyUnitHeaderColors,
                    ApplyGlobalDisplayUnitsPreference = token => CompanyUnitDetailsShared.ApplyGlobalDisplayUnitsPreference(
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
            Console.WriteLine($"CompanySelectionPage LoadSelectedUnitDetails completed: heading='{UnitNameHeading}', MOV='{UnitMov}', equipment='{EquipmentSummary}'.");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanySelectionPage LoadSelectedUnitDetails failed: {ex}");
        }
    }

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
                AdjustOptionDisplayCost = (group, option, adjustedCost) =>
                    ApplyCohesiveTacticalAwarenessDiscount(profileGroups, filters, group, option, adjustedCost),
                FormatMoveValue = FormatMoveValue
            });

        foreach (var profileItem in profiles)
        {
            Profiles.Add(profileItem);
        }

        ApplyLieutenantVisualStates();
    }

    private bool ShouldIncludeOption(string? optionName)
    {
        return !_restrictSelectedUnitProfilesToFto || FireteamMatching.IsFtoLabel(optionName);
    }

    /// <summary>
    /// Cohesive companies get 3 points off any trooper with Tactical Awareness (option, its includes, or its profiles).
    /// </summary>
    private static string ApplyCohesiveTacticalAwarenessDiscount(
        UnitProfileGroups profileGroups,
        ArmyFilterCatalog? filters,
        ProfileGroup group,
        ProfileOption option,
        string adjustedCost)
    {
        if (!int.TryParse(adjustedCost, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCost))
        {
            return adjustedCost;
        }

        var skillsLookup = (filters ?? ArmyFilterCatalog.Empty).Names(ArmyFilterSection.Skills);
        if (!HasTacticalAwarenessInOptionOrGroup(profileGroups, skillsLookup, group, option))
        {
            return adjustedCost;
        }

        return Math.Max(0, parsedCost - 3).ToString(CultureInfo.InvariantCulture);
    }

    private static bool HasTacticalAwarenessInOptionOrGroup(
        UnitProfileGroups profileGroups,
        IReadOnlyDictionary<int, string> skillsLookup,
        ProfileGroup group,
        ProfileOption option)
    {
        return ContainsTacticalAwareness(profileGroups.GetEntriesWithIncludes(option, ProfileEntryKind.Skills), skillsLookup) ||
               group.Profiles.Any(profile => ContainsTacticalAwareness(profile.Skills, skillsLookup));
    }

    private static bool ContainsTacticalAwareness(IEnumerable<ProfileEntry> skillEntries, IReadOnlyDictionary<int, string> skillsLookup)
    {
        return skillEntries.Any(entry =>
            entry.Id is int skillId &&
            skillsLookup.TryGetValue(skillId, out var skillName) &&
            !string.IsNullOrWhiteSpace(skillName) &&
            skillName.Contains("tactical awareness", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Stream?> OpenBestUnitLogoStreamAsync(ArmyUnitSelectionItem item)
    {
        return await CompanyUnitDetailsShared.OpenBestUnitLogoStreamAsync(
            item.Name,
            item.Id,
            item.SourceFactionId,
            BuildUnitCachedPathCandidates(item),
            BuildUnitPackagedPathCandidates(item));
    }

    private IEnumerable<string?> BuildUnitCachedPathCandidates(ArmyUnitSelectionItem item)
    {
        return BuildUnitCachedPathCandidatesCore(
            item,
            _factionSelectionState.LeftSlotFaction?.Id,
            _factionSelectionState.RightSlotFaction?.Id,
            _factionLogoCacheService is null ? null : (factionId, unitId) => _factionLogoCacheService.GetCachedUnitLogoPath(factionId, unitId),
            _factionLogoCacheService is null ? null : factionId => _factionLogoCacheService.GetCachedLogoPath(factionId));
    }

    private IEnumerable<string?> BuildUnitPackagedPathCandidates(ArmyUnitSelectionItem item)
    {
        return BuildUnitPackagedPathCandidatesCore(
            item,
            _factionSelectionState.LeftSlotFaction?.Id,
            _factionSelectionState.RightSlotFaction?.Id,
            _factionLogoCacheService is null ? null : (factionId, unitId) => _factionLogoCacheService.GetPackagedUnitLogoPath(factionId, unitId),
            _factionLogoCacheService is null ? null : factionId => _factionLogoCacheService.GetPackagedFactionLogoPath(factionId));
    }


    private static string BuildUnitSubtitle(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        return BuildUnitSubtitleCore(unit, typeLookup, categoryLookup);
    }

    private static bool IsCharacterCategory(ArmyResumeRecord unit, IReadOnlyDictionary<int, string> categoryLookup)
    {
        return IsCharacterCategoryCore(unit, categoryLookup);
    }

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
        UnitVitalityHeader = Stat.Vita.Key();
        UnitVitality = "-";
        UnitS = "-";
        UnitAva = "-";
        ResetPeripheralStatsOnly();
    }

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
        PeripheralVitalityHeader = Stat.Vita.Key();
        PeripheralVitality = "-";
        PeripheralS = "-";
        PeripheralAva = "-";
        PeripheralEquipment = "-";
        PeripheralSkills = "-";
        PeripheralEquipmentFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Equipment", [], Color.FromArgb("#B5C0CE"));
        PeripheralSkillsFormatted = CompanyProfileTextService.BuildNamedSummaryFormatted("Skills", [], Color.FromArgb("#F59E0B"));
    }

    /// <summary>
    /// Shows the statline of the unit's first profile.
    /// </summary>
    private void PopulateUnitStatsFromFirstProfile(UnitProfileGroups profileGroups)
    {
        CompanyUnitDetailsShared.PopulateUnitStatsFromFirstProfile(profileGroups, ResetUnitStatsOnly, PopulateUnitStatsFromProfile);
    }

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

    private string FormatMoveValue(int? firstCm, int? secondCm)
    {
        return _armyDataService.FormatMoveValue(firstCm, secondCm);
    }

    private void UpdateUnitMoveDisplay()
    {
        UnitMov = _armyDataService.FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm);
    }

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










