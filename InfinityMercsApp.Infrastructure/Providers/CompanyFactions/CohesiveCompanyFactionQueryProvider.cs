using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Data;

namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <inheritdoc/>
public sealed class CohesiveCompanyFactionQueryProvider(IFactionProvider factionProvider) : ICohesiveCompanyFactionQueryProvider
{
    /// <inheritdoc/>
    public CohesiveCompanyFactionQueryResult GetFilterQuerySource(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedFactionIds = factionIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (normalizedFactionIds.Count == 0)
        {
            return new CohesiveCompanyFactionQueryResult();
        }

        var typeLookup = new Dictionary<int, string>();
        var charsLookup = new Dictionary<int, string>();
        var skillsLookup = new Dictionary<int, string>();
        var equipLookup = new Dictionary<int, string>();
        var weaponsLookup = new Dictionary<int, string>();
        var ammoLookup = new Dictionary<int, string>();

        foreach (var factionId in normalizedFactionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var filters = factionProvider.GetFactionSnapshot(factionId)?.Filters;
            if (filters is null)
            {
                continue;
            }

            MergeLookup(typeLookup, filters.Names(ArmyFilterSection.Type));
            MergeLookup(charsLookup, filters.Names(ArmyFilterSection.Chars));
            MergeLookup(skillsLookup, filters.Names(ArmyFilterSection.Skills));
            MergeLookup(equipLookup, filters.Names(ArmyFilterSection.Equip));
            MergeLookup(weaponsLookup, filters.Names(ArmyFilterSection.Weapons));
            MergeLookup(ammoLookup, filters.Names(ArmyFilterSection.Ammunition));
        }

        var mergedEntries = GetMergedMercsArmyList(normalizedFactionIds, cancellationToken);
        return new CohesiveCompanyFactionQueryResult
        {
            TypeLookup = typeLookup,
            CharacteristicsLookup = charsLookup,
            SkillsLookup = skillsLookup,
            EquipmentLookup = equipLookup,
            WeaponsLookup = weaponsLookup,
            AmmoLookup = ammoLookup,
            MergedMercsListEntries = mergedEntries
        };
    }

