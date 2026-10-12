using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.Services;

internal sealed class CompanyUnitDetailsWorkflowRequest
{
    public required string UnitName { get; init; }
    public required int UnitId { get; init; }
    public required int SourceFactionId { get; init; }
    public required bool IsSpecOps { get; init; }
    public required bool LieutenantOnlyUnits { get; init; }
    public required bool ShowUnitsInInches { get; init; }
    public required Func<int, int, CancellationToken, InfinityMercsApp.Domain.Models.Army.Unit?> GetUnit { get; init; }
    public required Func<int, CancellationToken, InfinityMercsApp.Domain.Models.Army.Faction?> GetFactionSnapshot { get; init; }
    public required Func<int, CancellationToken, Task<IReadOnlyList<InfinityMercsApp.Domain.Models.Army.SpecopsUnit>>> GetSpecopsUnitsByFactionAsync { get; init; }
    public required Func<int, InfinityMercsApp.Domain.Models.Army.Unit?, CancellationToken, Task> ApplyUnitHeaderColorsAsync { get; init; }
    public required Func<CancellationToken, Task> ApplyGlobalDisplayUnitsPreferenceAsync { get; init; }
    public required Action<UnitProfileGroups> PopulateUnitStatsFromFirstProfile { get; init; }
    public required Action<bool, bool, bool, bool> SetOrderTraits { get; init; }
    public required Action<bool, bool, bool> SetTechTraits { get; init; }
    public required Func<IEnumerable<string>, List<string>> EnsureLieutenantSkill { get; init; }
    public required Action<IReadOnlyList<string>, IReadOnlyList<string>, bool> SetCommonEquipmentSkills { get; init; }
    public required Action<string, string> SetSummaryText { get; init; }
    public required Action RefreshSummaryFormatted { get; init; }
    public required Action<UnitProfileGroups, string?, bool> PopulateProfilesFromProfileGroups { get; init; }
    public required Action UpdatePeripheralStatBlockFromVisibleProfiles { get; init; }
    public required Action<string?> SetSelectedUnitProfileGroupsJson { get; init; }
    public required Action<string?> SetSelectedUnitFiltersJson { get; init; }
    public required Action<string> SetUnitNameHeading { get; init; }
    public required Action<bool> SetSummaryHighlightLieutenant { get; init; }
    public required Action<string> LogInfo { get; init; }
    public required Action<string> LogError { get; init; }
}

internal static class CompanyUnitDetailsWorkflowService
{
    /// <summary>
    /// Runs the shared selected-unit details workflow and applies outputs via callbacks.
    /// </summary>
    public static async Task LoadAsync(
        CompanyUnitDetailsWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        request.LogInfo($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync started: id={request.UnitId}, faction={request.SourceFactionId}, name='{request.UnitName}'.");
        request.SetUnitNameHeading(request.UnitName);

        var unit = request.GetUnit(request.SourceFactionId, request.UnitId, cancellationToken);
        InfinityMercsApp.Domain.Models.Army.SpecopsUnit? specopsUnit = null;
        if (request.IsSpecOps || unit is null)
        {
            var specopsUnits = await request.GetSpecopsUnitsByFactionAsync(request.SourceFactionId, cancellationToken);
            specopsUnit = specopsUnits.FirstOrDefault(x => x.UnitId == request.UnitId);
        }

        var treatAsSpecOps = request.IsSpecOps || (unit is null && specopsUnit is not null);
        await request.ApplyUnitHeaderColorsAsync(request.SourceFactionId, unit, cancellationToken);

        var profileGroupsJson = unit?.ProfileGroupsJson;
        if (treatAsSpecOps && !string.IsNullOrWhiteSpace(specopsUnit?.ProfileGroupsJson))
        {
            profileGroupsJson = specopsUnit.ProfileGroupsJson;
        }
        else if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            profileGroupsJson = specopsUnit?.ProfileGroupsJson;
        }

        var snapshot = request.GetFactionSnapshot(request.SourceFactionId, cancellationToken);
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            request.LogError($"ArmyFactionSelectionPage: profile groups not found for faction={request.SourceFactionId}, unit={request.UnitId}.");
            return;
        }

