using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanyProfiles;

namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>Reads what a selection screen shows for a picked unit from the army data.</summary>
public sealed class UnitDetailsLoader(IArmyDataService armyData, ISpecOpsProvider specOps)
{
    /// <summary>
    /// The unit's details; a Spec-Ops unit (or one the faction's units don't list) uses its Spec-Ops profiles.
    /// With <paramref name="lieutenantOnly"/>, the shared equipment and skills come from lieutenant options alone.
    /// </summary>
    /// <remarks>A unit with no profiles at all comes back with empty <see cref="UnitDetails.ProfileGroups"/>.</remarks>
    public UnitDetails Load(
        int factionId,
        int unitId,
        bool isSpecOps,
        bool lieutenantOnly,
        bool showUnitsInInches,
        CancellationToken cancellationToken = default)
    {
        var unit = armyData.GetUnit(factionId, unitId, cancellationToken);
        var specopsUnit = isSpecOps || unit is null
            ? specOps.GetSpecopsUnitsByFaction(factionId, cancellationToken).FirstOrDefault(x => x.UnitId == unitId)
            : null;
        var treatAsSpecOps = isSpecOps || (unit is null && specopsUnit is not null);

        var profileGroups = unit?.ProfileGroups ?? UnitProfileGroups.Empty;
        if (treatAsSpecOps && specopsUnit is { ProfileGroups.IsEmpty: false })
        {
            profileGroups = specopsUnit.ProfileGroups;
        }
        else if (profileGroups.IsEmpty)
        {
            profileGroups = specopsUnit?.ProfileGroups ?? UnitProfileGroups.Empty;
        }

        var filters = armyData.GetFactionSnapshot(factionId, cancellationToken)?.Filters ?? ArmyFilterCatalog.Empty;
        var equipLookup = filters.Names(ArmyFilterSection.Equip);
        var skillsLookup = filters.Names(ArmyFilterSection.Skills);
        var charsLookup = filters.Names(ArmyFilterSection.Chars);
        var entryNames = new ProfileEntryNames(filters.Extras, showUnitsInInches);

        var visibleOptions = profileGroups.AllOptions()
            .Select(x => x.Option)
            .Where(option => !option.CostsSwc)
            .Where(option => treatAsSpecOps || !lieutenantOnly || option.IsLieutenant(skillsLookup))
            .ToList();

        var commonEquipment = CommonNames(profileGroups, visibleOptions, ProfileEntryKind.Equip, equipLookup, entryNames);
        var commonSkills = CommonNames(profileGroups, visibleOptions, ProfileEntryKind.Skills, skillsLookup, entryNames);
        commonSkills = treatAsSpecOps
            ? CompanyProfileText.EnsureLieutenantSkill(commonSkills)
            : commonSkills.Where(x => !LieutenantRules.Mentions(x)).ToList();

        return new UnitDetails(
            unit,
            profileGroups,
            filters,
            treatAsSpecOps,
            UnitTraits.ReadOrderTraits(profileGroups),
            UnitTraits.ReadTechTraits(profileGroups, equipLookup, skillsLookup, charsLookup),
            commonEquipment,
            commonSkills);
    }

    /// <summary>
    /// The unit's AVA as the factions list it, the highest when more than one does (a unit in both chosen
    /// sectorials uses the higher AVA); null when none lists it.
    /// </summary>
    public string? HighestAva(int unitId, IEnumerable<int> factionIds, CancellationToken cancellationToken = default) =>
        factionIds
            .Select(factionId => armyData.GetUnit(factionId, unitId, cancellationToken)?.ProfileGroups.Groups
                .SelectMany(group => group.Profiles)
                .FirstOrDefault())
            .OfType<UnitProfile>()
            .Where(profile => profile.Ava is >= 0)
            .MaxBy(profile => profile.Ava)
            ?.AvaText;

    // Names every profile shares plus names every visible option shares, distinct and sorted.
    private static List<string> CommonNames(
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
