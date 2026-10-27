using System.Net;
using InfinityMercsApp.Api.ArmyData.Logos;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfinityMercsApp.Api.UnitTests.ArmyData.Logos;

public sealed class HttpLogoSourceTests
{
    [Fact]
    public async Task GetAsync_Served_ReturnsTheBytes() =>
        Assert.Equal([1, 2], await Source(new LogoAnswer(HttpStatusCode.OK)).GetAsync("https://cdn.example/1.svg"));

    [Fact]
    public async Task GetAsync_NotServed_IsNull() =>
        Assert.Null(await Source(new LogoAnswer(HttpStatusCode.NotFound)).GetAsync("https://cdn.example/1.svg"));

    [Fact]
    public async Task GetAsync_Unreachable_IsNull() =>
        Assert.Null(await Source(new LogoAnswer(null)).GetAsync("https://cdn.example/1.svg"));

    private static HttpLogoSource Source(LogoAnswer answer) => new(new HttpClient(answer), NullLogger<HttpLogoSource>.Instance);
}
