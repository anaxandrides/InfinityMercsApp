using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Data;

namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <summary>
/// Builds a generated company army (Airborne, Inspiring Leadership) from every stored army's units: keeps
/// the profile groups and options a company rule allows, merges same-named units across armies (taking the
/// highest AVA), and merges the source armies' filter catalogues.
/// </summary>
internal sealed class MergedCompanyArmyBuilder(IFactionProvider factionProvider, string logTag)
{
    /// <summary>The filter sections a generated army carries, in this order.</summary>
    private static readonly IReadOnlyList<string> MergedFilterSections =
    [
        ArmyFilterSection.Type,
        ArmyFilterSection.Chars,
        ArmyFilterSection.Skills,
        ArmyFilterSection.Equip,
        ArmyFilterSection.Weapons,
        ArmyFilterSection.Ammunition,
        ArmyFilterSection.Category,
        ArmyFilterSection.Peripheral,
        ArmyFilterSection.Extras
    ];

    /// <summary>
    /// Collects every MERCS-eligible unit of the given armies that keeps at least one profile group under
    /// <paramref name="qualifyingOptions"/>, which returns the options of a group the company allows (empty
    /// to drop the group). Units are keyed by their trimmed unit-index name.
    /// </summary>
    public Dictionary<string, MergedUnit> CollectUnits(
        IReadOnlyList<int> factionIds,
        Func<ProfileGroup, IReadOnlyList<ProfileOption>> qualifyingOptions,
        CancellationToken cancellationToken)
    {
        var mergedUnits = new Dictionary<string, MergedUnit>(StringComparer.OrdinalIgnoreCase);
        foreach (var factionId in factionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resumes = factionProvider.GetResumeByFactionMercsOnly(factionId);
            var unitLookup = factionProvider.GetUnitsByFaction(factionId).ToDictionary(unit => unit.UnitId);
            var qualified = 0;

            foreach (var resume in resumes)
            {
                if (string.IsNullOrWhiteSpace(resume.Name) || !unitLookup.TryGetValue(resume.UnitId, out var unit))
                {
                    continue;
                }

                var filtered = Filter(unit.ProfileGroups, qualifyingOptions);
                if (filtered is null)
                {
                    continue;
                }

                qualified++;
                var key = resume.Name.Trim();
                var maxAva = MaxAva(filtered);
                if (mergedUnits.TryGetValue(key, out var existing))
                {
                    // The same unit from another army: keep the version with the highest AVA.
                    existing.SourceFactionIds.Add(factionId);
                    if (maxAva > existing.MaxAva)
                    {
                        existing.MaxAva = maxAva;
                        existing.ProfileGroups = filtered;
                    }
                }
                else
                {
                    mergedUnits[key] = new MergedUnit
                    {
                        Name = resume.Name,
                        Resume = resume,
                        ProfileGroups = filtered,
                        Options = unit.Options,
                        Filters = unit.Filters,
                        Isc = unit.Isc,
                        IscAbbr = unit.IscAbbr,
                        Slug = unit.Slug,
                        MaxAva = maxAva,
                        SourceFactionId = factionId,
                        SourceUnitId = resume.UnitId,
                        SourceFactionIds = { factionId }
                    };
                }
            }

            if (qualified > 0)
            {
                Console.WriteLine($"[{logTag}]   Faction {factionId}: {resumes.Count} resumes, qualified={qualified} (total units: {mergedUnits.Count})");
            }
        }

        return mergedUnits;
    }

    /// <summary>
    /// The generated army: merged units (every profile set to the unit's highest AVA) numbered from 1 in name
    /// order, their unit index, and the source armies' merged filters. <see cref="ArmyDefinition.Version"/> is
    /// the content hash from <see cref="ComputeVersionHash"/>.
    /// </summary>
    public ArmyDefinition BuildArmy(Dictionary<string, MergedUnit> mergedUnits, IReadOnlyList<int> sourceFactionIds)
    {
        var units = new List<ArmyUnitDefinition>();
        var resumes = new List<ArmyImportResume>();
        var unitId = 1;

        foreach (var entry in mergedUnits.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            units.Add(new ArmyUnitDefinition
            {
                Id = unitId,
                Isc = entry.Isc,
                IscAbbr = entry.IscAbbr,
                Name = entry.Name,
                Slug = entry.Slug,
                ProfileGroups = WithAva(entry.ProfileGroups, entry.MaxAva),
                Options = entry.Options,
                Filters = entry.Filters
            });

            resumes.Add(new ArmyImportResume
            {
                Id = unitId,
                IdArmy = entry.Resume.IdArmy,
                Isc = entry.Resume.Isc,
                Name = entry.Resume.Name,
                Slug = entry.Resume.Slug,
                Logo = $"{entry.SourceFactionId}-{entry.SourceUnitId}",
                Type = entry.Resume.Type,
                Category = entry.Resume.Category
            });

            unitId++;
        }

        var army = new ArmyDefinition
        {
            Units = units,
            Resume = resumes,
            Filters = MergeFilters(sourceFactionIds)
        };

        return new ArmyDefinition
        {
            Version = ComputeVersionHash(army),
            Units = army.Units,
            Resume = army.Resume,
            Filters = army.Filters
        };
    }

