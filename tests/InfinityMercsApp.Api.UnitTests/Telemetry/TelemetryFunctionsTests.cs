using System.Text;
using System.Text.Json;
using InfinityMercsApp.Api.Auth;
using InfinityMercsApp.Api.Telemetry;
using InfinityMercsApp.ApiClient.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.Telemetry;

public sealed class TelemetryFunctionsTests
{
    private readonly IUserAuthenticator _authenticator = Substitute.For<IUserAuthenticator>();
    private readonly ITelemetrySink _sink = Substitute.For<ITelemetrySink>();
    private readonly TelemetryFunctions _functions;

    public TelemetryFunctionsTests()
    {
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(TelemetryBatches.Now);
        _authenticator.AuthenticateAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(new SignedInUser("user-1"));
        _functions = new TelemetryFunctions(new TelemetryBatchValidator(time), _sink, _authenticator);
    }

    private static HttpRequest Request(string body, string? authorization = null)
    {
        var context = new DefaultHttpContext();
        var bytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        return context.Request;
    }

    private static string Json(TelemetryBatch batch) => JsonSerializer.Serialize(batch, new JsonSerializerOptions(JsonSerializerDefaults.Web));

    [Fact]
    public async Task PostAsync_ValidBatch_IsKeptWithoutAUser()
    {
        var result = await _functions.PostAsync(Request(Json(TelemetryBatches.With(TelemetryBatches.PageShown))), CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        _sink.Received(1).Write(Arg.Is<TelemetryBatch>(batch => batch.Events.Count == 1), null);
        await _authenticator.DidNotReceive().AuthenticateAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostAsync_WithToken_IsKeptWithTheUser()
    {
        await _functions.PostAsync(Request(Json(TelemetryBatches.With(TelemetryBatches.PageShown)), "Bearer token"), CancellationToken.None);

        _sink.Received(1).Write(Arg.Any<TelemetryBatch>(), "user-1");
    }

    [Fact]
    public async Task PostAsync_UnknownKind_IsRefused()
    {
        var body = Json(TelemetryBatches.With(TelemetryBatches.PageShown)).Replace("\"PageShown\"", "\"Keylogger\"", StringComparison.Ordinal);

        Assert.IsType<BadRequestObjectResult>(await _functions.PostAsync(Request(body), CancellationToken.None));
        _sink.DidNotReceive().Write(Arg.Any<TelemetryBatch>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task PostAsync_InvalidBatch_IsRefused()
    {
        var batch = TelemetryBatches.With(TelemetryBatches.PageShown) with { InstallId = "not-a-guid" };

        Assert.IsType<BadRequestObjectResult>(await _functions.PostAsync(Request(Json(batch)), CancellationToken.None));
        _sink.DidNotReceive().Write(Arg.Any<TelemetryBatch>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task PostAsync_BodyTooLarge_IsRefused()
    {
        var result = await _functions.PostAsync(Request(new string(' ', TelemetryLimits.MaxBodyBytes + 1)), CancellationToken.None);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }
}
