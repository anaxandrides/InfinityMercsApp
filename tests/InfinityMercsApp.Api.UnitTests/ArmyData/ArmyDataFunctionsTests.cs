using InfinityMercsApp.Api.ArmyData;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

public sealed class ArmyDataFunctionsTests
{
    private const string SnapshotId = "0123456789abcdef";

    private static readonly ArmyMirrorManifest Snapshot = new()
    {
        SnapshotId = SnapshotId,
        Metadata = new MirroredPayload("m1", DateTimeOffset.UnixEpoch),
        Armies = [new MirroredArmy(101, "1.0", "a1", DateTimeOffset.UnixEpoch)]
    };

    private readonly IArmySnapshotStore _snapshots = Substitute.For<IArmySnapshotStore>();
    private readonly IArmyPayloadStore _payloads = Substitute.For<IArmyPayloadStore>();
    private readonly ArmyDataFunctions _functions;

    public ArmyDataFunctionsTests()
    {
        _payloads.OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new StoredPayload(new MemoryStream(), call.Arg<string>()));
        _functions = new ArmyDataFunctions(_snapshots, _payloads);
    }

    private static HttpRequest Request() => new DefaultHttpContext().Request;

    private static string CacheControlOf(HttpRequest request) => request.HttpContext.Response.Headers.CacheControl.ToString();

    private static string ETagOf(HttpRequest request) => request.HttpContext.Response.Headers.ETag.ToString();

    [Fact]
    public async Task GetManifestAsync_NothingMirroredYet_IsNotFound()
    {
        var result = await _functions.GetManifestAsync(Request(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetManifestAsync_Mirrored_ReturnsTheCurrentSnapshotUncached()
    {
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Snapshot);
        var request = Request();

        var result = await _functions.GetManifestAsync(request, CancellationToken.None);

        Assert.Same(Snapshot, Assert.IsType<JsonResult>(result).Value);
        Assert.Equal("no-cache", CacheControlOf(request));
    }

    [Fact]
    public async Task GetArmyAsync_CurrentSnapshot_SendsItsPayload()
    {
        _snapshots.ReadLatestAsync(Arg.Any<CancellationToken>()).Returns(Snapshot);
        var request = Request();

        var found = await _functions.GetArmyAsync(request, 101, CancellationToken.None);
        var missing = await _functions.GetArmyAsync(Request(), 999, CancellationToken.None);

        Assert.IsType<FileStreamResult>(found);
        Assert.Equal("\"a1\"", ETagOf(request));
        Assert.Equal(PayloadResponses.CurrentCacheControl, CacheControlOf(request));
        Assert.IsType<NotFoundResult>(missing);
    }

    [Fact]
    public async Task GetMetadataAsync_NothingMirroredYet_IsNotFound()
    {
        var result = await _functions.GetMetadataAsync(Request(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        await _payloads.DidNotReceive().OpenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSnapshotArmyAsync_PinnedSnapshot_SendsItsPayloadForGood()
    {
        _snapshots.ReadAsync(SnapshotId, Arg.Any<CancellationToken>()).Returns(Snapshot);
        var request = Request();

        var result = await _functions.GetSnapshotArmyAsync(request, SnapshotId, 101, CancellationToken.None);

        Assert.IsType<FileStreamResult>(result);
        Assert.Equal("\"a1\"", ETagOf(request));
        Assert.Equal(PayloadResponses.SnapshotCacheControl, CacheControlOf(request));
    }

    [Fact]
    public async Task GetSnapshotAsync_PinnedSnapshot_ReturnsItForGood()
    {
        _snapshots.ReadAsync(SnapshotId, Arg.Any<CancellationToken>()).Returns(Snapshot);
        var request = Request();

        var result = await _functions.GetSnapshotAsync(request, SnapshotId, CancellationToken.None);

        Assert.Same(Snapshot, Assert.IsType<JsonResult>(result).Value);
        Assert.Equal(PayloadResponses.SnapshotCacheControl, CacheControlOf(request));
    }

    [Theory]
    [InlineData("latest")]
    [InlineData("0123456789ABCDEF")]
    public async Task GetSnapshotMetadataAsync_NotASnapshotId_IsNotFoundWithoutLookingItUp(string snapshotId)
    {
        var result = await _functions.GetSnapshotMetadataAsync(Request(), snapshotId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        await _snapshots.DidNotReceive().ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSnapshotAsync_UnknownSnapshot_IsNotFound()
    {
        var result = await _functions.GetSnapshotAsync(Request(), SnapshotId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
