using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Json;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfinityMercsApp.IntegrationTests.InfinityArmy;

/// <summary>
/// Calls the live Infinity Army API through the app's own client, headers and JSON reading included. These fail
/// when Corvus Belli stops answering the app (in October 2026 the API began refusing the old site's Origin from
/// phones, and unit selection went empty) or reshapes the data the app reads.
/// </summary>
public sealed class InfinityArmyApiTests : IDisposable
{
    private const string AndroidUserAgent = "Dalvik/2.1.0 (Linux; U; Android 14; Pixel 7 Build/UQ1A)";

    private readonly CapturingHandler _handler = new();
    private readonly HttpClient _httpClient;
    private readonly InfinityArmyAPI _api;

    public InfinityArmyApiTests()
    {
        // As the app builds its client: no automatic decompression, since the API client unzips gzip itself.
        _httpClient = new HttpClient(_handler) { Timeout = TimeSpan.FromSeconds(60) };

        // Android's HTTP stack sends a User-Agent and desktop .NET sends none. The API refuses any request that
        // has a User-Agent unless its Origin is Corvus Belli's site, so the tests send one as a phone does.
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", AndroidUserAgent);
        _api = new InfinityArmyAPI(_httpClient, NullLogger<InfinityArmyAPI>.Instance, new ArmyJsonReader());
    }

    /// <summary>Every vanilla army; each sectorial is a subset of one of them.</summary>
    private static readonly IReadOnlyList<int> VanillaArmyIds = [101, 201, 301, 401, 501, 601, 701, 801, 1001, 1101];

    public static TheoryData<int> VanillaArmies() => new(VanillaArmyIds);

    public void Dispose() => _httpClient.Dispose();

    [Fact]
    public async Task Metadata_IsReadable()
    {
        var metadata = await _api.GetMetaDataAsync();

        Assert.NotNull(metadata);
        Assert.NotEmpty(metadata.Factions);
        Assert.NotEmpty(metadata.Weapons);
        Assert.NotEmpty(metadata.Skills);
        Assert.NotEmpty(metadata.Equips);
        Assert.NotEmpty(metadata.Ammunitions);
    }

    // Startup compares the fingerprint with the stored one; if it changed between identical downloads, every launch
    // would offer an update.
    [Fact]
    public async Task Metadata_FingerprintIsTheSameForEachDownload()
    {
        var first = await _api.GetMetaDataAsync();
        var second = await _api.GetMetaDataAsync();

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEmpty(first.Fingerprint);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }

    [Fact]
    public async Task Metadata_ListsEveryVanillaArmy()
    {
        var metadata = await _api.GetMetaDataAsync();

        Assert.NotNull(metadata);
        var vanilla = metadata.Factions.Where(faction => faction.Id == faction.ParentId).Select(faction => faction.Id).ToHashSet();
        Assert.All(VanillaArmyIds, army => Assert.Contains(army, vanilla));
    }

    [Fact]
    public async Task Metadata_KeepsTheReferenceShape()
    {
        await _api.GetMetaDataAsync();

        AssertSameShape(JsonShape.Of(ReferenceData.Metadata), JsonShape.Of(_handler.BodyFrom(InfinityArmyAPI.MetadataUrl)));
    }

    [Theory]
    [MemberData(nameof(VanillaArmies))]
    public async Task Army_HasUnitsToSelect(int factionId)
    {
        var army = await _api.GetArmyDataAsync(factionId);

        Assert.NotNull(army);
        Assert.NotEmpty(army.Units);
        Assert.NotEmpty(army.Resume);
        Assert.All(army.Units, unit => Assert.False(unit.ProfileGroups.IsEmpty, $"{unit.Name} has no profiles."));
        Assert.Contains(army.Units, unit => unit.ProfileGroups.AllOptions().Any());
    }

    [Fact]
    public async Task Army_KeepsTheReferenceShape()
    {
        await _api.GetArmyDataAsync(101);

        AssertSameShape(
            JsonShape.Of(ReferenceData.Army101),
            JsonShape.Of(_handler.BodyFrom($"{InfinityArmyAPI.ArmyUrlBase}101")),
            ignored: IsSpecops);
    }

    // The army data stopped sending spec-ops in October 2026 (Team-Ops replaced them); the import keeps the
    // stored ones until it is decided whether Mercs moves to Team-Ops.
    private static bool IsSpecops(string path) =>
        path.StartsWith("$.specops", StringComparison.Ordinal) || path == "$.filters.skills[].specops";

    /// <summary>Every field of the reference is still sent, holding the same kinds of value.</summary>
    private static void AssertSameShape(JsonShape reference, JsonShape live, Func<string, bool>? ignored = null)
    {
        var checkedPaths = reference.Paths.Where(path => ignored?.Invoke(path) != true).ToList();

        var missing = checkedPaths.Where(path => !live.Has(path)).ToList();
        Assert.True(missing.Count == 0, $"The API no longer sends: {string.Join(", ", missing)}");

        var changed = checkedPaths
            .Where(path => reference.KindsAt(path).Count > 0 && live.KindsAt(path).Except(reference.KindsAt(path)).Any())
            .Select(path => $"{path} ({string.Join("/", reference.KindsAt(path))} -> {string.Join("/", live.KindsAt(path))})")
            .ToList();
        Assert.True(changed.Count == 0, $"The API changed the kind of value in: {string.Join(", ", changed)}");
    }
}
