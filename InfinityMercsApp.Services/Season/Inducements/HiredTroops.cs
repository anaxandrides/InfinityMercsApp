using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Inducements;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanyProfiles;

namespace InfinityMercsApp.Services.Season.Inducements;

/// <summary>
/// Finds the Troops for Hire in the army data: each row's unit, from the first army that fields it (a vanilla army
/// before a sectorial), and the loadouts the row allows. Lieutenant loadouts are left out, since the company has its
/// captain.
/// </summary>
public sealed class HiredTroops(IArmyDataService armyData)
{
    private readonly Dictionary<string, Unit?> _units = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The loadouts the row can be hired in; none when no army has the unit.</summary>
    public IReadOnlyList<HiredTroopChoice> ChoicesFor(HiredTroopOffer offer)
    {
        if (UnitFor(offer.UnitName) is not { } unit || armyData.GetFactionSnapshot(unit.FactionId)?.Filters is not { } filters)
        {
            return [];
        }

        var names = new ProfileEntryNames(filters.Extras, showUnitsInInches: false);
        var weaponNames = filters.Names(ArmyFilterSection.Weapons);
        var skillNames = filters.Names(ArmyFilterSection.Skills);
        var unitName = UnitDisplayName(unit);
        return unit.ProfileGroups.AllOptions()
            .Where(grouped => grouped.Group.Id is not null && grouped.Option.Id is not null &&
                              !grouped.Option.Disabled && !grouped.Option.IsLieutenant(skillNames))
            .Select(grouped => new
            {
                grouped.Group,
                grouped.Option,
                Weapons = names.OrderedUnique(unit.ProfileGroups.GetEntriesWithIncludes(grouped.Option, ProfileEntryKind.Weapons), weaponNames)
            })
            .Where(loadout => offer.Allows(loadout.Weapons))
            .Select(loadout => new HiredTroopChoice(
                unit.FactionId,
                unit.UnitId,
                loadout.Group.Id ?? 0,
                loadout.Option.Id ?? 0,
                loadout.Weapons.Count == 0 ? unitName : $"{unitName}: {string.Join(", ", loadout.Weapons)}"))
            .DistinctBy(choice => choice.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>The hired troop's profile in its loadout, with the skills its row adds; null when the army data lacks it.</summary>
    public HiredTrooperProfile? ProfileOf(HiredTroop troop, bool showUnitsInInches)
    {
        if (armyData.GetUnit(troop.FactionId, troop.UnitId) is not { } unit ||
            armyData.GetFactionSnapshot(troop.FactionId)?.Filters is not { } filters ||
            unit.ProfileGroups.Groups.FirstOrDefault(group => group.Id == troop.GroupId) is not { Profiles.Count: > 0 } group ||
            unit.ProfileGroups.FindOption(troop.GroupId, troop.OptionId) is not { } option)
        {
            return null;
        }

        var profile = group.Profiles[0];
        var names = new ProfileEntryNames(filters.Extras, showUnitsInInches);
        List<string> Named(ProfileEntryKind kind, string section) => names.OrderedUnique(
            profile.Entries(kind).Concat(unit.ProfileGroups.GetEntriesWithIncludes(option, kind)),
            filters.Names(section));

        var weapons = Named(ProfileEntryKind.Weapons, ArmyFilterSection.Weapons);
        var orders = UnitTraits.ReadOrderTraits(option);
        return new HiredTrooperProfile(
            Movement.Format(profile.Movement, showUnitsInInches),
            UnitProfile.StatText(profile.Cc),
            UnitProfile.StatText(profile.Bs),
            UnitProfile.StatText(profile.Ph),
            UnitProfile.StatText(profile.Wip),
            UnitProfile.StatText(profile.Arm),
            UnitProfile.StatText(profile.Bts),
            profile.Vitality,
            UnitProfile.StatText(profile.S),
            [.. Named(ProfileEntryKind.Skills, ArmyFilterSection.Skills).Concat(troop.Skills).Distinct(StringComparer.OrdinalIgnoreCase)],
            Named(ProfileEntryKind.Equip, ArmyFilterSection.Equip),
            weapons.Where(weapon => !CompanyProfileText.IsMeleeWeaponName(weapon)).ToList(),
            weapons.Where(CompanyProfileText.IsMeleeWeaponName).ToList(),
            orders.HasIrregular && !orders.HasRegular,
            orders.HasImpetuous);
    }

    // The unit's name as the army's ISC gives it, e.g. "Diggers, Armed Prospectors".
    private static string UnitDisplayName(Unit unit) => string.IsNullOrWhiteSpace(unit.Isc) ? unit.Name : unit.Isc;

    // The first army unit whose name starts with the row's; armies are searched vanilla first, and the answer is kept.
    private Unit? UnitFor(string unitName)
    {
        if (_units.TryGetValue(unitName, out var known))
        {
            return known;
        }

        var unit = armyData.GetMetadataFactions()
            .OrderByDescending(faction => faction.ParentId == faction.Id)
            .ThenBy(faction => faction.Id)
            .Select(faction => armyData.GetResumeByFaction(faction.Id)
                .FirstOrDefault(resume => resume.Name.StartsWith(unitName, StringComparison.OrdinalIgnoreCase)) is { } resume
                ? armyData.GetUnit(faction.Id, resume.UnitId)
                : null)
            .FirstOrDefault(found => found is { ProfileGroups.IsEmpty: false });
        _units[unitName] = unit;
        return unit;
    }
}
