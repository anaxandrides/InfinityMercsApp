using InfinityMercsApp.Api.ArmyData;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

public sealed class ArmyDataFunctionsTests
{
    private readonly IArmyMirrorStore _store = Substitute.For<IArmyMirrorStore>();
    private readonly ArmyDataFunctions _functions;

    public ArmyDataFunctionsTests() => _functions = new ArmyDataFunctions(_store);

    private static HttpRequest Request() => new DefaultHttpContext().Request;

    [Fact]
    public async Task GetManifestAsync_NothingMirroredYet_IsNotFound()
    {
        var result = await _functions.GetManifestAsync(Request(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetManifestAsync_Mirrored_ReturnsItUncached()
    {
        var manifest = new ArmyMirrorManifest { Armies = [new MirroredArmy(101, "1.0", "abc", DateTimeOffset.UnixEpoch)] };
        _store.ReadManifestAsync(Arg.Any<CancellationToken>()).Returns(manifest);
        var request = Request();

        var result = await _functions.GetManifestAsync(request, CancellationToken.None);

        Assert.Same(manifest, Assert.IsType<JsonResult>(result).Value);
        Assert.Equal("no-cache", request.HttpContext.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task GetMetadataAsync_Mirrored_SendsThePayload()
    {
        _store.OpenPayloadAsync(MirrorBlobNames.Metadata, Arg.Any<CancellationToken>()).Returns(new StoredPayload(new MemoryStream(), "abc"));

        var result = await _functions.GetMetadataAsync(Request(), CancellationToken.None);

        Assert.IsType<FileStreamResult>(result);
    }

    [Fact]
    public async Task GetArmyAsync_Faction_OpensThatFactionsPayload()
    {
        _store.OpenPayloadAsync(MirrorBlobNames.Army(101), Arg.Any<CancellationToken>()).Returns(new StoredPayload(new MemoryStream(), "abc"));

        var found = await _functions.GetArmyAsync(Request(), 101, CancellationToken.None);
        var missing = await _functions.GetArmyAsync(Request(), 999, CancellationToken.None);

        Assert.IsType<FileStreamResult>(found);
        Assert.IsType<NotFoundResult>(missing);
    }
}
