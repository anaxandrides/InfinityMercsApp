using System.Text;
using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>Small requests ask the browser to finish them even when the tab closes.</summary>
public sealed class KeepAliveHandlerTests
{
    private static readonly HttpRequestOptionsKey<IDictionary<string, object>> FetchOptions = new("WebAssemblyFetchOptions");

    // Nothing listens on the discard port, so the request fails at once, after the handler has marked it.
    private static async Task<HttpRequestMessage> Sent(HttpContent? content, IDictionary<string, object>? options = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "http://127.0.0.1:9/telemetry") { Content = content };
        if (options is not null)
        {
            request.Options.Set(FetchOptions, options);
        }

        using var invoker = new HttpMessageInvoker(new KeepAliveHandler());
        await Assert.ThrowsAnyAsync<HttpRequestException>(() => invoker.SendAsync(request, CancellationToken.None));
        return request;
    }

    [Fact]
    public async Task SendAsync_SmallBody_AsksToKeepItAlive()
    {
        var request = await Sent(new StringContent("{}", Encoding.UTF8));

        Assert.True(request.Options.TryGetValue(FetchOptions, out var options));
        Assert.Equal(true, options["keepalive"]);
    }

    [Fact]
    public async Task SendAsync_OtherFetchOptions_KeepsThem()
    {
        var request = await Sent(new StringContent("{}"), new Dictionary<string, object> { ["credentials"] = "include" });

        request.Options.TryGetValue(FetchOptions, out var options);
        Assert.Equal("include", options?["credentials"]);
        Assert.Equal(true, options?["keepalive"]);
    }

    [Fact]
    public async Task SendAsync_LargeBody_IsSentAsUsual()
    {
        var request = await Sent(new ByteArrayContent(new byte[70 * 1024]));

        Assert.False(request.Options.TryGetValue(FetchOptions, out _));
    }

    [Fact]
    public async Task SendAsync_NoBody_IsSentAsUsual() => Assert.False((await Sent(null)).Options.TryGetValue(FetchOptions, out _));
}
