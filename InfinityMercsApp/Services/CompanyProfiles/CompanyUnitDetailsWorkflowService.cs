using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Traits;

namespace InfinityMercsApp.Services.CompanyProfiles;

internal static class CompanyUnitDetailsWorkflowService
{
    /// <summary>
    /// Runs the shared selected-unit details workflow and applies outputs via callbacks.
    /// </summary>
    public static void Load(
        CompanyUnitDetailsWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        request.LogInfo($"ArmyFactionSelectionPage LoadSelectedUnitDetails started: id={request.UnitId}, faction={request.SourceFactionId}, name='{request.UnitName}'.");
        request.SetUnitNameHeading(request.UnitName);

        var unit = request.GetUnit(request.SourceFactionId, request.UnitId, cancellationToken);
        InfinityMercsApp.Domain.Models.Army.SpecOps.SpecopsUnit? specopsUnit = null;
        if (request.IsSpecOps || unit is null)
        {
            var specopsUnits = request.GetSpecopsUnitsByFaction(request.SourceFactionId, cancellationToken);
            specopsUnit = specopsUnits.FirstOrDefault(x => x.UnitId == request.UnitId);
        }

        var treatAsSpecOps = request.IsSpecOps || (unit is null && specopsUnit is not null);
        request.ApplyUnitHeaderColors(request.SourceFactionId, unit, cancellationToken);

        var profileGroups = unit?.ProfileGroups ?? UnitProfileGroups.Empty;
        if (treatAsSpecOps && specopsUnit is { ProfileGroups.IsEmpty: false })
        {
            profileGroups = specopsUnit.ProfileGroups;
        }
        else if (profileGroups.IsEmpty)
        {
            profileGroups = specopsUnit?.ProfileGroups ?? UnitProfileGroups.Empty;
        }

        var snapshot = request.GetFactionSnapshot(request.SourceFactionId, cancellationToken);
        if (profileGroups.IsEmpty)
        {
            request.LogError($"ArmyFactionSelectionPage: profile groups not found for faction={request.SourceFactionId}, unit={request.UnitId}.");
            return;
        }

        var filters = snapshot?.Filters ?? ArmyFilterCatalog.Empty;
        var equipLookup = filters.Names(ArmyFilterSection.Equip);
        var skillsLookup = filters.Names(ArmyFilterSection.Skills);
        var charsLookup = filters.Names(ArmyFilterSection.Chars);
        var entryNames = new ProfileEntryNames(filters.Extras, request.ShowUnitsInInches);
        request.SetSelectedUnitProfileGroups(profileGroups);
        request.SetSelectedUnitFilters(filters);
        request.ApplyGlobalDisplayUnitsPreference(cancellationToken);

        var visibleOptions = profileGroups.AllOptions()
            .Select(x => x.Option)
            .Where(option => !option.CostsSwc)
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
            : stableSkills.Where(x => !LieutenantRules.Mentions(x)).ToList();

        request.SetCommonEquipmentSkills(stableEquip, stableSkills, treatAsSpecOps);
        request.LogInfo(
            $"ArmyFactionSelectionPage summary extraction: unit='{request.UnitName}', options={visibleOptions.Count}, commonEquip={stableEquip.Count}, commonSkills={stableSkills.Count}.");

        request.SetSummaryText(
            $"Equipment: {(stableEquip.Count == 0 ? "-" : string.Join(", ", stableEquip))}",
            $"Special Skills: {(stableSkills.Count == 0 ? "-" : string.Join(", ", stableSkills))}");
        request.RefreshSummaryFormatted();
        request.PopulateProfilesFromProfileGroups(profileGroups, filters, treatAsSpecOps);
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
