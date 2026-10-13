using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Repositories;
using DbMetadataFaction = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Faction;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class AirborneCompanyFactionGenerator(
    ISQLiteRepository sqliteRepository,
    IFactionProvider factionProvider,
    IArmyImportProvider armyImportProvider) : IAirborneCompanyFactionGenerator
{
    public const int AirborneCompanyFactionId = 2001;

    private const string LogTag = "AirborneGen";

    /// <inheritdoc/>
    public async Task GenerateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{LogTag}] GenerateAsync started.");

        var factionIds = factionProvider.GetStoredFactionIds()
            .Where(id => id != AirborneCompanyFactionId)
            .ToList();

        if (factionIds.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No stored factions found. Aborting.");
            return;
        }

        // Every skill id named like an airborne deployment skill (Combat Jump, Parachutist) in any army.
        var airborneSkillIds = FindAirborneSkillIds(factionIds);
        if (airborneSkillIds.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No airborne deployment skill IDs found in any faction.");
            return;
        }

        Console.WriteLine($"[{LogTag}] Found {airborneSkillIds.Count} airborne skill ID(s): [{string.Join(", ", airborneSkillIds)}]");

        var builder = new MergedCompanyArmyBuilder(factionProvider, LogTag);
        var mergedUnits = builder.CollectUnits(factionIds, group => AirborneOptions(group, airborneSkillIds), cancellationToken);
        if (mergedUnits.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No qualifying airborne deployment units found across all factions.");
            return;
        }

        var army = builder.BuildArmy(mergedUnits, factionIds);
        var existing = factionProvider.GetFactionSnapshot(AirborneCompanyFactionId);
        if (existing is not null && existing.Version == army.Version)
        {
            Console.WriteLine($"[{LogTag}] No changes detected (version {army.Version}), skipping reimport.");
            return;
        }

        Console.WriteLine($"[{LogTag}] Importing faction {AirborneCompanyFactionId} (version {army.Version}, previous: {existing?.Version ?? "none"})...");
        await armyImportProvider.ImportAsync(AirborneCompanyFactionId, army, cancellationToken);
        InsertMetadataFactionEntry();

        Console.WriteLine($"[{LogTag}] Successfully generated synthetic faction with {army.Units.Count} units (version {army.Version}).");
    }

    /// <summary>
    /// A group qualifies when one of its profiles has an airborne deployment skill (a profile-level skill);
    /// it keeps the options without a positive SWC cost.
    /// </summary>
    private static IReadOnlyList<ProfileOption> AirborneOptions(ProfileGroup group, HashSet<int> airborneSkillIds)
    {
        var hasAirborneDeployment = group.Profiles
            .SelectMany(profile => profile.Skills)
            .Any(skill => skill.Id is int id && airborneSkillIds.Contains(id));

        return hasAirborneDeployment
            ? group.Options.Where(option => !option.HasPositiveSwc).ToList()
            : [];
    }

    private HashSet<int> FindAirborneSkillIds(IReadOnlyList<int> factionIds)
    {
        var airborneSkillIds = new HashSet<int>();
        foreach (var factionId in factionIds)
        {
            var skills = factionProvider.GetFactionSnapshot(factionId)?.Filters.Names(ArmyFilterSection.Skills);
            foreach (var (id, name) in skills ?? new Dictionary<int, string>())
            {
                if (name.Contains("combat jump", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("parachutist", StringComparison.OrdinalIgnoreCase))
                {
                    airborneSkillIds.Add(id);
                }
            }
        }

        return airborneSkillIds;
    }

    private void InsertMetadataFactionEntry()
    {
        sqliteRepository.Delete<DbMetadataFaction>(x => x.Id == AirborneCompanyFactionId);
        sqliteRepository.Insert(new[]
        {
            new DbMetadataFaction
            {
                Id = AirborneCompanyFactionId,
                ParentId = AirborneCompanyFactionId,
                Name = "Airborne Company",
                Slug = "airborne-company",
                Discontinued = false,
                Logo = "SVGCache/MercsIcons/noun-airborne-8005870.svg"
            }
        });
    }
}
