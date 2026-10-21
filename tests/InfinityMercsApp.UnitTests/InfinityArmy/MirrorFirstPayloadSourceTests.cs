using System.Net;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.InfinityArmy;

public sealed class MirrorFirstPayloadSourceTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 20, 12, 0, 0, TimeSpan.Zero);

    private readonly IArmyPayloadSource _mirror = Substitute.For<IArmyPayloadSource>();
    private readonly IArmyPayloadSource _corvusBelli = Substitute.For<IArmyPayloadSource>();
    private readonly TimeProvider _time = Substitute.For<TimeProvider>();
    private readonly MirrorFirstPayloadSource _source;

    public MirrorFirstPayloadSourceTests()
    {
        _time.GetUtcNow().Returns(Start);
        _mirror.GetArmyAsync(101, Arg.Any<CancellationToken>()).Returns("mirror-101");
        _mirror.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns("mirror-metadata");
        _corvusBelli.GetArmyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns("corvus-belli");
        _corvusBelli.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns("corvus-belli-metadata");
        _source = new MirrorFirstPayloadSource(_mirror, _corvusBelli, _time);
    }

    [Fact]
    public async Task GetArmyAsync_CopyServesIt_ReadsTheCopy()
    {
        Assert.Equal("mirror-101", await _source.GetArmyAsync(101));
        Assert.Equal("mirror-metadata", await _source.GetMetadataAsync());
        await _corvusBelli.DidNotReceiveWithAnyArgs().GetArmyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetArmyAsync_CopyUnreachable_ReadsCorvusBelliAndLeavesTheCopyAlone()
    {
        _mirror.GetArmyAsync(101, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("No such host"));

        Assert.Equal("corvus-belli", await _source.GetArmyAsync(101));
        Assert.Equal("corvus-belli-metadata", await _source.GetMetadataAsync());

        await _mirror.DidNotReceive().GetMetadataAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetArmyAsync_RetryDelayOver_TriesTheCopyAgain()
    {
        _mirror.GetArmyAsync(101, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("No such host"));
        await _source.GetArmyAsync(101);
        _time.GetUtcNow().Returns(Start + MirrorFirstPayloadSource.MirrorRetryDelay);

        Assert.Equal("mirror-metadata", await _source.GetMetadataAsync());
    }

    [Fact]
    public async Task GetArmyAsync_CopyLacksThatArmy_ReadsCorvusBelliButKeepsUsingTheCopy()
    {
        _mirror.GetArmyAsync(999, Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Not found", null, HttpStatusCode.NotFound));

        Assert.Equal("corvus-belli", await _source.GetArmyAsync(999));
        Assert.Equal("mirror-101", await _source.GetArmyAsync(101));
    }

    [Fact]
    public async Task GetArmyAsync_Cancelled_DoesNotFallBack()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _mirror.GetArmyAsync(101, cancellation.Token).ThrowsAsync(new TaskCanceledException());

        await Assert.ThrowsAsync<TaskCanceledException>(() => _source.GetArmyAsync(101, cancellation.Token));

        await _corvusBelli.DidNotReceiveWithAnyArgs().GetArmyAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
