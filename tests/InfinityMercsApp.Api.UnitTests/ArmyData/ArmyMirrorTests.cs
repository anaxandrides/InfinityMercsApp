using System.Text.Json;
using InfinityMercsApp.Api.ArmyData;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

public sealed class ArmyMirrorTests
{
    private const string Metadata = """{"factions":[{"id":101},{"id":201}]}""";
    private const string PanOceania = """{"version":"1.0","units":[]}""";
    private const string YuJing = """{"version":"2.0","units":[]}""";

    private static readonly DateTimeOffset Earlier = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = new(2026, 10, 20, 6, 0, 0, TimeSpan.Zero);

    private readonly IArmyPayloadSource _source = Substitute.For<IArmyPayloadSource>();
    private readonly IArmyMirrorStore _store = Substitute.For<IArmyMirrorStore>();
    private readonly TimeProvider _time = Substitute.For<TimeProvider>();
    private readonly ArmyMirror _mirror;
    private ArmyMirrorManifest? _written;

    public ArmyMirrorTests()
    {
        _time.GetUtcNow().Returns(Now);
        _source.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns(Metadata);
        _source.GetArmyAsync(101, Arg.Any<CancellationToken>()).Returns(PanOceania);
        _source.GetArmyAsync(201, Arg.Any<CancellationToken>()).Returns(YuJing);
        _store.WriteManifestAsync(Arg.Do<ArmyMirrorManifest>(manifest => _written = manifest), Arg.Any<CancellationToken>());
        _mirror = new ArmyMirror(_source, _store, _time, NullLogger<ArmyMirror>.Instance);
    }

    // The manifest a refresh would leave when every payload was copied earlier, as served now.
    private static ArmyMirrorManifest Current() => new()
    {
        CheckedAt = Earlier,
        Metadata = new MirroredPayload(ArmyPayloads.Hash(Metadata), Earlier),
        Armies =
        [
            new MirroredArmy(101, "1.0", ArmyPayloads.Hash(PanOceania), Earlier),
            new MirroredArmy(201, "2.0", ArmyPayloads.Hash(YuJing), Earlier)
        ]
    };

    private ArmyMirrorManifest Written() => _written ?? throw new InvalidOperationException("No manifest was written.");

    [Fact]
    public async Task RefreshAsync_EmptyMirror_CopiesMetadataAndEveryArmy()
    {
        var refresh = await _mirror.RefreshAsync();

        Assert.True(refresh.MetadataUpdated);
        Assert.Equal([101, 201], refresh.UpdatedFactionIds);
        Assert.Empty(refresh.FailedFactionIds);
        await _store.Received(1).WritePayloadAsync(MirrorBlobNames.Metadata, Metadata, ArmyPayloads.Hash(Metadata), Arg.Any<CancellationToken>());
        await _store.Received(1).WritePayloadAsync(MirrorBlobNames.Army(101), PanOceania, ArmyPayloads.Hash(PanOceania), Arg.Any<CancellationToken>());
        await _store.Received(1).WritePayloadAsync(MirrorBlobNames.Army(201), YuJing, ArmyPayloads.Hash(YuJing), Arg.Any<CancellationToken>());
        var manifest = Written();
        Assert.Equal(Now, manifest.CheckedAt);
        Assert.Equal(new MirroredPayload(ArmyPayloads.Hash(Metadata), Now), manifest.Metadata);
        Assert.Equal(
            [new MirroredArmy(101, "1.0", ArmyPayloads.Hash(PanOceania), Now), new MirroredArmy(201, "2.0", ArmyPayloads.Hash(YuJing), Now)],
            manifest.Armies);
    }

    [Fact]
    public async Task RefreshAsync_NothingChanged_WritesOnlyTheManifest()
    {
        _store.ReadManifestAsync(Arg.Any<CancellationToken>()).Returns(Current());

        var refresh = await _mirror.RefreshAsync();

        Assert.False(refresh.MetadataUpdated);
        Assert.Empty(refresh.UpdatedFactionIds);
        await _store.DidNotReceive().WritePayloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        var manifest = Written();
        Assert.Equal(Now, manifest.CheckedAt);
        Assert.Equal(Current().Metadata, manifest.Metadata);
        Assert.Equal(Current().Armies, manifest.Armies);
    }

    [Fact]
    public async Task RefreshAsync_OneArmyChanged_CopiesOnlyThatArmy()
    {
        _store.ReadManifestAsync(Arg.Any<CancellationToken>()).Returns(Current());
        const string newYuJing = """{"version":"2.1","units":[]}""";
        _source.GetArmyAsync(201, Arg.Any<CancellationToken>()).Returns(newYuJing);

        var refresh = await _mirror.RefreshAsync();

        Assert.Equal([201], refresh.UpdatedFactionIds);
        await _store.Received(1).WritePayloadAsync(MirrorBlobNames.Army(201), newYuJing, ArmyPayloads.Hash(newYuJing), Arg.Any<CancellationToken>());
        await _store.DidNotReceive().WritePayloadAsync(MirrorBlobNames.Army(101), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Equal(new MirroredArmy(201, "2.1", ArmyPayloads.Hash(newYuJing), Now), Written().Armies[1]);
    }

    [Fact]
    public async Task RefreshAsync_ArmyDownloadFails_KeepsItsEarlierCopy()
    {
        _store.ReadManifestAsync(Arg.Any<CancellationToken>()).Returns(Current());
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
        await _store.DidNotReceive().WritePayloadAsync(MirrorBlobNames.Army(201), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_FactionNoLongerListed_DropsIt()
    {
        _store.ReadManifestAsync(Arg.Any<CancellationToken>()).Returns(Current());
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
        await _store.DidNotReceive().WritePayloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_MetadataUnreadable_ChangesNothing()
    {
        _source.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns("<html>Forbidden</html>");

        await Assert.ThrowsAnyAsync<JsonException>(() => _mirror.RefreshAsync());

        Assert.Null(_written);
    }
}
