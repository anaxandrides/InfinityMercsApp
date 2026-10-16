using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Common;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.CompanySelection.Units;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

internal static class CompanySelectionRosterWorkflow
{
    internal static TPeripheralStats? BuildMercsCompanyPeripheralStats<TPeripheralStats>(
        ViewerProfileItem profile,
        UnitProfileGroups? selectedUnitProfileGroups,
        ArmyFilterCatalog? selectedUnitFilters,
        Func<string, UnitProfile, ArmyFilterCatalog?, TPeripheralStats?> buildPeripheralStatBlock)
        where TPeripheralStats : class
    {
        var peripheralName = CompanyUnitDetailsShared.ExtractFirstPeripheralName(profile.Peripherals);
        if (string.IsNullOrWhiteSpace(peripheralName) || selectedUnitProfileGroups is not { IsEmpty: false })
        {
            return null;
        }

        var peripheralProfile = (selectedUnitProfileGroups ?? UnitProfileGroups.Empty)
            .FindPeripheralProfile(peripheralName);
        return peripheralProfile is null
            ? null
            : buildPeripheralStatBlock(peripheralName, peripheralProfile, selectedUnitFilters);
    }

    internal static int ApplyLieutenantProfileVisibility<TEntry>(
        IEnumerable<TEntry> mercsCompanyEntries,
        IEnumerable<ViewerProfileItem> profiles,
        int pointsLimit,
        int currentPoints,
        int? avaLimit,
        int? selectedUnitId,
        int? selectedUnitSourceFactionId,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        Func<TEntry, bool> readIsLieutenant,
        Func<TEntry, int> readSourceUnitId,
        Func<TEntry, int> readSourceFactionId)
    {
        var entries = mercsCompanyEntries.ToList();
        var pointsRemaining = pointsLimit - currentPoints;
        var selectedUnitCountInCompany = (!selectedUnitId.HasValue || !selectedUnitSourceFactionId.HasValue)
            ? 0
            : entries.Count(x =>
                readSourceUnitId(x) == selectedUnitId.Value &&
                readSourceFactionId(x) == selectedUnitSourceFactionId.Value);
        var avaReached = avaLimit.HasValue && selectedUnitCountInCompany >= avaLimit.Value;
        var hasActiveLieutenant = entries.Any(readIsLieutenant);
        var visibleProfiles = 0;

        foreach (var profile in profiles)
        {
            var profileCost = ProfileValues.ParseCost(profile.Cost);
            var overRemainingPoints = profileCost > pointsRemaining;
            var belowMinFilterPoints = activeUnitFilter.MinPoints.HasValue && profileCost < activeUnitFilter.MinPoints.Value;
            var aboveMaxFilterPoints = activeUnitFilter.MaxPoints.HasValue && profileCost > activeUnitFilter.MaxPoints.Value;
            var lieutenantFilteredOut = lieutenantOnlyUnits && !profile.IsLieutenant;

            profile.IsVisible = !lieutenantFilteredOut &&
                                !overRemainingPoints &&
                                !belowMinFilterPoints &&
                                !aboveMaxFilterPoints;
            profile.IsLieutenantBlocked =
                (hasActiveLieutenant && profile.IsLieutenant) ||
                overRemainingPoints ||
                avaReached;

            if (profile.IsVisible)
            {
                visibleProfiles++;
            }
        }

        return visibleProfiles;
    }

