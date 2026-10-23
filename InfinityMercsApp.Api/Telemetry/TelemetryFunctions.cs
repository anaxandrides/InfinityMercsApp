using System.Text.Json;
using InfinityMercsApp.Api.Auth;
using InfinityMercsApp.Api.Http;
using InfinityMercsApp.ApiClient.Telemetry;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace InfinityMercsApp.Api.Telemetry;

/// <summary>
/// Takes the app's telemetry, so no Application Insights key ships in the app. Anyone can post, signed in or not;
/// a batch is kept only when it passes <see cref="TelemetryBatchValidator"/>.
/// </summary>
public sealed class TelemetryFunctions(TelemetryBatchValidator validator, ITelemetrySink sink, IUserAuthenticator authenticator)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { RespectNullableAnnotations = true };

    [Function("PostTelemetry")]
    public async Task<IActionResult> PostAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telemetry")] HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (await RequestBodies.ReadAsync(request, TelemetryLimits.MaxBodyBytes, cancellationToken) is not { } body)
        {
            return new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
        }

        if (Parse(body) is not { } batch)
        {
            return new BadRequestObjectResult(new List<string> { "The body must be a telemetry batch of known events." });
        }

        if (validator.Validate(batch) is { IsValid: false } result)
        {
            return new BadRequestObjectResult(result.Errors.Select(error => error.ErrorMessage).ToList());
        }

        var user = request.Headers.Authorization.Count > 0
            ? await authenticator.AuthenticateAsync(request, cancellationToken)
            : null;
        sink.Write(batch, user?.UserId);
        return new NoContentResult();
    }

    private static TelemetryBatch? Parse(byte[] body)
    {
        try
        {
            return JsonSerializer.Deserialize<TelemetryBatch>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
