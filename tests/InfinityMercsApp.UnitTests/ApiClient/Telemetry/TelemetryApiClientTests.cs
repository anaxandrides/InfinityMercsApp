using System.Net;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.UnitTests.ApiClient.UserDocuments;

namespace InfinityMercsApp.UnitTests.ApiClient.Telemetry;

public sealed class TelemetryApiClientTests
{
    private static readonly TelemetryEndpoint Endpoint = new(new Uri("https://api.example/api/"));

    private static readonly TelemetryBatch Batch = new()
    {
        InstallId = Guid.Empty.ToString("D"),
        AppVersion = "0.1.0",
        Platform = "Android",
        Events = [new TelemetryEvent { Kind = TelemetryKind.PageShown, At = DateTimeOffset.UnixEpoch, Name = "SettingsPage", Value = 100 }]
    };

    [Theory]
    [InlineData(HttpStatusCode.NoContent, TelemetrySendResult.Delivered)]
    [InlineData(HttpStatusCode.BadRequest, TelemetrySendResult.Refused)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, TelemetrySendResult.Refused)]
    [InlineData(HttpStatusCode.InternalServerError, TelemetrySendResult.Unavailable)]
    [InlineData(HttpStatusCode.ServiceUnavailable, TelemetrySendResult.Unavailable)]
    public async Task SendAsync_Answer_GivesItsResult(HttpStatusCode status, TelemetrySendResult expected)
    {
        var client = new TelemetryApiClient(new HttpClient(new CapturingHandler(status, string.Empty)), Endpoint);

        Assert.Equal(expected, await client.SendAsync(Batch, accessToken: null));
    }

    [Fact]
    public async Task SendAsync_WithToken_PostsTheBatchWithIt()
    {
        var handler = new CapturingHandler(HttpStatusCode.NoContent, string.Empty);
        var client = new TelemetryApiClient(new HttpClient(handler), Endpoint);

        await client.SendAsync(Batch, "token-1");

        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal("https://api.example/api/telemetry", handler.Request?.RequestUri?.ToString());
        Assert.Equal("Bearer token-1", handler.Request?.Headers.Authorization?.ToString());
        Assert.Contains("\"kind\":\"PageShown\"", handler.RequestBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendAsync_SignedOut_SendsNoToken()
    {
        var handler = new CapturingHandler(HttpStatusCode.NoContent, string.Empty);

        await new TelemetryApiClient(new HttpClient(handler), Endpoint).SendAsync(Batch, accessToken: null);

        Assert.Null(handler.Request?.Headers.Authorization);
    }

    [Fact]
    public async Task SendAsync_ServerUnreachable_IsUnavailable()
    {
        var client = new TelemetryApiClient(new HttpClient(new FailingHandler()), Endpoint);

        Assert.Equal(TelemetrySendResult.Unavailable, await client.SendAsync(Batch, accessToken: null));
    }
}
