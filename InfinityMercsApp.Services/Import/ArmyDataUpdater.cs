using System.Collections.Concurrent;
using System.Globalization;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Services.Logos;

namespace InfinityMercsApp.Services.Import;

/// <inheritdoc/>
internal sealed class ArmyDataUpdater(
    IInfinityArmyAPI infinityArmyAPI,
    IArmyMirror armyMirror,
    IFactionProvider factionProvider,
    IMetadataProvider metadataProvider,
    IArmyImportProvider armyImportProvider,
    IAppSettingsProvider appSettingsProvider,
    FactionLogoCacheService factionLogoCacheService,
    IInspiringCompanyFactionGenerator inspiringCompanyFactionGenerator,
    IAirborneCompanyFactionGenerator airborneCompanyFactionGenerator,
    ITagCompanyFactionGenerator tagCompanyFactionGenerator) : IArmyDataUpdater
{
    private const int ConcurrentDownloads = 6;

    private static readonly HashSet<int> SuppressedFactionIds = [901];

    /// <inheritdoc/>
    public bool HasStoredData() => metadataProvider.HasMetadata() && factionProvider.HasAnyFactionData();

    /// <inheritdoc/>
    public async Task<ArmyDataCheck> CheckAsync(IProgress<ArmyDataProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        progress?.Report(new ArmyDataProgress("Downloading army data...", 0));

        MetadataDocument? metadata;
        try
        {
            metadata = await infinityArmyAPI.GetMetaDataAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"Downloading the metadata failed: {ex.Message}");
            return ArmyDataCheck.Failed($"The Infinity Army data couldn't be downloaded ({ex.Message}).");
        }

        if (metadata is not { Factions.Count: > 0 })
        {
            return ArmyDataCheck.Failed("The Infinity Army data came back without any factions.");
        }

        var factions = metadata.Factions
            .Where(faction => !SuppressedFactionIds.Contains(faction.Id))
            .DistinctBy(faction => faction.Id)
            .OrderBy(faction => faction.Id)
            .ToList();

        // The copy's manifest says which versions it holds, so an army already stored at that version isn't downloaded.
        var mirroredVersions = (await armyMirror.GetManifestAsync(cancellationToken))?.Armies
            .ToDictionary(army => army.FactionId, army => army.Version) ?? [];

        var failed = new ConcurrentBag<string>();
        var checkedCount = 0;
        using var gate = new SemaphoreSlim(ConcurrentDownloads);
        var downloads = await Task.WhenAll(factions.Select(async faction =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                if (IsStoredAt(faction.Id, mirroredVersions.GetValueOrDefault(faction.Id)))
                {
                    return null;
                }

                var army = await DownloadAsync(faction, cancellationToken);
                if (army is null)
                {
                    failed.Add(faction.Name);
                }

                return army;
            }
            finally
            {
                gate.Release();
                var count = Interlocked.Increment(ref checkedCount);
                progress?.Report(new ArmyDataProgress($"Checking armies: {count} of {factions.Count}", (double)count / factions.Count));
            }
        }));

        var armies = downloads.OfType<DownloadedArmy>().ToList();
        if (armies.Count == 0 && failed.Count == factions.Count)
        {
            return ArmyDataCheck.Failed("None of the Infinity Army armies could be downloaded.");
        }

        var metadataChanged = metadata.Fingerprint != appSettingsProvider.GetMetadataFingerprint() || !metadataProvider.HasMetadata();
        return ArmyDataCheck.Downloaded(
            metadata,
            metadataChanged,
            armies,
            failed.Order(StringComparer.OrdinalIgnoreCase).ToList());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ApplyAsync(
        ArmyDataCheck check,
        IProgress<ArmyDataProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (check.Metadata is not { } metadata)
        {
            return [];
        }

        if (check.MetadataChanged)
        {
            progress?.Report(new ArmyDataProgress("Storing factions, weapons and skills...", 0));
            metadataProvider.Import(metadata);
            appSettingsProvider.SetMetadataFingerprint(metadata.Fingerprint);
        }

        await factionLogoCacheService.CacheAllAsync(metadata.Factions, cancellationToken);

        var failed = new List<string>();
        for (var index = 0; index < check.Updates.Count; index++)
        {
            var army = check.Updates[index];
            progress?.Report(new ArmyDataProgress($"Storing {army.FactionName}...", (double)index / check.Updates.Count));
            try
            {
                await factionLogoCacheService.CacheUnitLogosAsync(army.FactionId, army.Army.Resume, cancellationToken);
                armyImportProvider.Import(army.FactionId, army.Army, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Console.Error.WriteLine($"Storing faction {army.FactionId} ({army.FactionName}) failed: {ex.Message}");
                failed.Add(army.FactionName);
            }
        }

        progress?.Report(new ArmyDataProgress("Building the generated companies...", 1));
        Generate("Airborne Company", airborneCompanyFactionGenerator.Generate, cancellationToken);
        Generate("Inspiring Company", inspiringCompanyFactionGenerator.Generate, cancellationToken);
        Generate("TAG Company", tagCompanyFactionGenerator.Generate, cancellationToken);
        return failed;
    }

    // True when the army is stored, usably, at the version the copy holds.
    private bool IsStoredAt(int factionId, string? mirroredVersion) =>
        mirroredVersion is not null &&
        factionProvider.GetFactionSnapshot(factionId)?.Version == mirroredVersion &&
        factionProvider.GetResumeByFaction(factionId).Count > 0;

    // An army with no version can't be compared with the stored one, so it counts as not downloaded.
    private async Task<DownloadedArmy?> DownloadAsync(Faction faction, CancellationToken cancellationToken)
    {
        try
        {
            if (await infinityArmyAPI.GetArmyDataAsync(faction.Id, cancellationToken) is not { Version.Length: > 0 } army)
            {
                return null;
            }

            var storedVersion = factionProvider.GetFactionSnapshot(faction.Id)?.Version;
            var hasUsableData = storedVersion is not null && factionProvider.GetResumeByFaction(faction.Id).Count > 0;
            var isNewer = string.IsNullOrWhiteSpace(storedVersion) || CompareVersions(army.Version, storedVersion) > 0;
            return new DownloadedArmy(faction.Id, faction.Name, army, storedVersion, IsUpdate: !hasUsableData || isNewer);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"Downloading faction {faction.Id} ({faction.Name}) failed: {ex.Message}");
            return null;
        }
    }

    private static void Generate(string company, Action<CancellationToken> generate, CancellationToken cancellationToken)
    {
        try
        {
            generate(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.Error.WriteLine($"Generating the {company} factions failed: {ex.Message}");
        }
    }

    private static int CompareVersions(string left, string right)
    {
        var leftParts = left.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var rightParts = right.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var maxLength = Math.Max(leftParts.Length, rightParts.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var leftPart = i < leftParts.Length && int.TryParse(leftParts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : 0;
            var rightPart = i < rightParts.Length && int.TryParse(rightParts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out var r) ? r : 0;

            if (leftPart != rightPart)
            {
                return leftPart.CompareTo(rightPart);
            }
        }

        return 0;
    }
}
