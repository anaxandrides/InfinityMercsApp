using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Repositories;
using DbMetadataFaction = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Faction;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class InspiringCompanyFactionGenerator(
    ISQLiteRepository sqliteRepository,
    IFactionProvider factionProvider,
    IArmyImportProvider armyImportProvider) : IInspiringCompanyFactionGenerator
{
    public const int InspiringCompanyFactionId = 2002;

    private const string LogTag = "InspiringGen";

    /// <inheritdoc/>
    public async Task GenerateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[{LogTag}] GenerateAsync started.");

        var factionIds = factionProvider.GetStoredFactionIds()
            .Where(id => id != InspiringCompanyFactionId)
            .ToList();

        if (factionIds.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No stored factions found. Aborting.");
            return;
        }

        var builder = new MergedCompanyArmyBuilder(factionProvider, LogTag);
        var mergedUnits = builder.CollectUnits(factionIds, IrregularOptions, cancellationToken);
        if (mergedUnits.Count == 0)
        {
            Console.WriteLine($"[{LogTag}] No qualifying Irregular units found across all factions.");
            return;
        }

        var army = builder.BuildArmy(mergedUnits, factionIds);
        var existing = factionProvider.GetFactionSnapshot(InspiringCompanyFactionId);
        if (existing is not null && existing.Version == army.Version)
        {
            Console.WriteLine($"[{LogTag}] No changes detected (version {army.Version}), skipping reimport.");
            return;
        }

        Console.WriteLine($"[{LogTag}] Importing faction {InspiringCompanyFactionId} (version {army.Version}, previous: {existing?.Version ?? "none"})...");
        await armyImportProvider.ImportAsync(InspiringCompanyFactionId, army, cancellationToken);
        InsertMetadataFactionEntry();

        Console.WriteLine($"[{LogTag}] Successfully generated synthetic faction with {army.Units.Count} units (version {army.Version}).");
    }

    /// <summary>A group keeps the options that give an IRREGULAR order and have no positive SWC cost.</summary>
    private static IReadOnlyList<ProfileOption> IrregularOptions(ProfileGroup group) =>
        group.Options
            .Where(option => option.Orders.Any(order => string.Equals(order.Type, "IRREGULAR", StringComparison.OrdinalIgnoreCase)))
            .Where(option => !option.HasPositiveSwc)
            .ToList();

    private void InsertMetadataFactionEntry()
    {
        sqliteRepository.Delete<DbMetadataFaction>(x => x.Id == InspiringCompanyFactionId);
        sqliteRepository.Insert(new[]
        {
            new DbMetadataFaction
            {
                Id = InspiringCompanyFactionId,
                ParentId = InspiringCompanyFactionId,
                Name = "Inspiring Company",
                Slug = "inspiring-company",
                Discontinued = false,
                Logo = "SVGCache/MercsIcons/noun-leadership-7195245.svg"
            }
        });
    }
}