    internal static MercsCompanyEntry BuildMercsCompanyEntry(
        ArmyUnitSelectionItem selectedUnit,
        ViewerProfileItem profile,
        IReadOnlyCollection<string> selectedUnitCommonEquipment,
        IReadOnlyCollection<string> selectedUnitCommonSkills,
        string unitMov,
        string unitCc,
        string unitBs,
        string unitPh,
        string unitWip,
        string unitArm,
        string unitBts,
        string unitVitalityHeader,
        string unitVitality,
        string unitS,
        int? unitMoveFirstCm,
        int? unitMoveSecondCm,
        Func<int?, int?, string> formatMoveValue,
        PeripheralMercsCompanyStats? peripheralStats)
    {
        var combinedEquipment = CompanyProfileText.MergeCommonAndUnique(selectedUnitCommonEquipment, profile.UniqueEquipment);
        var combinedSkills = CompanyProfileText.MergeCommonAndUnique(selectedUnitCommonSkills, profile.UniqueSkills);
        var combinedCharacteristics = CompanyProfileText.SplitDisplayLine(profile.Characteristics).ToList();
        var inferredPeripheralName = CompanyUnitDetailsShared.ExtractFirstPeripheralName(profile.Peripherals);
        var peripheralHasControlModifier = ProfileHasPeripheralControlModifier(profile, inferredPeripheralName);
        if (peripheralHasControlModifier && !string.IsNullOrWhiteSpace(inferredPeripheralName))
        {
            EnsureSkill(combinedSkills, $"FT Master ({inferredPeripheralName})");
        }
        var combinedEquipmentText = CompanyProfileText.JoinOrDash(combinedEquipment);
        var combinedSkillsText = CompanyProfileText.JoinOrDash(combinedSkills);
        var combinedCharacteristicsText = CompanyProfileText.JoinOrDash(combinedCharacteristics);
        var hasInferredPeripheral = !string.IsNullOrWhiteSpace(inferredPeripheralName);
        var hasPeripheralData = peripheralStats is not null || hasInferredPeripheral;
        var peripheralNameHeading = peripheralStats?.NameHeading;
        if (string.IsNullOrWhiteSpace(peripheralNameHeading) && hasInferredPeripheral)
        {
            peripheralNameHeading = $"Peripheral: {inferredPeripheralName}";
        }

        var peripheralMov = peripheralStats is null
            ? (profile.HasPeripheralStatBlock ? profile.PeripheralMov : "-")
            : formatMoveValue(peripheralStats.MoveFirstCm, peripheralStats.MoveSecondCm);
        var peripheralCc = peripheralStats?.Cc ?? (profile.HasPeripheralStatBlock ? profile.PeripheralCc : "-");
        var peripheralBs = peripheralStats?.Bs ?? (profile.HasPeripheralStatBlock ? profile.PeripheralBs : "-");
        var peripheralPh = peripheralStats?.Ph ?? (profile.HasPeripheralStatBlock ? profile.PeripheralPh : "-");
        var peripheralWip = peripheralStats?.Wip ?? (profile.HasPeripheralStatBlock ? profile.PeripheralWip : "-");
        var peripheralArm = peripheralStats?.Arm ?? (profile.HasPeripheralStatBlock ? profile.PeripheralArm : "-");
        var peripheralBts = peripheralStats?.Bts ?? (profile.HasPeripheralStatBlock ? profile.PeripheralBts : "-");
        var peripheralVitalityHeader = peripheralStats?.VitalityHeader ?? (profile.HasPeripheralStatBlock ? profile.PeripheralVitalityHeader : Stat.Vita.Key());
        var peripheralVitality = peripheralStats?.Vitality ?? (profile.HasPeripheralStatBlock ? profile.PeripheralVitality : "-");
        var peripheralS = peripheralStats?.S ?? (profile.HasPeripheralStatBlock ? profile.PeripheralS : "-");
        var peripheralAva = peripheralStats?.Ava ?? (profile.HasPeripheralStatBlock ? profile.PeripheralAva : "-");
        var peripheralEquipment = peripheralStats?.Equipment ?? "-";
        var peripheralSkillsText = peripheralStats?.Skills ?? "-";
        var peripheralCharacteristicsText = peripheralStats?.Characteristics ?? "-";
        var peripheralRangedWeapons = peripheralStats?.RangedWeapons ?? "-";
        var peripheralCcWeapons = peripheralStats?.CcWeapons ?? "-";
        if (profile.PeripheralIsIrregular || peripheralHasControlModifier)
        {
            var peripheralSkillLines = CompanyProfileText.SplitDisplayLine(peripheralSkillsText).ToList();
            if (!peripheralSkillLines.Any(x => string.Equals(x, "Irregular", StringComparison.OrdinalIgnoreCase)))
            {
                peripheralSkillLines.Add("Irregular");
            }

            peripheralSkillsText = CompanyProfileText.JoinOrDash(peripheralSkillLines);

            var peripheralCharacteristicLines = CompanyProfileText.SplitDisplayLine(peripheralCharacteristicsText).ToList();
            if (!peripheralCharacteristicLines.Any(x => string.Equals(x, "Irregular", StringComparison.OrdinalIgnoreCase)))
            {
                peripheralCharacteristicLines.Add("Irregular");
            }

            peripheralCharacteristicsText = CompanyProfileText.JoinOrDash(peripheralCharacteristicLines);
        }
        var currentUnitMove = formatMoveValue(unitMoveFirstCm, unitMoveSecondCm);
        var statline =
            $"MOV {unitMov} | CC {unitCc} | BS {unitBs} | PH {unitPh} | WIP {unitWip} | ARM {unitArm} | BTS {unitBts} | {unitVitalityHeader} {unitVitality} | S {unitS}";

        return new MercsCompanyEntry
        {
            Name = profile.Name,
            BaseUnitName = selectedUnit.Name,
            NameFormatted = profile.NameFormatted ?? CompanyProfileFormatting.BuildNameFormatted(profile.Name),
            Subtitle = statline,
            UnitTypeCode = CompanyStartSharedState.ExtractUnitTypeCode(selectedUnit.Subtitle),
            CostDisplay = $"C {profile.Cost}",
            CostValue = ProfileValues.ParseCost(profile.Cost),
            IsLieutenant = profile.IsLieutenant,
            IsSpecOps = selectedUnit.IsSpecOps,
            ProfileKey = profile.ProfileKey,
            SourceUnitId = selectedUnit.Id,
            SourceFactionId = selectedUnit.SourceFactionId,
            LogoSourceFactionId = selectedUnit.LogoSourceFactionId > 0 ? selectedUnit.LogoSourceFactionId : selectedUnit.SourceFactionId,
            LogoSourceUnitId = selectedUnit.LogoSourceUnitId > 0 ? selectedUnit.LogoSourceUnitId : selectedUnit.Id,
            CachedLogoPath = selectedUnit.CachedLogoPath,
            PackagedLogoPath = selectedUnit.PackagedLogoPath,
            SavedEquipment = combinedEquipmentText,
            SavedSkills = combinedSkillsText,
            SavedCharacteristics = combinedCharacteristicsText,
            SavedRangedWeapons = profile.RangedWeapons,
            SavedCcWeapons = profile.MeleeWeapons,
            ExperiencePoints = 0,
            EquipmentLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Equipment", combinedEquipmentText, Color.FromArgb("#06B6D4")),
            HasEquipmentLine = combinedEquipment.Count > 0,
            SkillsLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Skills", combinedSkillsText, Color.FromArgb("#F59E0B")),
            HasSkillsLine = combinedSkills.Count > 0,
            RangedLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Ranged Weapons", profile.RangedWeapons, Color.FromArgb("#EF4444")),
            CcLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("CC Weapons", profile.MeleeWeapons, Color.FromArgb("#22C55E")),
            HasPeripheralStatBlock = hasPeripheralData,
            PeripheralNameHeading = peripheralNameHeading ?? string.Empty,
            PeripheralMov = peripheralMov,
            PeripheralCc = peripheralCc,
            PeripheralBs = peripheralBs,
            PeripheralPh = peripheralPh,
            PeripheralWip = peripheralWip,
            PeripheralArm = peripheralArm,
            PeripheralBts = peripheralBts,
            PeripheralVitalityHeader = peripheralVitalityHeader,
            PeripheralVitality = peripheralVitality,
            PeripheralS = peripheralS,
            PeripheralAva = peripheralAva,
            SavedPeripheralEquipment = peripheralEquipment,
            SavedPeripheralSkills = peripheralSkillsText,
            SavedPeripheralCharacteristics = peripheralCharacteristicsText,
            SavedPeripheralRangedWeapons = peripheralRangedWeapons,
            SavedPeripheralCcWeapons = peripheralCcWeapons,
            PeripheralEquipmentLineFormatted =
                CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Equipment", peripheralEquipment, Color.FromArgb("#06B6D4")),
            HasPeripheralEquipmentLine =
                hasPeripheralData && !string.IsNullOrWhiteSpace(peripheralEquipment) && peripheralEquipment != "-",
            PeripheralSkillsLineFormatted =
                CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Skills", peripheralSkillsText, Color.FromArgb("#F59E0B")),
            HasPeripheralSkillsLine =
                hasPeripheralData && !string.IsNullOrWhiteSpace(peripheralSkillsText) && peripheralSkillsText != "-",
            UnitMoveFirstCm = unitMoveFirstCm,
            UnitMoveSecondCm = unitMoveSecondCm,
            UnitMoveDisplay = currentUnitMove,
            PeripheralMoveFirstCm = peripheralStats?.MoveFirstCm,
            PeripheralMoveSecondCm = peripheralStats?.MoveSecondCm
        };
    }

