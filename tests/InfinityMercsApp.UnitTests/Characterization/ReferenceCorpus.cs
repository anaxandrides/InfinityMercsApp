using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// One unit's stored JSON blobs, exactly as the app keeps them (the SQLite <c>units</c> table columns),
/// plus the owning army's <c>filters</c> blob used to build id → name lookups.
/// </summary>
internal sealed record CorpusUnit(
    string Key,
    string Source,
    int FactionId,
    string ProfileGroupsJson,
    string? FiltersJson,
    string? FactionsJson,
    string? ArmyFiltersJson)
{
    public override string ToString() => Key;
}

/// <summary>
/// Real Infinity Army API payloads checked into the repo:
/// <list type="bullet">
/// <item><c>ReferenceData/army-101.json</c> — a raw <c>/army/units/en/101</c> response (PanOceania).</item>
/// <item><c>ReferenceData/infinitymercs.db3</c> — a snapshot of the app database from before the typed tables
/// (schema version 0): ~4,000 unit JSON blobs across every army, including the generated company factions.
/// The app no longer reads this layout; it is kept only as test data.</item>
/// </list>
/// </summary>
internal static class ReferenceCorpus
{
    public const string Army101Path = "InfinityMercsApp/ReferenceData/army-101.json";
    public const string DatabasePath = "InfinityMercsApp/ReferenceData/infinitymercs.db3";

    private static readonly Lazy<IReadOnlyList<CorpusUnit>> Army101Units = new(LoadArmy101);
    private static readonly Lazy<IReadOnlyList<CorpusUnit>> DatabaseUnits = new(LoadDatabase);

    public static IReadOnlyList<CorpusUnit> Army101 => Army101Units.Value;

    public static IReadOnlyList<CorpusUnit> Database => DatabaseUnits.Value;

    public static IEnumerable<CorpusUnit> All => Army101.Concat(Database);

    public static CorpusUnit Army101Unit(int unitId) =>
        Army101.Single(x => x.Key == $"army-101:{unitId}");

    public static string ReadArmy101FiltersJson()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(RepoFiles.Resolve(Army101Path)));
        return doc.RootElement.GetProperty("filters").GetRawText();
    }

    private static IReadOnlyList<CorpusUnit> LoadArmy101()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(RepoFiles.Resolve(Army101Path)));
        var root = doc.RootElement;
        var armyFilters = root.GetProperty("filters").GetRawText();

        var units = new List<CorpusUnit>();
        foreach (var unit in root.GetProperty("units").EnumerateArray())
        {
            var id = unit.GetProperty("id").GetInt32();
            units.Add(new CorpusUnit(
                Key: $"army-101:{id}",
                Source: "army-101.json",
                FactionId: 101,
                ProfileGroupsJson: unit.GetProperty("profileGroups").GetRawText(),
                FiltersJson: unit.TryGetProperty("filters", out var filters) ? filters.GetRawText() : null,
                FactionsJson: unit.TryGetProperty("factions", out var factions) ? factions.GetRawText() : null,
                ArmyFiltersJson: armyFilters));
        }

        return units;
    }

    private static IReadOnlyList<CorpusUnit> LoadDatabase()
    {
        var dbPath = RepoFiles.Resolve(DatabasePath);
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();

        var armyFilters = new Dictionary<int, string?>();
        using (var armies = connection.CreateCommand())
        {
            armies.CommandText = "SELECT FactionId, FiltersJson FROM armies";
            using var reader = armies.ExecuteReader();
            while (reader.Read())
            {
                armyFilters[reader.GetInt32(0)] = reader.IsDBNull(1) ? null : reader.GetString(1);
            }
        }

        var units = new List<CorpusUnit>();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT UnitKey, FactionId, ProfileGroupsJson, FiltersJson, FactionsJson FROM units ORDER BY UnitKey";
        using (var reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                if (reader.IsDBNull(2))
                {
                    continue;
                }

                var factionId = reader.GetInt32(1);
                units.Add(new CorpusUnit(
                    Key: $"db:{reader.GetString(0)}",
                    Source: "infinitymercs.db3",
                    FactionId: factionId,
                    ProfileGroupsJson: reader.GetString(2),
                    FiltersJson: reader.IsDBNull(3) ? null : reader.GetString(3),
                    FactionsJson: reader.IsDBNull(4) ? null : reader.GetString(4),
                    ArmyFiltersJson: armyFilters.TryGetValue(factionId, out var filters) ? filters : null));
            }
        }

        return units;
    }
}

internal static class RepoFiles
{
    public static string Resolve(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{relativePath}' above '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Resolves the repository root (the directory holding InfinityMercsApp.sln).
    /// </summary>
    public static string RepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "InfinityMercsApp.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException($"Could not locate the repository root above '{AppContext.BaseDirectory}'.");
    }
}
