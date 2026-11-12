using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.SpecOps;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Options;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace InfinityMercsApp.UnitTests.Storage;

/// <summary>
/// Since October 2026 each army sends a Spec-Ops and a Team-Ops unit with an upgrade table
/// (<c>spectables.table</c>). The table is read, stored with the unit, and marks the unit as one that isn't
/// recruited like the rest.
/// </summary>
public sealed class ArmyUpgradeTableTests : IDisposable
{
    private const int PanOceania = 101;
    private const int Fusilier = 5;
    private const int Indigo = 1912;

    private const string Payload = """
        {
          "version": "7.26246",
          "units": [
            { "id": 5, "name": "FUSILIERS", "slug": "fusiliers", "profileGroups": [] },
            {
              "id": 1912, "name": "INDIGO SPEC-OPS", "slug": "indigo-spec-ops", "profileGroups": [],
              "spectables": {
                "specball": { "items": [ { "attrs": [ { "type": "weapon", "id": 96 } ] } ] },
                "table": { "items": [
                  { "attrs": [ { "type": "stat", "stat": "bts", "q": 3 } ] },
                  { "attrs": [ { "type": "stat", "stat": "arm", "q": 1 }, { "type": "skill", "id": 85 } ] },
                  { "attrs": [ { "type": "equip", "id": 183, "extra": [6] } ] },
                  { "attrs": [ { "type": "specball", "id": 1 } ] }
                ] }
              }
            }
          ],
          "resume": [
            { "id": 5, "name": "FUSILIERS", "slug": "fusiliers", "type": 1, "category": 1 },
            { "id": 1912, "name": "INDIGO SPEC-OPS", "slug": "indigo-spec-ops", "type": 2, "category": 4 }
          ]
        }
        """;

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"army-upgrades-{Guid.NewGuid():N}.db3");
    private readonly SQLiteRepository _repository;
    private readonly ArmyContentStore _contentStore;

    public ArmyUpgradeTableTests()
    {
        _repository = new SQLiteRepository(new SQLIteConfiguration { DBPath = _databasePath });
        _contentStore = new ArmyContentStore(_repository);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        IEnumerable<string> suffixes = [string.Empty, "-wal", "-shm"];
        foreach (var suffix in suffixes)
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

    private static ArmyDefinition Army() =>
        new ArmyJsonReader().ReadArmyPayload(Payload) ?? throw new InvalidOperationException("The payload could not be read.");

    private static IReadOnlyList<SpecUpgrade> Expected { get; } =
    [
        new([SpecUpgradePart.ForStat("bts", 3)]),
        new([SpecUpgradePart.ForStat("arm", 1), SpecUpgradePart.ForItem(SpecUpgradePartKind.Skill, 85)]),
        new([SpecUpgradePart.ForItem(SpecUpgradePartKind.Equipment, 183, 6)])
    ];

    private static void AssertTable(SpecUpgradeTable table)
    {
        Assert.Equal(Expected.Count, table.Upgrades.Count);
        for (var index = 0; index < Expected.Count; index++)
        {
            var expected = Expected[index].Parts;
            var actual = table.Upgrades[index].Parts;
            Assert.Equal(expected.Count, actual.Count);
            for (var part = 0; part < expected.Count; part++)
            {
                Assert.Equal(expected[part].Kind, actual[part].Kind);
                Assert.Equal(expected[part].Id, actual[part].Id);
                Assert.Equal(expected[part].StatKey, actual[part].StatKey);
                Assert.Equal(expected[part].Quantity, actual[part].Quantity);
                Assert.Equal(expected[part].Extras, actual[part].Extras);
            }
        }
    }

    [Fact]
    public void ReadArmyPayload_SpecOpsUnit_ReadsItsTableWithoutUnknownParts() =>
        AssertTable(Army().Units.Single(unit => unit.Id == Indigo).Upgrades);

    [Fact]
    public void ReadArmyPayload_OrdinaryUnit_HasNoTable() =>
        Assert.True(Army().Units.Single(unit => unit.Id == Fusilier).Upgrades.IsEmpty);

    [Fact]
    public void ArmyDefinitionJson_SpecOpsUnit_KeepsItsTable() =>
        AssertTable(ArmyDefinitionJson.Read(ArmyDefinitionJson.Write(Army())).Units.Single(unit => unit.Id == Indigo).Upgrades);

    [Fact]
    public void Import_SpecOpsUnit_StoresItsTable()
    {
        new ArmyImportProvider(_repository, _contentStore).Import(PanOceania, Army());

        var unit = new FactionProvider(_repository, new ArmyContentStore(_repository)).GetUnit(PanOceania, Indigo);

        AssertTable(Assert.IsType<Unit>(unit).Upgrades);
    }

    [Fact]
    public void Import_SpecOpsUnit_ListsItApartFromTheRecruits()
    {
        new ArmyImportProvider(_repository, _contentStore).Import(PanOceania, Army());
        var factions = new FactionProvider(_repository, _contentStore);

        Assert.Equal([Fusilier], factions.GetResumeByFactionMercsOnly(PanOceania).Select(resume => resume.UnitId));
        Assert.Equal([Indigo], factions.GetUpgradeUnitResumes(PanOceania).Select(resume => resume.UnitId));
    }
}
