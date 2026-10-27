using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.Services.Telemetry;
using Microsoft.JSInterop;

namespace InfinityMercsApp.Web.ArmyData;

/// <summary>
/// The army data in the browser. Opening the site loads only the metadata (factions, weapons, skills); each army loads
/// the first time a screen needs it, and the rest follow in the background one at a time, so the whole set is soon in
/// memory without a long wait up front. Payloads are kept in the browser's cache, so later visits load them from there.
/// The generated companies' factions come from the API ready built.
/// </summary>
public sealed partial class WebArmyData : ObservableObject, IArmyDataLoader, IDisposable
{
    // The faction the app never loads (ArmyDataUpdater): a placeholder in the metadata with no army of its own.
    private const int SuppressedFactionId = 901;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // A pause between background loads, so the page can draw and answer between armies.
    private static readonly TimeSpan BackgroundPause = TimeSpan.FromMilliseconds(20);

    // How many armies the background fill fetches ahead of the one it stores: enough to keep downloading, few enough
    // that an army a page asks for isn't queued behind the rest.
    private const int BackgroundFetchAhead = 3;

    private readonly ArmyMirrorClient _mirror;
    private readonly CachedArmyPayloads _payloads;
    private readonly BrowserPayloadCache _cache;
    private readonly IInfinityArmyAPI _api;
    private readonly IMetadataProvider _metadata;
    private readonly IArmyImportProvider _import;
    private readonly FactionLogoCacheService _logos;
    private readonly IReadOnlyList<ICompanyFactionGenerator> _generators;
    private readonly ISyncCoordinator _sync;
    private readonly ITelemetryRecorder _telemetry;
    private readonly TimeProvider _time;
    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _one = new(1, 1);
    private readonly CancellationTokenSource _stopping = new();
    private readonly HashSet<int> _loaded = [];
    private ArmyMirrorManifest _manifest = new();
    private IReadOnlyList<int> _armyIds = [];
    private Task? _starting;

    public WebArmyData(
        ArmyMirrorClient mirror,
        CachedArmyPayloads payloads,
        BrowserPayloadCache cache,
        IInfinityArmyAPI api,
        IMetadataProvider metadata,
        IArmyImportProvider import,
        FactionLogoCacheService logos,
        IAirborneCompanyFactionGenerator airborne,
        IInspiringCompanyFactionGenerator inspiring,
        ITagCompanyFactionGenerator tag,
        ISyncCoordinator sync,
        ITelemetryRecorder telemetry,
        TimeProvider time,
        IJSRuntime js)
    {
        _mirror = mirror;
        _payloads = payloads;
        _cache = cache;
        _api = api;
        _metadata = metadata;
        _import = import;
        _logos = logos;
        _generators = [airborne, inspiring, tag];
        _sync = sync;
        _telemetry = telemetry;
        _time = time;
        _js = js;
    }

    /// <summary>Whether the metadata is in, so pages can show; armies may still be loading.</summary>
    [ObservableProperty]
    public partial bool IsReady { get; private set; }

    /// <summary>Why the army data couldn't be loaded; empty while it can.</summary>
    [ObservableProperty]
    public partial string FailureMessage { get; private set; } = string.Empty;

    /// <summary>How many armies are in memory, of <see cref="TotalArmies"/>.</summary>
    [ObservableProperty]
    public partial int LoadedArmies { get; private set; }

    public int TotalArmies => _armyIds.Count;

    /// <inheritdoc/>
    public FactionListings? Listings { get; private set; }

    /// <summary>Loads the metadata and starts on the armies, once however often it is called.</summary>
    public Task StartAsync() => _starting ??= StartOnceAsync();

