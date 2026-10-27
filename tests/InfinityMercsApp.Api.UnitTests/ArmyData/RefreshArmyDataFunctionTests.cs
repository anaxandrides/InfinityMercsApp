using InfinityMercsApp.Api.ArmyData;
using InfinityMercsApp.Api.ArmyData.Logos;
using InfinityMercsApp.Api.ArmyData.Prebuilt;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

/// <summary>The timed refresh copies the army data and logs what changed.</summary>
public sealed class RefreshArmyDataFunctionTests
{
    private readonly IArmyPayloadSource _source = Substitute.For<IArmyPayloadSource>();
    private readonly IArmySnapshotStore _snapshots = Substitute.For<IArmySnapshotStore>();
    private readonly ILogger<RefreshArmyDataFunction> _logger = Substitute.For<ILogger<RefreshArmyDataFunction>>();
    private readonly RefreshArmyDataFunction _function;

    public RefreshArmyDataFunctionTests()
    {
        var payloads = Substitute.For<IArmyPayloadStore>();
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(new DateTimeOffset(2026, 10, 25, 6, 0, 0, TimeSpan.Zero));
        _source.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns("""{"factions":[]}""");
        var mirror = new ArmyMirror(
            _source,
            payloads,
            _snapshots,
            new LogoBundler(Substitute.For<ILogoSource>(), Substitute.For<ILogoBundleStore>()),
            new PrebuiltArmyBuilder(payloads, NullLogger<InfinityArmyAPI>.Instance),
            time,
            NullLogger<ArmyMirror>.Instance);
        _function = new RefreshArmyDataFunction(mirror, _logger);
    }

    [Fact]
    public async Task RunAsync_FirstCheck_KeepsANewSnapshotAndLogsIt()
    {
        await _function.RunAsync(new TimerInfo(), CancellationToken.None);

        await _snapshots.Received(1).SetLatestAsync(Arg.Any<ArmyMirrorManifest>(), Arg.Any<CancellationToken>());
        var logged = Assert.Single(_logger.ReceivedCalls(), call => call.GetMethodInfo().Name == nameof(ILogger.Log));
        Assert.Equal(LogLevel.Information, logged.GetArguments()[0]);
        Assert.Contains("(new), metadata updated", logged.GetArguments()[2]?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_MetadataUnreachable_Throws()
    {
        _source.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns<string>(_ => throw new HttpRequestException("Down."));

        await Assert.ThrowsAsync<HttpRequestException>(() => _function.RunAsync(new TimerInfo(), CancellationToken.None));
    }
}
