using System.Text.Json;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.Services.Telemetry;

namespace InfinityMercsApp.AotSmokeTests.Smoke;

/// <summary>The source-generated JSON of the files the app keeps: companies, seasons, the sync ledger and telemetry.</summary>
public static class RecordFilesSmoke
{
    public static Task RunAsync(SmokeChecks checks)
    {
        var folder = Directory.CreateTempSubdirectory("aot-smoke-").FullName;
        try
        {
            Records(checks);
            LoneWolf(checks);
            Ledger(checks, RecordPaths.ForAppData(folder));
            Telemetry(checks, TelemetryPaths.ForAppData(folder));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }

        return Task.CompletedTask;
    }

    private static void Records(SmokeChecks checks)
    {
        var company = new SavedCompanyFile
        {
            CompanyName = "Blue Lions",
            CompanyType = "Standard Company - Vanilla",
            CompanyIdentifier = "c1",
            SourceFactions = [new SavedCompanyFaction { FactionId = 101, FactionName = "PanOceania" }],
            Entries = [new SavedCompanyEntry { EntryIndex = 0, Name = "Fusilier", IsLieutenant = true, Cost = 10 }]
        };
        var companyJson = JsonSerializer.Serialize(company, JsonContracts.For<SavedCompanyFile>(RecordJson.CompanyWriteOptions));
        var readCompany = JsonSerializer.Deserialize(companyJson, JsonContracts.For<SavedCompanyFile>(RecordJson.ReadOptions))
            ?? throw new InvalidDataException("The company read as null.");
        checks.True("json: company round trip", companyJson == JsonSerializer.Serialize(readCompany, JsonContracts.For<SavedCompanyFile>(RecordJson.CompanyWriteOptions)));

        // A member the file leaves out keeps its initial value.
        var sparse = JsonSerializer.Deserialize("""{"CompanyName":"x","CompanyType":"y","CompanyIdentifier":"z","SourceFactions":[],"Entries":[{"Name":"a"}]}""", JsonContracts.For<SavedCompanyFile>(RecordJson.ReadOptions));
        checks.True("json: missing members keep their defaults", sparse?.CoreFireteamSlots is not null && sparse.Entries[0].CurrentEquipmentCodes is not null);

        var season = new SeasonFile { SeasonIdentifier = "s1", CompanyName = "Blue Lions", Rounds = [new SeasonRound { RoundIndex = 1 }] };
        var seasonJson = JsonSerializer.Serialize(season, JsonContracts.For<SeasonFile>(RecordJson.SeasonWriteOptions));
        var readSeason = JsonSerializer.Deserialize(seasonJson, JsonContracts.For<SeasonFile>(RecordJson.ReadOptions))
            ?? throw new InvalidDataException("The season read as null.");
        checks.True("json: season round trip", seasonJson == JsonSerializer.Serialize(readSeason, JsonContracts.For<SeasonFile>(RecordJson.SeasonWriteOptions)));
    }

    // Aspects are copied from the Lone Wolf through a JSON node.
    private static void LoneWolf(SmokeChecks checks)
    {
        var company = new SavedCompanyFile
        {
            CompanyType = ProxyPack.CompanyType,
            Entries =
            [
                new SavedCompanyEntry { EntryIndex = 0, Name = "Kestrel", IsLoneWolf = true, Cost = 30 },
                new SavedCompanyEntry { EntryIndex = 1, Name = "Stale", AspectOf = 0 }
            ]
        };

        checks.True("json: Lone Wolf aspects refreshed", LoneWolfAspects.Refresh(company));
        checks.Equal("json: aspect copied", "Kestrel (Aspect 2) 0", $"{company.Entries[1].CustomName} {company.Entries[1].Cost}");
    }

    private static void Ledger(SmokeChecks checks, RecordPaths paths)
    {
        var ledger = new SyncLedger(paths);
        ledger.UseAccount("kestrel@example.com");
        ledger.RecordSave(UserDocumentKind.Company, "c1", Path.Combine(paths.CompaniesDirectory, "blue-lions-0001.json"));

        var reloaded = new SyncLedger(paths);
        checks.Equal("json: ledger reloaded", "kestrel@example.com Save", $"{reloaded.Account} {reloaded.Documents.Single().Pending}");
        checks.True("json: ledger enums as text", File.ReadAllText(paths.SyncStatePath).Contains("\"Save\"", StringComparison.Ordinal));
    }

    private static void Telemetry(SmokeChecks checks, TelemetryPaths paths)
    {
        var store = new TelemetryStore(paths, TimeProvider.System);
        store.SetEnabled(true);
        store.Record(TelemetryEvents.Timing("ArmyWait", 250, "1 army", DateTimeOffset.UnixEpoch));

        var read = store.TakePending().SelectMany(TelemetryStore.Read).ToList();
        checks.Equal("json: telemetry outbox", "Timing ArmyWait 250", string.Join(";", read.Select(e => $"{e.Kind} {e.Name} {e.Value}")));
        checks.True("json: telemetry setting kept", store.InstallId == new TelemetryStore(paths, TimeProvider.System).InstallId);
    }
}