    private IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionIds.Count == 0)
        {
            return [];
        }

        var normalizedFactionIds = factionIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (normalizedFactionIds.Count == 0)
        {
            return [];
        }

        var resultSets = normalizedFactionIds
            .Select(factionProvider.GetResumeByFactionMercsOnly)
            .ToList();
        var unitLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, Unit>>();
        for (var setIndex = 0; setIndex < resultSets.Count; setIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var factionId = normalizedFactionIds[setIndex];
            var unitIds = resultSets[setIndex]
                .Select(x => x.UnitId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
            var lookup = new Dictionary<int, Unit>();
            foreach (var id in unitIds)
            {
                if (factionProvider.GetUnit(factionId, id) is { } unit)
                {
                    lookup[id] = unit;
                }
            }

            unitLookupByFaction[factionId] = lookup;
        }

        var mergedByUnitName = new Dictionary<string, MutableMercsArmyListEntry>(StringComparer.OrdinalIgnoreCase);
        for (var setIndex = 0; setIndex < resultSets.Count; setIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var factionId = normalizedFactionIds[setIndex];
            var rows = resultSets[setIndex];
            unitLookupByFaction.TryGetValue(factionId, out var unitLookup);
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    continue;
                }

                var key = row.Name.Trim();
                if (!mergedByUnitName.TryGetValue(key, out var merged))
                {
                    var unitRecord = TryGetUnit(unitLookup, row.UnitId);
                    mergedByUnitName[key] = new MutableMercsArmyListEntry
                    {
                        Resume = CloneResume(row),
                        ProfileGroups = unitRecord?.ProfileGroups ?? UnitProfileGroups.Empty,
                        SourceFactionIds = new HashSet<int> { factionId }
                    };
                    continue;
                }

                merged.SourceFactionIds.Add(factionId);

                var incomingUnitRecord = TryGetUnit(unitLookup, row.UnitId);
                merged.ProfileGroups = MergeProfileGroups(merged.ProfileGroups, incomingUnitRecord?.ProfileGroups ?? UnitProfileGroups.Empty);
            }
        }

        var sorted = ArmyUnitSort
            .OrderByUnitTypeAndName(mergedByUnitName.Values, x => x.Resume.Type, x => x.Resume.Name)
            .ToList();

        return sorted
            .Select(x => new MercsArmyListEntry
            {
                Resume = x.Resume,
                ProfileGroups = x.ProfileGroups,
                SourceFactionIds = x.SourceFactionIds.Order().ToList()
            })
            .ToList();
    }

    private static Unit? TryGetUnit(IReadOnlyDictionary<int, Unit>? lookup, int unitId)
    {
        if (lookup is null || unitId <= 0 || !lookup.TryGetValue(unitId, out var row))
        {
            return null;
        }

        return row;
    }

    private static Resume CloneResume(Resume source)
    {
        return new Resume
        {
            ResumeKey = source.ResumeKey,
            FactionId = source.FactionId,
            UnitId = source.UnitId,
            IdArmy = source.IdArmy,
            Isc = source.Isc,
            Name = source.Name,
            Slug = source.Slug,
            Logo = source.Logo,
            Type = source.Type,
            Category = source.Category
        };
    }

    /// <summary>
    /// Merges the same unit's profile groups from another army: groups match by id; a matched group gains the
    /// incoming profiles it lacks (by id and name) and the incoming options it lacks (by content).
    /// </summary>
    private static UnitProfileGroups MergeProfileGroups(UnitProfileGroups current, UnitProfileGroups incoming)
    {
        if (current.IsEmpty)
        {
            return incoming;
        }

        if (incoming.IsEmpty)
        {
            return current;
        }

        var merged = current.Groups.Select(group => new MergingGroup(group)).ToList();
        var byGroupKey = new Dictionary<string, MergingGroup>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in merged)
        {
            byGroupKey[GetGroupKey(group.Source)] = group;
        }

        foreach (var incomingGroup in incoming.Groups)
        {
            var key = GetGroupKey(incomingGroup);
            if (!byGroupKey.TryGetValue(key, out var existing))
            {
                var added = new MergingGroup(incomingGroup);
                byGroupKey[key] = added;
                merged.Add(added);
                continue;
            }

            existing.AddProfilesAndOptions(incomingGroup);
        }

        return new UnitProfileGroups(merged.Select(group => group.Build()).ToList());
    }

    private static string GetGroupKey(ProfileGroup group) => $"{group.Id}:".Trim().ToLowerInvariant();

    private static string GetProfileKey(UnitProfile profile) => $"{profile.Id}::{profile.Name}".Trim().ToLowerInvariant();

    /// <summary>A profile group gaining another army's profiles and options.</summary>
    private sealed class MergingGroup(ProfileGroup source)
    {
        private readonly List<UnitProfile> _profiles = [.. source.Profiles];
        private readonly HashSet<string> _profileKeys = new(source.Profiles.Select(GetProfileKey), StringComparer.OrdinalIgnoreCase);
        private readonly List<ProfileOption> _options = [.. source.Options];
        private readonly HashSet<string> _optionKeys = new(source.Options.Select(ProfileFingerprint.Of), StringComparer.OrdinalIgnoreCase);
        private bool _changed;

        public ProfileGroup Source { get; } = source;

        public void AddProfilesAndOptions(ProfileGroup incoming)
        {
            foreach (var profile in incoming.Profiles)
            {
                if (_profileKeys.Add(GetProfileKey(profile)))
                {
                    _profiles.Add(profile);
                    _changed = true;
                }
            }

            foreach (var option in incoming.Options)
            {
                if (_optionKeys.Add(ProfileFingerprint.Of(option)))
                {
                    _options.Add(option);
                    _changed = true;
                }
            }
        }

        public ProfileGroup Build() => !_changed ? Source : new ProfileGroup
        {
            Id = Source.Id,
            Isc = Source.Isc,
            Category = Source.Category,
            Profiles = _profiles,
            Options = _options
        };
    }

    private static void MergeLookup(Dictionary<int, string> target, IReadOnlyDictionary<int, string> source)
    {
        foreach (var pair in source)
        {
            if (target.ContainsKey(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            target[pair.Key] = pair.Value.Trim();
        }
    }

    private sealed class MutableMercsArmyListEntry
    {
        public required Resume Resume { get; init; }
        public required UnitProfileGroups ProfileGroups { get; set; }
        public required HashSet<int> SourceFactionIds { get; init; }
    }
}
