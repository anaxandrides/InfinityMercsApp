using InfinityMercsApp.AotSmokeTests.Support;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Options;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfinityMercsApp.AotSmokeTests.Smoke;

/// <summary>sqlite-net: the reference metadata and PanOceania army imported into SQLite and read back, as the phone does.</summary>
public static class ArmyStorageSmoke
{
    private const int PanOceania = 101;

    public static async Task RunAsync(SmokeChecks checks, RepoFolder repo)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"aot-smoke-{Guid.NewGuid():N}.db3");
        try
        {
            var repository = new SQLiteRepository(new SQLIteConfiguration { DBPath = databasePath });
            var content = new ArmyContentStore(repository);
            var factions = new FactionProvider(repository, content);
            var metadata = new MetadataProvider(repository);
            var api = new InfinityArmyAPI(new ReferencePayloads(repo), NullLogger<InfinityArmyAPI>.Instance, ArmyJsonReader.Shared);
            var army = await api.GetArmyDataAsync(PanOceania) ?? throw new InvalidDataException("The reference army didn't read.");

            metadata.Import(await api.GetMetaDataAsync() ?? throw new InvalidDataException("The reference metadata didn't read."));
            new ArmyImportProvider(repository, content).Import(PanOceania, army);

            checks.True("sqlite: metadata factions", metadata.GetFactions().Count > 0);
            checks.Note("sqlite: metadata factions, weapons, skills", $"{metadata.GetFactions().Count}, {metadata.GetAllWeapons().Count}, {metadata.GetSkills().Count}");
            var units = factions.GetUnitsByFaction(PanOceania);
            checks.True("sqlite: units", units.Count > 0);
            checks.Note("sqlite: units, resume, mercs", $"{units.Count}, {factions.GetResumeByFaction(PanOceania).Count}, {factions.GetResumeByFactionMercsOnly(PanOceania).Count}");
            var first = units.OrderBy(unit => unit.UnitId).First();
            checks.Note("sqlite: first unit", $"{first.UnitId} {first.Name}, {first.ProfileGroups.Groups.Count} groups, {first.ProfileGroups.AllOptions().Count()} options");
            checks.True("sqlite: army filters", factions.GetFactionSnapshot(PanOceania)?.Filters.Sections.Any() == true);

            var settings = new AppSettingsProvider(repository);
            settings.SetShowUnitsInInches(false);
            settings.SetMetadataFingerprint("first");
            settings.SetMetadataFingerprint("second");
            checks.Equal("sqlite: settings update", "second", settings.GetMetadataFingerprint());
            checks.Equal("sqlite: settings read", false, settings.GetShowUnitsInInches());

            var specOps = new SpecOpsProvider(repository, content);
            specOps.UpsertCCFactionFireteamValidity(PanOceania, "key", true, ["Haris", "Core"]);
            specOps.UpsertCCFactionFireteamValidity(PanOceania, "key", false, ["Duo"]);
            var validity = specOps.GetCCFactionFireteamValidity("key", [PanOceania]).Single();
            checks.Equal("sqlite: replaced rows", "False Duo", $"{validity.HasValidCoreFireteams} {string.Join(",", validity.ValidCoreFireteams)}");

            // An army sent whole as JSON (the API's generated companies) reads back as it was written.
            var json = ArmyDefinitionJson.Write(army);
            checks.True("json: army round trip", json == ArmyDefinitionJson.Write(ArmyDefinitionJson.Read(json)));
        }
        finally
        {
            File.Delete(databasePath);
        }
    }
}
