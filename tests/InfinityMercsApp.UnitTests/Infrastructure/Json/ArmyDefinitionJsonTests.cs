using System.Text.Json;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.UnitTests.Characterization;
using InfinityMercsApp.UnitTests.Infrastructure.Repositories;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json;

/// <summary>An army sent as JSON (the generated factions, from the API to the web site) must store as the army itself does.</summary>
public sealed class ArmyDefinitionJsonTests
{
    private static readonly JsonSerializerOptions DumpOptions = new();

    private static string Dump(FactionProvider factions, int factionId)
    {
        var snapshot = factions.GetFactionSnapshot(factionId);
        if (snapshot is not null)
        {
            snapshot.ImportedAtUnixSeconds = 0;
        }

        return JsonSerializer.Serialize(
            new List<object?> { snapshot, factions.GetUnitsByFaction(factionId), factions.GetResumeByFaction(factionId) },
            DumpOptions);
    }

    [Fact]
    public async Task Read_ReferenceArmyWritten_StoresAsTheArmyItself()
    {
        var army = ArmyJsonReader.Shared.ReadArmyPayload(File.ReadAllText(RepoFiles.Resolve(ReferenceCorpus.Army101Path)))
            ?? throw new InvalidOperationException("The reference army didn't read.");
        var direct = await StoredArmies.CreateAsync();
        var viaJson = await StoredArmies.CreateAsync();

        direct.Import.Import(ImportedArmyData.PanOceania, army);
        viaJson.Import.Import(ImportedArmyData.PanOceania, ArmyDefinitionJson.Read(ArmyDefinitionJson.Write(army)));

        Assert.Equal(Dump(direct.Factions, ImportedArmyData.PanOceania), Dump(viaJson.Factions, ImportedArmyData.PanOceania));
    }

    [Fact]
    public async Task Store_GeneratedFactionsSentAsJson_StoreAsGeneratingThemDoes()
    {
        var army = ArmyJsonReader.Shared.ReadArmyPayload(File.ReadAllText(RepoFiles.Resolve(ReferenceCorpus.Army101Path)))
            ?? throw new InvalidOperationException("The reference army didn't read.");
        var built = await StoredArmies.CreateAsync();
        var sent = await StoredArmies.CreateAsync();
        built.Import.Import(ImportedArmyData.PanOceania, army);

        for (var index = 0; index < built.Generators.Count; index++)
        {
            var generator = built.Generators[index];
            var receiver = sent.Generators[index];
            generator.Generate();
            var generated = generator.Build() ?? throw new InvalidOperationException($"Faction {generator.FactionId} built nothing.");
            receiver.Store(ArmyDefinitionJson.Read(ArmyDefinitionJson.Write(generated)));

            Assert.NotEmpty(sent.Factions.GetUnitsByFaction(generator.FactionId));
            Assert.Equal(Dump(built.Factions, generator.FactionId), Dump(sent.Factions, generator.FactionId));
        }
    }
}
