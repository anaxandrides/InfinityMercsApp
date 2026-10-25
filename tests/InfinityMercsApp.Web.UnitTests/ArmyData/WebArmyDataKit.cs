using System.Text.Json;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Infrastructure.Repositories.InMemory;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.Web.ArmyData;
using InfinityMercsApp.Web.Platform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.ArmyData;

/// <summary>The site's army data over a fake API serving the reference metadata and PanOceania army.</summary>
internal sealed class WebArmyDataKit
{
    public const int PanOceania = 101;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public WebArmyDataKit(bool apiBuiltGeneratedFactions = true, bool manifestServed = true)
    {
        var tag = new TagCompanyFactionGenerator(new InMemoryRepository(), Substitute.For<IFactionProvider>(), Substitute.For<IArmyImportProvider>(), ArmyJsonReader.Shared)
            .Build() ?? throw new InvalidOperationException("No TAG faction.");
        var manifest = new ArmyMirrorManifest
        {
            SnapshotId = "0123456789abcdef",
            Metadata = new MirroredPayload("metadata-hash", DateTimeOffset.UnixEpoch),
            Armies = [new MirroredArmy(PanOceania, "1", "army-hash", DateTimeOffset.UnixEpoch)],
            Generated = apiBuiltGeneratedFactions ? [new MirroredArmy(TagCompanyFactionGenerator.TagCompanyFactionId, "t", "tag-hash", DateTimeOffset.UnixEpoch)] : []
        };
        var bodies = new Dictionary<string, string>
        {
            ["army/metadata"] = RepoFile.Read("InfinityMercsApp/ReferenceData/metadata.json"),
            [$"army/units/{PanOceania}"] = RepoFile.Read("InfinityMercsApp/ReferenceData/army-101.json"),
            [$"army/generated/{TagCompanyFactionGenerator.TagCompanyFactionId}"] = ArmyDefinitionJson.Write(tag)
        };
        if (manifestServed)
        {
            bodies["army/manifest"] = JsonSerializer.Serialize(manifest, JsonOptions);
        }

        Api = new FakeArmyApi(bodies);

        var js = Substitute.For<IJSRuntime>();
        js.InvokeAsync<IJSObjectReference>("import", Arg.Any<object?[]?>()).Returns(new ValueTask<IJSObjectReference>(Substitute.For<IJSObjectReference>()));
        js.InvokeAsync<double>("performance.now", Arg.Any<object?[]?>()).Returns(new ValueTask<double>(250d));

        var mirror = new ArmyMirrorClient(new HttpClient(Api), new ArmyMirrorEndpoint(new Uri("https://api.example/api/")));
        var cache = new BrowserPayloadCache(js);
        var payloads = new CachedArmyPayloads(mirror, cache);
        var repository = new InMemoryRepository();
        var content = new ArmyContentStore(repository);
        Factions = new FactionProvider(repository, content);
        var import = new ArmyImportProvider(repository, content);
        var packageFiles = Substitute.For<IAppPackageFiles>();
        Data = new WebArmyData(
            mirror,
            payloads,
            cache,
            new InfinityArmyAPI(payloads, NullLogger<InfinityArmyAPI>.Instance, ArmyJsonReader.Shared),
            new MetadataProvider(repository),
            import,
            new FactionLogoCacheService(new RemoteLogoStore(), RecordPaths.ForAppData("/mercs"), packageFiles),
            new AirborneCompanyFactionGenerator(repository, Factions, import),
            new InspiringCompanyFactionGenerator(repository, Factions, import),
            new TagCompanyFactionGenerator(repository, Factions, import, ArmyJsonReader.Shared),
            Sync,
            Telemetry,
            TimeProvider.System,
            js);
    }

    public FakeArmyApi Api { get; }

    public FactionProvider Factions { get; }

    public ISyncCoordinator Sync { get; } = Substitute.For<ISyncCoordinator>();

    public ITelemetryRecorder Telemetry { get; } = Substitute.For<ITelemetryRecorder>();

    public WebArmyData Data { get; }
}
