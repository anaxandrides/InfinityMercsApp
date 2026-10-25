using System.Text.Json;
using InfinityMercsApp.Api.ArmyData;
using InfinityMercsApp.Api.ArmyData.Logos;
using InfinityMercsApp.Api.ArmyData.Prebuilt;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Services.CompanySelection.Factions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

public sealed class ArmyMirrorTests
{
    private const string Metadata = """{"factions":[{"id":101,"logo":"https://cdn.example/101.svg"},{"id":201}]}""";
    private const string PanOceania = """{"version":"1.0","units":[],"resume":[{"id":7,"logo":"https://cdn.example/units/7.svg"}]}""";
    private const string YuJing = """{"version":"2.0","units":[]}""";

    private static readonly DateTimeOffset Earlier = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 6, 0, 0, TimeSpan.Zero);

    private readonly IArmyPayloadSource _source = Substitute.For<IArmyPayloadSource>();
    private readonly IArmyPayloadStore _payloads = Substitute.For<IArmyPayloadStore>();
    private readonly IArmySnapshotStore _snapshots = Substitute.For<IArmySnapshotStore>();
    private readonly TimeProvider _time = Substitute.For<TimeProvider>();
    private readonly ILogoSource _logoSource = Substitute.For<ILogoSource>();
    private readonly ILogoBundleStore _logoBundles = Substitute.For<ILogoBundleStore>();
    private readonly ArmyMirror _mirror;
    private ArmyMirrorManifest? _written;

    public ArmyMirrorTests()
    {
        _time.GetUtcNow().Returns(Now);
        _source.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns(Metadata);
        _source.GetArmyAsync(101, Arg.Any<CancellationToken>()).Returns(PanOceania);
        _source.GetArmyAsync(201, Arg.Any<CancellationToken>()).Returns(YuJing);
        _snapshots.SetLatestAsync(Arg.Do<ArmyMirrorManifest>(manifest => _written = manifest), Arg.Any<CancellationToken>());
        _logoSource.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("<svg/>"u8.ToArray());
        _mirror = new ArmyMirror(
            _source,
            _payloads,
            _snapshots,
            new LogoBundler(_logoSource, _logoBundles),
            new PrebuiltArmyBuilder(_payloads, NullLogger<InfinityArmyAPI>.Instance),
            _time,
            NullLogger<ArmyMirror>.Instance);
    }

    // The current snapshot when every payload was copied earlier, as served now.
    private static ArmyMirrorManifest Current()
    {
        var metadata = new MirroredPayload(ArmyPayloads.Hash(Metadata), Earlier);
        List<MirroredArmy> armies =
        [
            new MirroredArmy(101, "1.0", ArmyPayloads.Hash(PanOceania), Earlier),
            new MirroredArmy(201, "2.0", ArmyPayloads.Hash(YuJing), Earlier)
        ];
        return new ArmyMirrorManifest
        {
            SnapshotId = ArmySnapshots.IdFor(metadata, armies),
            CheckedAt = Earlier,
            Metadata = metadata,
            Armies = armies,
            Generated = [new MirroredArmy(TagCompanyFactionGenerator.TagCompanyFactionId, "tag", "tag-hash", Earlier)],
            GeneratedRulesVersion = GeneratedCompanies.RulesVersion,
            Listings = new MirroredPayload("listings-hash", Earlier),
            ListingsVersion = FactionListings.Version
        };
    }

    private ArmyMirrorManifest Written() => _written ?? throw new InvalidOperationException("No manifest was written.");

    [Fact]
    public async Task RefreshAsync_EmptyMirror_CopiesMetadataAndEveryArmy()
    {
        var refresh = await _mirror.RefreshAsync();

        Assert.True(refresh.MetadataUpdated);
        Assert.Equal([101, 201], refresh.UpdatedFactionIds);
        Assert.Empty(refresh.FailedFactionIds);
        await _payloads.Received(1).SaveAsync(ArmyPayloads.Hash(Metadata), Metadata, Arg.Any<CancellationToken>());
        await _payloads.Received(1).SaveAsync(ArmyPayloads.Hash(PanOceania), PanOceania, Arg.Any<CancellationToken>());
        await _payloads.Received(1).SaveAsync(ArmyPayloads.Hash(YuJing), YuJing, Arg.Any<CancellationToken>());
        var manifest = Written();
        Assert.Equal(Now, manifest.CheckedAt);
        Assert.Equal(ArmySnapshots.IdFor(manifest.Metadata, manifest.Armies), manifest.SnapshotId);
        Assert.Equal(manifest.SnapshotId, refresh.SnapshotId);
        Assert.True(refresh.NewSnapshot);
        await _snapshots.Received(1).SaveAsync(manifest, Arg.Any<CancellationToken>());
        Assert.Equal(new MirroredPayload(ArmyPayloads.Hash(Metadata), Now), manifest.Metadata);
        Assert.Equal(
            [new MirroredArmy(101, "1.0", ArmyPayloads.Hash(PanOceania), Now), new MirroredArmy(201, "2.0", ArmyPayloads.Hash(YuJing), Now)],
            manifest.Armies);
    }

    [Fact]
    public async Task RefreshAsync_NothingChanged_WritesOnlyTheManifest()
    {
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Current());

        var refresh = await _mirror.RefreshAsync();

        Assert.False(refresh.MetadataUpdated);
        Assert.Empty(refresh.UpdatedFactionIds);
        await _payloads.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        var manifest = Written();
        Assert.Equal(Now, manifest.CheckedAt);
        Assert.Equal(Current().SnapshotId, manifest.SnapshotId);
        Assert.False(refresh.NewSnapshot);
        await _snapshots.DidNotReceive().SaveAsync(Arg.Any<ArmyMirrorManifest>(), Arg.Any<CancellationToken>());
        Assert.Equal(Current().Metadata, manifest.Metadata);
        Assert.Equal(Current().Armies, manifest.Armies);
    }

    [Fact]
    public async Task RefreshAsync_OneArmyChanged_CopiesOnlyThatArmy()
    {
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Current());
        const string newYuJing = """{"version":"2.1","units":[]}""";
        _source.GetArmyAsync(201, Arg.Any<CancellationToken>()).Returns(newYuJing);

        var refresh = await _mirror.RefreshAsync();

        Assert.Equal([201], refresh.UpdatedFactionIds);
        await _payloads.Received(1).SaveAsync(ArmyPayloads.Hash(newYuJing), newYuJing, Arg.Any<CancellationToken>());
        await _payloads.DidNotReceive().SaveAsync(ArmyPayloads.Hash(PanOceania), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal(new MirroredArmy(201, "2.1", ArmyPayloads.Hash(newYuJing), Now), Written().Armies[1]);
        Assert.True(refresh.NewSnapshot);
        Assert.NotEqual(Current().SnapshotId, refresh.SnapshotId);
        await _snapshots.Received(1).SaveAsync(Written(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_ArmyDownloadFails_KeepsItsEarlierCopy()
    {
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Current());
        _source.GetArmyAsync(201, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("403"));

        var refresh = await _mirror.RefreshAsync();

        Assert.Equal([201], refresh.FailedFactionIds);
        Assert.Equal(Current().Armies[1], Written().Armies[1]);
    }

    [Fact]
    public async Task RefreshAsync_NewArmyFails_LeavesItOut()
    {
        _source.GetArmyAsync(201, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("403"));

        var refresh = await _mirror.RefreshAsync();

        Assert.Equal([201], refresh.FailedFactionIds);
        Assert.Equal(101, Assert.Single(Written().Armies).FactionId);
    }

    [Theory]
    [InlineData("""{"units":[]}""")]
    [InlineData("<html>Forbidden</html>")]
    public async Task RefreshAsync_ArmyUnreadable_CountsAsFailed(string payload)
    {
        _source.GetArmyAsync(201, Arg.Any<CancellationToken>()).Returns(payload);

        var refresh = await _mirror.RefreshAsync();

        Assert.Equal([201], refresh.FailedFactionIds);
        await _payloads.DidNotReceive().SaveAsync(Arg.Any<string>(), payload, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_FactionNoLongerListed_DropsIt()
    {
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Current());
        const string metadata = """{"factions":[{"id":101}]}""";
        _source.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns(metadata);

        await _mirror.RefreshAsync();

        Assert.Equal(101, Assert.Single(Written().Armies).FactionId);
        await _source.DidNotReceive().GetArmyAsync(201, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_MetadataDownloadFails_ChangesNothing()
    {
        _source.GetMetadataAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("403"));

        await Assert.ThrowsAsync<HttpRequestException>(() => _mirror.RefreshAsync());

        Assert.Null(_written);
        await _snapshots.DidNotReceive().SaveAsync(Arg.Any<ArmyMirrorManifest>(), Arg.Any<CancellationToken>());
        await _payloads.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_MetadataUnreadable_ChangesNothing()
    {
        _source.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns("<html>Forbidden</html>");

        await Assert.ThrowsAnyAsync<JsonException>(() => _mirror.RefreshAsync());

        Assert.Null(_written);
    }

    [Fact]
    public async Task RefreshAsync_NewSnapshot_IsKeptBeforeItBecomesCurrent()
    {
        await _mirror.RefreshAsync();

        Received.InOrder(() =>
        {
            _snapshots.SaveAsync(Arg.Any<ArmyMirrorManifest>(), Arg.Any<CancellationToken>());
            _snapshots.SetLatestAsync(Arg.Any<ArmyMirrorManifest>(), Arg.Any<CancellationToken>());
        });
    }

    private static string LogosHash => ArmyLogos.Hash(
    [
        new LogoFile("101.svg", "https://cdn.example/101.svg"),
        new LogoFile("units/101-7.svg", "https://cdn.example/units/7.svg")
    ]);

    [Fact]
    public async Task RefreshAsync_LogosNew_BundlesThem()
    {
        var refresh = await _mirror.RefreshAsync();

        Assert.True(refresh.LogosUpdated);
        Assert.Equal(LogosHash, Written().Logos?.Hash);
        await _logoBundles.Received(1).SaveAsync(LogosHash, Arg.Any<Stream>(), Arg.Any<CancellationToken>());
        await _logoSource.Received(1).GetAsync("https://cdn.example/units/7.svg", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_LogosUnchanged_KeepsTheBundle()
    {
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Current() with { Logos = new MirroredPayload(LogosHash, Earlier) });

        var refresh = await _mirror.RefreshAsync();

        Assert.False(refresh.LogosUpdated);
        Assert.Equal(Earlier, Written().Logos?.UpdatedAt);
        await _logoBundles.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, default);
    }

    [Fact]
    public async Task RefreshAsync_AnArmyFailed_KeepsTheEarlierBundle()
    {
        var earlier = new MirroredPayload("earlier", Earlier);
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Current() with { Logos = earlier });
        _source.GetArmyAsync(201, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("down"));

        await _mirror.RefreshAsync();

        Assert.Equal(earlier, Written().Logos);
        await _logoBundles.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, default);
    }

    [Fact]
    public async Task RefreshAsync_EmptyMirror_BuildsTheGeneratedFactionsAndListings()
    {
        var refresh = await _mirror.RefreshAsync();

        var manifest = Written();
        var tag = Assert.Single(manifest.Generated, army => army.FactionId == TagCompanyFactionGenerator.TagCompanyFactionId);
        Assert.Equal(GeneratedCompanies.RulesVersion, manifest.GeneratedRulesVersion);
        Assert.Equal(FactionListings.Version, manifest.ListingsVersion);
        Assert.True(refresh.PrebuiltUpdated);
        await _payloads.Received(1).SaveAsync(tag.Hash, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _payloads.Received(1).SaveAsync(
            manifest.Listings?.Hash ?? throw new InvalidOperationException("No listings."), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_NothingChanged_KeepsWhatWasBuiltAhead()
    {
        var current = Current();
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(current);

        var refresh = await _mirror.RefreshAsync();

        Assert.False(refresh.PrebuiltUpdated);
        Assert.Equal(current.Generated, Written().Generated);
        Assert.Equal(current.Listings, Written().Listings);
    }

    [Fact]
    public async Task RefreshAsync_RulesVersionChanged_BuildsAheadAgain()
    {
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Current() with { GeneratedRulesVersion = "0" });

        var refresh = await _mirror.RefreshAsync();

        Assert.True(refresh.PrebuiltUpdated);
        Assert.Equal(GeneratedCompanies.RulesVersion, Written().GeneratedRulesVersion);
        Assert.DoesNotContain(Written().Generated, army => army.Hash == "tag-hash");
    }

    [Fact]
    public async Task RefreshAsync_ListingsVersionChanged_BuildsAheadAgain()
    {
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Current() with { ListingsVersion = "0" });

        var refresh = await _mirror.RefreshAsync();

        Assert.True(refresh.PrebuiltUpdated);
        Assert.NotEqual("listings-hash", Written().Listings?.Hash);
    }

    [Fact]
    public async Task RefreshAsync_ArmyFailed_KeepsWhatWasBuiltAhead()
    {
        var current = Current() with { GeneratedRulesVersion = "0" };
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(current);
        _source.GetArmyAsync(201, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("403"));

        var refresh = await _mirror.RefreshAsync();

        Assert.False(refresh.PrebuiltUpdated);
        Assert.Equal(current.Generated, Written().Generated);
        Assert.Equal(current.Listings, Written().Listings);
    }
}
