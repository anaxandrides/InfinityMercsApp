using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Repositories;

namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <inheritdoc/>
public sealed class InspiringCompanyFactionGenerator(
    ISQLiteRepository sqliteRepository,
    IFactionProvider factionProvider,
    IArmyImportProvider armyImportProvider) : IInspiringCompanyFactionGenerator
{
    public const int InspiringCompanyFactionId = 2002;

    private const string LogTag = "InspiringGen";

    private static readonly GeneratedFactionListing Listing =
        new(InspiringCompanyFactionId, "Inspiring Company", "inspiring-company", "SVGCache/MercsIcons/noun-leadership-7195245.svg");

    /// <inheritdoc/>
    public int FactionId => InspiringCompanyFactionId;

    /// <inheritdoc/>
    public void Generate(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{LogTag}] GenerateAsync started.");
        if (Build(cancellationToken) is not { } army)
        {
            return;
        }

        var existing = factionProvider.GetFactionSnapshot(InspiringCompanyFactionId);
        if (existing is not null && existing.Version == army.Version)
        {
            Console.WriteLine($"[{LogTag}] No changes detected (version {army.Version}), skipping reimport.");
            return;
        }

        Console.WriteLine($"[{LogTag}] Importing faction {InspiringCompanyFactionId} (version {army.Version}, previous: {existing?.Version ?? "none"})...");
        Store(army, cancellationToken);
        Console.WriteLine($"[{LogTag}] Successfully generated synthetic faction with {army.Units.Count} units (version {army.Version}).");
    }

    /// <inheritdoc/>
    public void Store(ArmyDefinition army, CancellationToken cancellationToken = default)
    {
        armyImportProvider.Import(InspiringCompanyFactionId, army, cancellationToken);
        Listing.StoreIn(sqliteRepository);
    }

    /// <inheritdoc/>
    public ArmyDefinition? Build(CancellationToken cancellationToken = default)
    {
        var factionIds = factionProvider.GetStoredFactionIds()
            .Where(id => id != InspiringCompanyFactionId)
            .ToList();

        if (factionIds.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No stored factions found. Aborting.");
            return null;
        }

        var builder = new MergedCompanyArmyBuilder(factionProvider, LogTag);
        var mergedUnits = builder.CollectUnits(factionIds, IrregularOptions, cancellationToken);
        if (mergedUnits.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No qualifying Irregular units found across all factions.");
            return null;
        }

        return builder.BuildArmy(mergedUnits, factionIds);
    }

    /// <summary>A group keeps the options that give an IRREGULAR order and have no positive SWC cost.</summary>
    private static IReadOnlyList<ProfileOption> IrregularOptions(ProfileGroup group) =>
        group.Options
            .Where(option => option.Orders.Any(order => string.Equals(order.Type, "IRREGULAR", StringComparison.OrdinalIgnoreCase)))
            .Where(option => !option.CostsSwc)
            .ToList();
}
