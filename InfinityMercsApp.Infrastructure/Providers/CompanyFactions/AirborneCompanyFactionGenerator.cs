using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Repositories;

namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <inheritdoc/>
public sealed class AirborneCompanyFactionGenerator(
    ISQLiteRepository sqliteRepository,
    IFactionProvider factionProvider,
    IArmyImportProvider armyImportProvider) : IAirborneCompanyFactionGenerator
{
    public const int AirborneCompanyFactionId = 2001;

    private const string LogTag = "AirborneGen";

    private static readonly GeneratedFactionListing Listing =
        new(AirborneCompanyFactionId, "Airborne Company", "airborne-company", "SVGCache/MercsIcons/noun-airborne-8005870.svg");

    /// <inheritdoc/>
    public int FactionId => AirborneCompanyFactionId;

    /// <inheritdoc/>
    public void Generate(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{LogTag}] GenerateAsync started.");
        if (Build(cancellationToken) is not { } army)
        {
            return;
        }

        var existing = factionProvider.GetFactionSnapshot(AirborneCompanyFactionId);
        if (existing is not null && existing.Version == army.Version)
        {
            Console.WriteLine($"[{LogTag}] No changes detected (version {army.Version}), skipping reimport.");
            return;
        }

        Console.WriteLine($"[{LogTag}] Importing faction {AirborneCompanyFactionId} (version {army.Version}, previous: {existing?.Version ?? "none"})...");
        Store(army, cancellationToken);
        Console.WriteLine($"[{LogTag}] Successfully generated synthetic faction with {army.Units.Count} units (version {army.Version}).");
    }

    /// <inheritdoc/>
    public void Store(ArmyDefinition army, CancellationToken cancellationToken = default)
    {
        armyImportProvider.Import(AirborneCompanyFactionId, army, cancellationToken);
        Listing.StoreIn(sqliteRepository);
    }

    /// <inheritdoc/>
    public ArmyDefinition? Build(CancellationToken cancellationToken = default)
    {
        var factionIds = factionProvider.GetStoredFactionIds()
            .Where(id => id != AirborneCompanyFactionId)
            .ToList();

        if (factionIds.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No stored factions found. Aborting.");
            return null;
        }

        // Every skill id named like an airborne deployment skill (Combat Jump, Parachutist) in any army.
        var airborneSkillIds = FindAirborneSkillIds(factionIds);
        if (airborneSkillIds.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No airborne deployment skill IDs found in any faction.");
            return null;
        }

        Console.WriteLine($"[{LogTag}] Found {airborneSkillIds.Count} airborne skill ID(s): [{string.Join(", ", airborneSkillIds)}]");

        var builder = new MergedCompanyArmyBuilder(factionProvider, LogTag);
        var mergedUnits = builder.CollectUnits(factionIds, group => AirborneOptions(group, airborneSkillIds), cancellationToken);
        if (mergedUnits.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No qualifying airborne deployment units found across all factions.");
            return null;
        }

        return builder.BuildArmy(mergedUnits, factionIds);
    }

    /// <summary>
    /// The group's options without a positive SWC cost that deploy airborne: every one when a profile has an
    /// airborne deployment skill, otherwise those that give one themselves (only the Crushers' Combat Jump loadout).
    /// </summary>
    internal static IReadOnlyList<ProfileOption> AirborneOptions(ProfileGroup group, IReadOnlySet<int> airborneSkillIds)
    {
        var profileDeploysAirborne = HasAirborneSkill(group.Profiles.SelectMany(profile => profile.Skills), airborneSkillIds);
        return group.Options
            .Where(option => !option.CostsSwc && (profileDeploysAirborne || HasAirborneSkill(option.Skills, airborneSkillIds)))
            .ToList();
    }

    private static bool HasAirborneSkill(IEnumerable<ProfileEntry> skills, IReadOnlySet<int> airborneSkillIds) =>
        skills.Any(skill => skill.Id is int id && airborneSkillIds.Contains(id));

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
}
