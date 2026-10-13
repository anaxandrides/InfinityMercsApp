using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Models.Database.Army;
using InfinityMercsApp.Infrastructure.Options;
using InfinityMercsApp.Infrastructure.Repositories;
using InfinityMercsApp.UnitTests.Characterization;
using Microsoft.Data.Sqlite;

namespace InfinityMercsApp.UnitTests.Storage;

/// <summary>
/// Proves the typed tables are lossless: every unit, army and spec-ops choice in the reference database
/// (stored there as JSON columns), parsed into typed models, written to the new tables and read back,
/// dumps exactly the same as the model parsed straight from the JSON.
/// </summary>
public sealed class TypedStorageRoundTripTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"typed-storage-{Guid.NewGuid():N}.db3");
    private readonly SQLiteRepository _repository;
    private readonly ArmyJsonReader _reader = new();

    public TypedStorageRoundTripTests()
    {
        _repository = new SQLiteRepository(new SQLIteConfiguration { DBPath = _databasePath });
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { string.Empty, "-wal", "-shm" })
        {
            try
            {
                File.Delete(_databasePath + suffix);
            }
            catch (IOException)
            {
                // The repository keeps its connection open; a leftover temp file is harmless.
            }
        }
    }

    [Fact]
    public void UnitContent_RoundTripsThroughTheTables()
    {
        var expected = new Dictionary<string, UnitContent>(StringComparer.Ordinal);
        var rows = new UnitContentRows();
        foreach (var (factionId, key, profileGroups, options, filters, factions) in ReadLegacyUnits())
        {
            var content = new UnitContent(
                _reader.ReadProfileGroups(profileGroups),
                _reader.ReadUnitOptions(options),
                _reader.ReadUnitFilters(filters),
                _reader.ReadFactionIds(factions));
            expected[key] = content;
            rows.Add(factionId, key, content);
        }

        Assert.True(expected.Count > 4000, $"expected the whole corpus, got {expected.Count} units");
        Insert(rows);

        var loaded = new UnitContentRows();
        loaded.Groups.AddRange(_repository.GetAll<UnitProfileGroupRow>(_ => true));
        loaded.Profiles.AddRange(_repository.GetAll<UnitProfileRow>(_ => true));
        loaded.Moves.AddRange(_repository.GetAll<UnitProfileMoveRow>(_ => true));
        loaded.Options.AddRange(_repository.GetAll<UnitOptionRow>(_ => true));
        loaded.Orders.AddRange(_repository.GetAll<UnitOptionOrderRow>(_ => true));
        loaded.Includes.AddRange(_repository.GetAll<UnitOptionIncludeRow>(_ => true));
        loaded.Entries.AddRange(_repository.GetAll<UnitEntryRow>(_ => true));
        loaded.Extras.AddRange(_repository.GetAll<UnitEntryExtraRow>(_ => true));
        loaded.FilterIds.AddRange(_repository.GetAll<UnitFilterIdRow>(_ => true));
        loaded.Factions.AddRange(_repository.GetAll<UnitFactionRow>(_ => true));
        var actual = loaded.Assemble(expected.Keys);

        AssertSameDumps(expected, actual);
    }

    [Fact]
    public void ArmyContent_RoundTripsThroughTheTables()
    {
        var expected = new Dictionary<int, ArmyContent>();
        var rows = new ArmyContentRows();
        foreach (var army in ReadLegacyArmies())
        {
            var content = new ArmyContent(
                _reader.ReadArmyFilters(army.FiltersJson),
                _reader.ReadRelations(army.RelationsJson),
                _reader.ReadFireteamChart(army.FireteamChartJson));
            expected[army.FactionId] = content;
            rows.Add(army.FactionId, content);
        }

        Assert.True(expected.Count > 50);
        Insert(rows.FilterItems);
        Insert(rows.Relations);
        Insert(rows.RelationUnits);
        Insert(rows.Specs);
        Insert(rows.Teams);
        Insert(rows.TeamTypes);
        Insert(rows.TeamUnits);

        var loaded = new ArmyContentRows();
        loaded.FilterItems.AddRange(_repository.GetAll<ArmyFilterItemRow>(_ => true));
        loaded.Relations.AddRange(_repository.GetAll<ArmyRelationRow>(_ => true));
        loaded.RelationUnits.AddRange(_repository.GetAll<ArmyRelationUnitRow>(_ => true));
        loaded.Specs.AddRange(_repository.GetAll<FireteamChartSpecRow>(_ => true));
        loaded.Teams.AddRange(_repository.GetAll<FireteamChartTeamRow>(_ => true));
        loaded.TeamTypes.AddRange(_repository.GetAll<FireteamChartTeamTypeRow>(_ => true));
        loaded.TeamUnits.AddRange(_repository.GetAll<FireteamChartTeamUnitRow>(_ => true));
        var actual = loaded.Assemble(expected.Keys);

        AssertSameDumps(expected, actual);
    }

    [Fact]
    public void SpecopsChoiceLinks_RoundTripThroughTheTables()
    {
        var expected = new Dictionary<string, SpecopsChoiceLinks>(StringComparer.Ordinal);
        var rows = new SpecopsLinkRows();
        foreach (var army in ReadLegacyArmies())
        {
            var specops = _reader.ReadSpecops(army.SpecopsJson);
            var lists = new[] { ("skill", specops.Skills), ("equip", specops.Equip), ("weapon", specops.Weapons) };
            foreach (var (kind, choices) in lists)
            {
                for (var i = 0; i < (choices?.Count ?? 0); i++)
                {
                    var key = $"{army.FactionId}:{kind}:{i}";
                    var links = SpecopsChoiceLinks.From(choices![i]);
                    expected[key] = links;
                    rows.Add(army.FactionId, key, links);
                }
            }
        }

        Assert.Contains(expected.Values, links => links.Skills.Any(link => link.Extras.Count > 0));
        Insert(rows.Links);
        Insert(rows.LinkExtras);

        var loaded = new SpecopsLinkRows();
        loaded.Links.AddRange(_repository.GetAll<SpecopsLinkRow>(_ => true));
        loaded.LinkExtras.AddRange(_repository.GetAll<SpecopsLinkExtraRow>(_ => true));
        var assembled = loaded.Assemble();
        var actual = expected.Keys.ToDictionary(
            key => key,
            key => assembled.TryGetValue(key, out var links) ? links : SpecopsChoiceLinks.Empty,
            StringComparer.Ordinal);

        AssertSameDumps(expected, actual);
    }

    private void Insert(UnitContentRows rows)
    {
        Insert(rows.Groups);
        Insert(rows.Profiles);
        Insert(rows.Moves);
        Insert(rows.Options);
        Insert(rows.Orders);
        Insert(rows.Includes);
        Insert(rows.Entries);
        Insert(rows.Extras);
        Insert(rows.FilterIds);
        Insert(rows.Factions);
    }

    private void Insert<T>(List<T> rows) where T : new() => _repository.RunInTransaction(() => _repository.Insert(rows));

    private static void AssertSameDumps<TKey, TValue>(Dictionary<TKey, TValue> expected, Dictionary<TKey, TValue> actual)
        where TKey : notnull
    {
        var mismatches = new List<string>();
        foreach (var (key, value) in expected)
        {
            var expectedDump = ModelDump.Of(value);
            var actualDump = actual.TryGetValue(key, out var loaded) ? ModelDump.Of(loaded) : "<missing>";
            if (!string.Equals(expectedDump, actualDump, StringComparison.Ordinal))
            {
                mismatches.Add($"{key}\n  expected: {expectedDump}\n  actual:   {actualDump}");
            }
        }

        Assert.True(mismatches.Count == 0, $"{mismatches.Count} mismatches; first:\n{string.Join("\n", mismatches.Take(3))}");
    }

    private static readonly Lazy<IReadOnlyList<(int FactionId, string Key, string? ProfileGroups, string? Options, string? Filters, string? Factions)>> LegacyUnits =
        new(LoadLegacyUnits);

    private static readonly Lazy<IReadOnlyList<LegacyArmy>> LegacyArmies = new(LoadLegacyArmies);

    internal static IReadOnlyList<(int FactionId, string Key, string? ProfileGroups, string? Options, string? Filters, string? Factions)> ReadLegacyUnits() =>
        LegacyUnits.Value;

    internal static IReadOnlyList<LegacyArmy> ReadLegacyArmies() => LegacyArmies.Value;

    private static IReadOnlyList<(int FactionId, string Key, string? ProfileGroups, string? Options, string? Filters, string? Factions)> LoadLegacyUnits()
    {
        using var connection = OpenReferenceDatabase();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT FactionId, UnitKey, ProfileGroupsJson, OptionsJson, FiltersJson, FactionsJson FROM units
            UNION ALL
            SELECT FactionId, SpecopsUnitKey, ProfileGroupsJson, OptionsJson, FiltersJson, FactionsJson FROM specops_units
            """;
        using var reader = command.ExecuteReader();
        var units = new List<(int, string, string?, string?, string?, string?)>();
        while (reader.Read())
        {
            units.Add((reader.GetInt32(0), reader.GetString(1), Text(reader, 2), Text(reader, 3), Text(reader, 4), Text(reader, 5)));
        }

        return units;
    }

    private static IReadOnlyList<LegacyArmy> LoadLegacyArmies()
    {
        using var connection = OpenReferenceDatabase();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT FactionId, FiltersJson, RelationsJson, FireteamChartJson, SpecopsJson, ReinforcementsJson, RawJson FROM armies ORDER BY FactionId";
        using var reader = command.ExecuteReader();
        var armies = new List<LegacyArmy>();
        while (reader.Read())
        {
            armies.Add(new LegacyArmy(
                reader.GetInt32(0),
                Text(reader, 1),
                Text(reader, 2),
                Text(reader, 3),
                Text(reader, 4),
                Text(reader, 5),
                Text(reader, 6)));
        }

        return armies;
    }

    private static SqliteConnection OpenReferenceDatabase()
    {
        var connection = new SqliteConnection($"Data Source={RepoFiles.Resolve(ReferenceCorpus.DatabasePath)};Mode=ReadOnly");
        connection.Open();
        return connection;
    }

    private static string? Text(SqliteDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    internal sealed record LegacyArmy(
        int FactionId,
        string? FiltersJson,
        string? RelationsJson,
        string? FireteamChartJson,
        string? SpecopsJson,
        string? ReinforcementsJson,
        string? RawJson);
}