    /// <summary>A short hash of the units' names and profile groups and the unit index, for change detection.</summary>
    public static string ComputeVersionHash(ArmyDefinition army)
    {
        var text = new StringBuilder();
        foreach (var unit in army.Units.OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase))
        {
            text.Append(unit.Name).Append('|');
            ProfileFingerprint.AppendGroups(text, unit.ProfileGroups);
            text.Append('|');
        }

        foreach (var resume in army.Resume.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            text.Append(resume.Name).Append('|').Append(resume.Type?.ToString(CultureInfo.InvariantCulture)).Append('|');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private static UnitProfileGroups? Filter(UnitProfileGroups groups, Func<ProfileGroup, IReadOnlyList<ProfileOption>> qualifyingOptions)
    {
        var kept = new List<ProfileGroup>();
        foreach (var group in groups.Groups)
        {
            var options = qualifyingOptions(group);
            if (options.Count == 0)
            {
                continue;
            }

            kept.Add(new ProfileGroup
            {
                Id = group.Id,
                Isc = group.Isc,
                Category = group.Category,
                Profiles = group.Profiles,
                Options = options
            });
        }

        return kept.Count > 0 ? new UnitProfileGroups(kept) : null;
    }

    /// <summary>The highest AVA of any profile (AVA is a profile value, not an option one); 0 when none.</summary>
    private static int MaxAva(UnitProfileGroups groups) =>
        groups.Groups.SelectMany(group => group.Profiles).Select(profile => profile.Ava ?? 0).Prepend(0).Max();

    private static UnitProfileGroups WithAva(UnitProfileGroups groups, int ava) =>
        new(groups.Groups.Select(group => new ProfileGroup
        {
            Id = group.Id,
            Isc = group.Isc,
            Category = group.Category,
            Profiles = group.Profiles.Select(profile => profile.WithAva(ava)).ToList(),
            Options = group.Options
        }).ToList());

    /// <summary>
    /// The source armies' filter catalogues merged section by section; the first army to define an id wins.
    /// </summary>
    private ArmyFilterCatalog MergeFilters(IReadOnlyList<int> sourceFactionIds)
    {
        var merged = MergedFilterSections.ToDictionary(section => section, _ => new Dictionary<int, ArmyFilterItem>(), StringComparer.Ordinal);
        foreach (var factionId in sourceFactionIds)
        {
            var filters = factionProvider.GetFactionSnapshot(factionId)?.Filters;
            if (filters is null)
            {
                continue;
            }

            foreach (var section in MergedFilterSections)
            {
                foreach (var item in filters.Items(section))
                {
                    merged[section].TryAdd(item.Id, item);
                }
            }
        }

        return new ArmyFilterCatalog(MergedFilterSections.ToDictionary(
            section => section,
            section => (IReadOnlyList<ArmyFilterItem>)merged[section].Values.ToList(),
            StringComparer.Ordinal));
    }

    /// <summary>A unit being merged into the generated army.</summary>
    internal sealed class MergedUnit
    {
        public required string Name { get; init; }

        public required Resume Resume { get; init; }

        public required UnitProfileGroups ProfileGroups { get; set; }

        public required IReadOnlyList<ProfileOption> Options { get; init; }

        public required UnitFilterIds Filters { get; init; }

        public string? Isc { get; init; }

        public string? IscAbbr { get; init; }

        public string? Slug { get; init; }

        public int MaxAva { get; set; }

        public int SourceFactionId { get; init; }

        public int SourceUnitId { get; init; }

        public HashSet<int> SourceFactionIds { get; init; } = [];
    }
}
