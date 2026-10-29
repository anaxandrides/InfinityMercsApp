using System.Text.Json;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Services.SignIn;

namespace InfinityMercsApp.Services.Telemetry;

/// <summary>
/// Sends the outbox to the API in batches, oldest first. A batch the API took or turned down is gone from the outbox;
/// when the API can't be reached, what is left waits for the next try.
/// </summary>
public sealed class TelemetrySender(TelemetryStore store, ITelemetryApi api, ISignInService signIn, TelemetrySource source)
{
    /// <summary>A batch's events stay under this many bytes of JSON, well inside what the API reads.</summary>
    public const int MaxBatchBytes = TelemetryLimits.MaxBodyBytes / 2;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { TypeInfoResolver = TelemetryJsonContext.Default };

    private readonly SemaphoreSlim _sending = new(1, 1);

    /// <summary>Sends everything waiting; does nothing when a send is already running or telemetry is off.</summary>
    public async Task SendPendingAsync(CancellationToken cancellationToken = default)
    {
        if (!store.IsEnabled || !await _sending.WaitAsync(0, cancellationToken))
        {
            return;
        }

        try
        {
            var files = store.TakePending();
            if (files.Count == 0)
            {
                return;
            }

            var token = signIn.IsAvailable ? await signIn.GetAccessTokenAsync(cancellationToken) : null;
            foreach (var file in files)
            {
                if (!await SendFileAsync(file, token, cancellationToken))
                {
                    return;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Telemetry waits for the next try rather than troubling the app.
            Console.Error.WriteLine($"Sending telemetry failed: {ex.Message}");
        }
        finally
        {
            _sending.Release();
        }
    }

    /// <summary>The events split into batches the API takes: at most so many events, and so many bytes.</summary>
    public static IReadOnlyList<IReadOnlyList<TelemetryEvent>> Batches(IReadOnlyList<TelemetryEvent> events)
    {
        var batches = new List<IReadOnlyList<TelemetryEvent>>();
        var batch = new List<TelemetryEvent>();
        var bytes = 0;
        foreach (var telemetryEvent in events)
        {
            var size = JsonSerializer.SerializeToUtf8Bytes(telemetryEvent, JsonContracts.For<TelemetryEvent>(JsonOptions)).Length;
            if (batch.Count > 0 && (batch.Count == TelemetryLimits.MaxEvents || bytes + size > MaxBatchBytes))
            {
                batches.Add(batch);
                batch = [];
                bytes = 0;
            }

            batch.Add(telemetryEvent);
            bytes += size;
        }

        if (batch.Count > 0)
        {
            batches.Add(batch);
        }

        return batches;
    }

    // True when the whole file went (or was turned down); false when the API couldn't be reached.
    private async Task<bool> SendFileAsync(string file, string? token, CancellationToken cancellationToken)
    {
        var events = TelemetryStore.Read(file);
        var sent = 0;
        foreach (var batch in Batches(events))
        {
            if (await api.SendAsync(BatchOf(batch), token, cancellationToken) == TelemetrySendResult.Unavailable)
            {
                store.Keep(file, events.Skip(sent).ToList());
                return false;
            }

            sent += batch.Count;
        }

        store.Keep(file, []);
        return true;
    }

    private TelemetryBatch BatchOf(IReadOnlyList<TelemetryEvent> events) => new()
    {
        InstallId = store.InstallId,
        AppVersion = Cut(source.AppVersion),
        Platform = Cut(source.Platform),
        OsVersion = Cut(source.OsVersion),
        DeviceModel = Cut(source.DeviceModel),
        Events = events
    };

    private static string Cut(string text) => text.Length <= TelemetryLimits.MaxFieldLength ? text : text[..TelemetryLimits.MaxFieldLength];
}