        var reader = ArmyJsonReader.Shared;
        var filters = reader.ReadArmyFilters(snapshot?.FiltersJson);
        var equipLookup = filters.Names(ArmyFilterSection.Equip);
        var skillsLookup = filters.Names(ArmyFilterSection.Skills);
        var charsLookup = filters.Names(ArmyFilterSection.Chars);
        var entryNames = new ProfileEntryNames(filters.Extras, request.ShowUnitsInInches);
        request.SetSelectedUnitProfileGroupsJson(profileGroupsJson);
        request.SetSelectedUnitFiltersJson(snapshot?.FiltersJson);
        await request.ApplyGlobalDisplayUnitsPreferenceAsync(cancellationToken);

        var profileGroups = reader.ReadProfileGroups(profileGroupsJson);
        var visibleOptions = profileGroups.AllOptions()
            .Select(x => x.Option)
            .Where(option => !option.HasPositiveSwc)
            .Where(option => !treatAsSpecOps && request.LieutenantOnlyUnits ? option.IsLieutenant(skillsLookup) : true)
            .ToList();

        request.PopulateUnitStatsFromFirstProfile(profileGroups);
        var orderTraits = UnitTraits.ReadOrderTraits(profileGroups);
        request.SetOrderTraits(orderTraits.HasRegular, orderTraits.HasIrregular, orderTraits.HasImpetuous, orderTraits.HasTacticalAwareness);

        var techTraits = UnitTraits.ReadTechTraits(profileGroups, equipLookup, skillsLookup, charsLookup);
        request.SetTechTraits(techTraits.HasCube, techTraits.HasCube2, techTraits.HasHackable);

        var stableEquip = CommonNames(profileGroups, visibleOptions, ProfileEntryKind.Equip, equipLookup, entryNames);
        var stableSkills = CommonNames(profileGroups, visibleOptions, ProfileEntryKind.Skills, skillsLookup, entryNames);

        stableSkills = treatAsSpecOps
            ? request.EnsureLieutenantSkill(stableSkills)
            : stableSkills.Where(x => !x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase)).ToList();

        request.SetCommonEquipmentSkills(stableEquip, stableSkills, treatAsSpecOps);
        request.LogInfo(
            $"ArmyFactionSelectionPage summary extraction: unit='{request.UnitName}', options={visibleOptions.Count}, commonEquip={stableEquip.Count}, commonSkills={stableSkills.Count}.");

        request.SetSummaryText(
            $"Equipment: {(stableEquip.Count == 0 ? "-" : string.Join(", ", stableEquip))}",
            $"Special Skills: {(stableSkills.Count == 0 ? "-" : string.Join(", ", stableSkills))}");
        request.RefreshSummaryFormatted();
        request.PopulateProfilesFromProfileGroups(profileGroups, snapshot?.FiltersJson, treatAsSpecOps);
        request.UpdatePeripheralStatBlockFromVisibleProfiles();
    }

    /// <summary>
    /// Names every profile shares plus names every visible option shares, distinct and sorted.
    /// </summary>
    internal static List<string> CommonNames(
        UnitProfileGroups profileGroups,
        IReadOnlyList<ProfileOption> visibleOptions,
        ProfileEntryKind kind,
        IReadOnlyDictionary<int, string> lookup,
        ProfileEntryNames entryNames)
    {
        var fromProfiles = entryNames.CommonAcrossProfiles(profileGroups, kind, lookup);
        var fromVisibleOptions = visibleOptions.Count > 0
            ? entryNames.CommonAcrossOptions(profileGroups, visibleOptions, kind, lookup)
            : [];
        return fromProfiles
            .Concat(fromVisibleOptions)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