    private static void EnsureSkill(ICollection<string> skills, string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
        {
            return;
        }

        if (skills.Any(x => string.Equals(x?.Trim(), skill, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        skills.Add(skill);
    }

    private static bool ProfileHasPeripheralControlModifier(ViewerProfileItem profile, string? peripheralName)
    {
        if (profile.PeripheralIsIrregular)
        {
            return true;
        }

        var text = string.Join(" ",
            profile.Peripherals ?? string.Empty,
            profile.PeripheralNameHeading ?? string.Empty,
            profile.PeripheralSubtitle ?? string.Empty,
            peripheralName ?? string.Empty);

        return text.Contains("synchronized", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("control", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("cyberplug", StringComparison.OrdinalIgnoreCase);
    }

    internal static ArmyUnitSelectionItem SetSelectedUnit(
        ArmyUnitSelectionItem item,
        ArmyUnitSelectionItem? currentSelectedUnit,
        bool selectionContextChanged,
        Action onContextChangedForSameSelection,
        Action<ArmyUnitSelectionItem> loadSelectedUnitLogo,
        Action<ArmyUnitSelectionItem> loadSelectedUnitDetails)
    {
        Console.WriteLine($"CompanySelectionPage SetSelectedUnit requested: id={item.Id}, faction={item.SourceFactionId}, name='{item.Name}'.");
        if (currentSelectedUnit == item)
        {
            if (selectionContextChanged)
            {
                Console.WriteLine("CompanySelectionPage SetSelectedUnit context changed; reloading selected unit details.");
                onContextChangedForSameSelection();
            }
            else
            {
                Console.WriteLine("CompanySelectionPage SetSelectedUnit skipped (same item instance).");
            }

            return currentSelectedUnit;
        }

        if (currentSelectedUnit is not null)
        {
            currentSelectedUnit.IsSelected = false;
        }

        item.IsSelected = true;
        Console.WriteLine($"CompanySelectionPage selected unit now: id={item.Id}, faction={item.SourceFactionId}, name='{item.Name}'.");
        loadSelectedUnitLogo(item);
        loadSelectedUnitDetails(item);
        return item;
    }

    internal static int ApplyLieutenantVisualStates<TEntry, TUnit>(
        IEnumerable<TEntry> mercsCompanyEntries,
        IEnumerable<ViewerProfileItem> profiles,
        string selectedStartSeasonPoints,
        string seasonPointsCapText,
        string unitAva,
        TUnit? selectedUnit,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        Func<TEntry, bool> readIsLieutenant,
        Func<TEntry, int> readSourceUnitId,
        Func<TEntry, int> readSourceFactionId,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readSourceFactionIdFromUnit)
    {
        var pointsLimit = int.TryParse(selectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(seasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        var avaLimit = CompanySelectionSharedUtilities.ParseAvaLimit(unitAva);
        return ApplyLieutenantProfileVisibility(
            mercsCompanyEntries,
            profiles,
            pointsLimit,
            currentPoints,
            avaLimit,
            selectedUnit is null ? null : readUnitId(selectedUnit),
            selectedUnit is null ? null : readSourceFactionIdFromUnit(selectedUnit),
            activeUnitFilter,
            lieutenantOnlyUnits,
            readIsLieutenant,
            readSourceUnitId,
            readSourceFactionId);
    }

    internal static void RemoveMercsCompanyEntry<TEntry>(
        TEntry? entry,
        ICollection<TEntry> mercsCompanyEntries,
        Action updateMercsCompanyTotal,
        Action postEntryMutation,
        Action applyUnitVisibilityFilters)
        where TEntry : class
    {
        if (entry is null)
        {
            return;
        }

        mercsCompanyEntries.Remove(entry);
        updateMercsCompanyTotal();
        postEntryMutation();
        applyUnitVisibilityFilters();
    }

    internal static void SelectMercsCompanyEntry<TEntry>(
        TEntry? entry,
        IReadOnlyList<ArmyUnitSelectionItem> units,
        Func<int, int, CancellationToken, ArmyUnitRecord?> getUnitFromProvider,
        Func<int, int, string, string?, string?, ArmyUnitSelectionItem> createFallbackUnit,
        Action<ArmyUnitSelectionItem> setSelectedUnit,
        Action<CancellationToken> loadSelectedUnitDetails,
        CancellationToken cancellationToken = default)
        where TEntry : class, ICompanyMercsEntry
    {
        if (entry is null)
        {
            return;
        }

        try
        {
            var unitItem = units.FirstOrDefault(x =>
                x.Id == entry.SourceUnitId &&
                x.SourceFactionId == entry.SourceFactionId);

            if (unitItem is null)
            {
                var unitRecord = getUnitFromProvider(entry.SourceFactionId, entry.SourceUnitId, cancellationToken);
                var unitName = !string.IsNullOrWhiteSpace(unitRecord?.Name)
                    ? unitRecord.Name
                    : entry.Name;

                unitItem = createFallbackUnit(
                    entry.SourceUnitId,
                    entry.SourceFactionId,
                    unitName,
                    entry.CachedLogoPath,
                    entry.PackagedLogoPath);
            }

            setSelectedUnit(unitItem);
            loadSelectedUnitDetails(cancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanySelectionPage SelectMercsCompanyEntry failed: {ex.Message}");
        }
    }
}
