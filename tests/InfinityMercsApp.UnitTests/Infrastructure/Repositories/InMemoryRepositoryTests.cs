using System.Text.Json;
using InfinityMercsApp.Infrastructure.Models.App;
using InfinityMercsApp.Infrastructure.Repositories.InMemory;

namespace InfinityMercsApp.UnitTests.Infrastructure.Repositories;

/// <summary>The in-memory repository, which the web site's army data lives in, must answer every provider as SQLite does.</summary>
public sealed class InMemoryRepositoryTests(ArmyDataInBothRepositories data) : IClassFixture<ArmyDataInBothRepositories>
{
    private static readonly JsonSerializerOptions DumpOptions = new() { WriteIndented = false };

    private static void AssertSame<T>(T expected, T actual) =>
        Assert.Equal(JsonSerializer.Serialize(expected, DumpOptions), JsonSerializer.Serialize(actual, DumpOptions));

    [Fact]
    public async Task FactionProvider_ImportedArmy_AnswersAsSqliteDoes()
    {
        var sqlite = await data.Sqlite;
        var memory = await data.Memory;
        Assert.NotEmpty(memory.Factions.GetUnitsByFaction(ImportedArmyData.PanOceania));
        AssertSame(sqlite.Factions.GetStoredFactionIds().Order(), memory.Factions.GetStoredFactionIds().Order());
        foreach (var factionId in sqlite.Factions.GetStoredFactionIds())
        {
            AssertSame(sqlite.Factions.GetUnitsByFaction(factionId), memory.Factions.GetUnitsByFaction(factionId));
            AssertSame(sqlite.Factions.GetResumeByFaction(factionId), memory.Factions.GetResumeByFaction(factionId));
            AssertSame(sqlite.Factions.GetResumeByFactionMercsOnly(factionId), memory.Factions.GetResumeByFactionMercsOnly(factionId));
            // Each import stamps its own time, which can tick over a second between the two.
            var sqliteSnapshot = sqlite.Factions.GetFactionSnapshot(factionId);
            var memorySnapshot = memory.Factions.GetFactionSnapshot(factionId);
            if (sqliteSnapshot is not null && memorySnapshot is not null)
            {
                memorySnapshot.ImportedAtUnixSeconds = sqliteSnapshot.ImportedAtUnixSeconds;
            }

            AssertSame(sqliteSnapshot, memorySnapshot);
        }
    }

    // sqlite-net's Contains minds case (instr), so "fusil" finds nothing in either.
    [Theory]
    [InlineData("Fusil")]
    [InlineData("fusil")]
    [InlineData("FUSIL")]
    [InlineData("Knight")]
    public async Task SearchUnits_AnyTerm_FindsWhatSqliteFinds(string term)
    {
        var sqlite = await data.Sqlite;
        var memory = await data.Memory;
        AssertSame(sqlite.Factions.SearchUnits(term), memory.Factions.SearchUnits(term));
        AssertSame(sqlite.Factions.SearchUnits(term, ImportedArmyData.PanOceania), memory.Factions.SearchUnits(term, ImportedArmyData.PanOceania));
    }

    [Fact]
    public async Task SearchUnits_TermInAName_FindsIt() =>
        Assert.NotEmpty((await data.Memory).Factions.SearchUnits("Fusil"));

    [Fact]
    public async Task MetadataProvider_ImportedMetadata_AnswersAsSqliteDoes()
    {
        var sqlite = await data.Sqlite;
        var memory = await data.Memory;
        AssertSame(sqlite.Metadata.GetFactions(includeDiscontinued: true), memory.Metadata.GetFactions(includeDiscontinued: true));
        AssertSame(sqlite.Metadata.GetAllWeapons(), memory.Metadata.GetAllWeapons());
        AssertSame(sqlite.Metadata.SearchWeaponsByName("RIFLE"), memory.Metadata.SearchWeaponsByName("RIFLE"));
        AssertSame(sqlite.Metadata.GetSkills(), memory.Metadata.GetSkills());
        AssertSame(sqlite.Metadata.GetAmmunitions(), memory.Metadata.GetAmmunitions());
        AssertSame(sqlite.Metadata.GetBooty(), memory.Metadata.GetBooty());
        AssertSame(sqlite.Metadata.GetMetachemistry(), memory.Metadata.GetMetachemistry());
    }

    [Fact]
    public async Task SpecOpsProvider_ImportedArmy_AnswersAsSqliteDoes()
    {
        var sqlite = await data.Sqlite;
        var memory = await data.Memory;
        var factionId = ImportedArmyData.PanOceania;
        Assert.NotEmpty(memory.SpecOps.GetSpecopsUnitsByFaction(factionId));
        AssertSame(sqlite.SpecOps.GetSpecopsUnitsByFaction(factionId), memory.SpecOps.GetSpecopsUnitsByFaction(factionId));
        AssertSame(sqlite.SpecOps.GetSpecopsSkillsByFaction(factionId), memory.SpecOps.GetSpecopsSkillsByFaction(factionId));
        AssertSame(sqlite.SpecOps.GetSpecopsEquipsByFaction(factionId), memory.SpecOps.GetSpecopsEquipsByFaction(factionId));
        AssertSame(sqlite.SpecOps.GetSpecopsWeaponsByFaction(factionId), memory.SpecOps.GetSpecopsWeaponsByFaction(factionId));
    }

    [Fact]
    public void Update_RowReadThenChanged_ChangesOnlyOnceUpdated()
    {
        var repository = new InMemoryRepository();
        repository.Insert([new AppSetting { Key = "units", Value = "inches" }]);
        var read = repository.FirstOrDefault<AppSetting>(setting => setting.Key == "units") ?? throw new InvalidOperationException("Not stored.");

        read.Value = "centimeters";
        Assert.Equal("inches", repository.FirstOrDefault<AppSetting>(setting => setting.Key == "units")?.Value);

        repository.Update(read);
        Assert.Equal("centimeters", repository.FirstOrDefault<AppSetting>(setting => setting.Key == "units")?.Value);
    }
}
