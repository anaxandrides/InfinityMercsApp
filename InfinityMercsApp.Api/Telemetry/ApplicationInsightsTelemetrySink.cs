using InfinityMercsApp.ApiClient.Telemetry;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace InfinityMercsApp.Api.Telemetry;

/// <summary>
/// Keeps the app's telemetry in Application Insights, beside the API's own: crashes as exceptions, everything else as
/// custom events with the number as a measurement. The install id is the user id there, and a signed-in user's id
/// the authenticated user id.
/// </summary>
public sealed class ApplicationInsightsTelemetrySink(TelemetryClient insights) : ITelemetrySink
{
    /// <summary>The role the app's records carry, to tell them apart from the API's.</summary>
    public const string RoleName = "app";

    /// <inheritdoc/>
    public void Write(TelemetryBatch batch, string? userId)
    {
        foreach (var telemetryEvent in batch.Events)
        {
            var record = RecordFor(telemetryEvent);
            record.Timestamp = telemetryEvent.At;
            Describe(record, batch, userId);
            insights.Track(record);
        }
    }

    /// <summary>The record <paramref name="telemetryEvent"/> is kept as.</summary>
    public static ITelemetry RecordFor(TelemetryEvent telemetryEvent)
    {
        if (telemetryEvent.Kind == TelemetryKind.Crash)
        {
            var stack = telemetryEvent.StackTrace ?? string.Empty;
            var details = new ExceptionDetailsInfo(1, 0, telemetryEvent.Name, telemetryEvent.Detail ?? string.Empty, stack.Length > 0, stack, []);
            return new ExceptionTelemetry([details], SeverityLevel.Critical, problemId: null, new Dictionary<string, string>(), new Dictionary<string, double>());
        }

        var record = new EventTelemetry(telemetryEvent.Kind.ToString());
        record.Properties["Name"] = telemetryEvent.Name;
        if (telemetryEvent.Detail is { } detail)
        {
            record.Properties["Detail"] = detail;
        }

        if (telemetryEvent.Value is { } value)
        {
            record.Metrics[MeasurementName(telemetryEvent.Kind)] = value;
        }

        return record;
    }

    /// <summary>What an event's number measures, as its measurement is named.</summary>
    public static string MeasurementName(TelemetryKind kind) => kind switch
    {
        TelemetryKind.PageShown => "JavaObjects",
        TelemetryKind.Startup => "Milliseconds",
        _ => "Value"
    };

    private static void Describe(ITelemetry record, TelemetryBatch batch, string? userId)
    {
        record.Context.Cloud.RoleName = RoleName;
        record.Context.Component.Version = batch.AppVersion;
        record.Context.Device.Type = batch.Platform;
        record.Context.Device.OperatingSystem = $"{batch.Platform} {batch.OsVersion}".Trim();
        record.Context.Device.Model = batch.DeviceModel;
        record.Context.User.Id = batch.InstallId;
        if (userId is not null)
        {
            record.Context.User.AuthenticatedUserId = userId;
        }
    }
}