    /// <inheritdoc/>
    public async Task EnsureAsync(IReadOnlyCollection<int> factionIds, CancellationToken cancellationToken = default)
    {
        await StartAsync();
        var missing = factionIds.Distinct().Where(factionId => !_loaded.Contains(factionId)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        var watch = Stopwatch.StartNew();
        _payloads.FetchAhead(missing.Where(factionId => !GeneratedCompanies.IsGenerated(factionId)));
        foreach (var factionId in missing)
        {
            await LoadAsync(factionId, cancellationToken);
        }

        _telemetry.Record(TelemetryEvents.Timing("ArmyWait", watch.Elapsed.TotalMilliseconds, $"{missing.Count} armies", _time.GetUtcNow()));
    }

    /// <inheritdoc/>
    public Task EnsureEveryArmyAsync(CancellationToken cancellationToken = default) => EnsureAsync(_armyIds, cancellationToken);

    private async Task StartOnceAsync()
    {
        try
        {
            _manifest = await _mirror.GetManifestAsync() ?? throw new InvalidDataException("The army data's manifest couldn't be read.");
            _payloads.Use(_manifest);
            _ = _cache.PruneAsync(Hashes(_manifest));

            var listings = _payloads.GetListingsAsync();
            var metadata = await _api.GetMetaDataAsync() ?? throw new InvalidDataException("The army data came back without its metadata.");
            _metadata.Import(metadata);
            Listings = await ReadListingsAsync(listings);
            await _logos.CacheAllAsync(metadata.Factions);
            var listed = metadata.Factions.Select(faction => faction.Id).ToHashSet();
            _armyIds = _manifest.Armies
                .Select(army => army.FactionId)
                .Where(factionId => listed.Contains(factionId) && factionId != SuppressedFactionId)
                .ToList();
            OnPropertyChanged(nameof(TotalArmies));
            IsReady = true;

            _telemetry.Record(TelemetryEvents.Startup("WebReady", await _js.InvokeAsync<double>("performance.now"), _time.GetUtcNow()));
            _sync.Start();
            _ = FillAsync();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or System.Text.Json.JsonException or TaskCanceledException)
        {
            Console.Error.WriteLine($"Loading the army data failed: {ex.Message}");
            FailureMessage = $"The army data couldn't be loaded ({ex.Message}). Reload the page to try again.";
        }
    }

    // The listings the API worked out; null when it hasn't (an older API) or they can't be had, and the screens read every
    // army to list those factions instead.
    private static async Task<FactionListings?> ReadListingsAsync(Task<string?> listings)
    {
        try
        {
            return await listings is { } json ? JsonSerializer.Deserialize<FactionListings>(json, JsonOptions) : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            Console.Error.WriteLine($"The faction listings couldn't be read: {ex.Message}");
            return null;
        }
    }

    /// <summary>Stops loading armies in the background, as when the site's services are disposed.</summary>
    /// <remarks>The source is only cancelled: a fill starting afterwards still reads its token, and it holds no timer.</remarks>
    public void Dispose() => _stopping.Cancel();

    // Loads the armies no screen has asked for yet, one at a time, pausing between them, until the services are disposed.
    private async Task FillAsync()
    {
        var stopping = _stopping.Token;
        var watch = Stopwatch.StartNew();
        try
        {
            for (var index = 0; index < _armyIds.Count; index++)
            {
                _payloads.FetchAhead(_armyIds.Skip(index).Take(BackgroundFetchAhead).Where(factionId => !_loaded.Contains(factionId)));
                await LoadAsync(_armyIds[index], stopping);
                await Task.Delay(BackgroundPause, stopping);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested)
        {
            return;
        }

        _telemetry.Record(TelemetryEvents.Timing(
            "ArmyFill",
            watch.Elapsed.TotalMilliseconds,
            $"{LoadedArmies} armies, {_payloads.CacheHits} from the browser's cache",
            _time.GetUtcNow()));
    }

    private async Task LoadAsync(int factionId, CancellationToken cancellationToken)
    {
        // A generated faction the API hasn't built (an older API) is built here, from every army, as the app builds it.
        var buildHere = GeneratedCompanies.IsGenerated(factionId) && _manifest.Generated.All(army => army.FactionId != factionId);
        if (buildHere)
        {
            await EnsureEveryArmyAsync(cancellationToken);
        }

        await _one.WaitAsync(cancellationToken);
        try
        {
            if (_loaded.Contains(factionId))
            {
                return;
            }

            await StoreAsync(factionId, buildHere, cancellationToken);
            _loaded.Add(factionId);
            LoadedArmies = _armyIds.Count(_loaded.Contains);
            _payloads.Release(factionId);
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or System.Text.Json.JsonException or KeyNotFoundException)
        {
            // The faction shows no units; a later ask tries again.
            _payloads.Release(factionId);
            Console.Error.WriteLine($"Loading the army of faction {factionId} failed: {ex.Message}");
        }
        finally
        {
            _one.Release();
        }
    }

    private async Task StoreAsync(int factionId, bool buildHere, CancellationToken cancellationToken)
    {
        if (GeneratedCompanies.IsGenerated(factionId))
        {
            var generator = _generators.First(candidate => candidate.FactionId == factionId);
            if (buildHere)
            {
                generator.Generate(cancellationToken);
                return;
            }

            var generated = ArmyDefinitionJson.Read(await _payloads.GetGeneratedArmyAsync(factionId, cancellationToken));
            generator.Store(generated, cancellationToken);
            await CacheLogosAsync(factionId, generated, cancellationToken);
            return;
        }

        var army = await _api.GetArmyDataAsync(factionId, cancellationToken)
            ?? throw new InvalidDataException($"The army of faction {factionId} came back empty.");
        _import.Import(factionId, army, cancellationToken);
        await CacheLogosAsync(factionId, army, cancellationToken);
    }

    // Notes where the army's unit logos are, for the pages' images (RemoteLogoStore).
    private Task CacheLogosAsync(int factionId, ArmyDefinition army, CancellationToken cancellationToken) =>
        _logos.CacheUnitLogosAsync(factionId, army.Resume, cancellationToken);

    private static List<string> Hashes(ArmyMirrorManifest manifest) =>
        manifest.Armies.Select(army => army.Hash)
            .Concat(manifest.Generated.Select(army => army.Hash))
            .Append(manifest.Metadata?.Hash ?? string.Empty)
            .Where(hash => hash.Length > 0)
            .ToList();
}
