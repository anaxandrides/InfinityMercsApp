using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.Services.Telemetry;

/// <summary>
/// The telemetry folder: the setting, and an outbox of one JSON-lines file per session that events are appended to
/// as they happen. Telemetry is disposable, so the outbox is capped and drops its oldest files to stay under it.
/// </summary>
public sealed class TelemetryStore : ITelemetryRecorder, ITelemetrySettings
{
    /// <summary>The most a session file grows to before the next event starts another.</summary>
    public const long MaxFileBytes = 256 * 1024;

    /// <summary>The most the outbox holds; the oldest files go first.</summary>
    public const long MaxOutboxBytes = 2 * 1024 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { TypeInfoResolver = TelemetryJsonContext.Default };

    private static JsonTypeInfo<TelemetryEvent> Event => JsonContracts.For<TelemetryEvent>(JsonOptions);

    private static JsonTypeInfo<TelemetrySettings> Settings => JsonContracts.For<TelemetrySettings>(JsonOptions);

    private readonly Lock _sync = new();
    private readonly TelemetryPaths _paths;
    private readonly TimeProvider _time;
    private TelemetrySettings _settings;
    private string? _sessionFile;

    public TelemetryStore(TelemetryPaths paths, TimeProvider time)
    {
        _paths = paths;
        _time = time;
        _settings = LoadSettings(paths.SettingsPath);
    }

    /// <inheritdoc/>
    public bool IsEnabled
    {
        get
        {
            lock (_sync)
            {
                return _settings.IsEnabled;
            }
        }
    }

    /// <summary>The random id this install's reports carry.</summary>
    public string InstallId
    {
        get
        {
            lock (_sync)
            {
                return _settings.InstallId;
            }
        }
    }

    /// <inheritdoc/>
    public void SetEnabled(bool isEnabled)
    {
        lock (_sync)
        {
            _settings = _settings with { IsEnabled = isEnabled };
            SaveSettings();
            if (!isEnabled)
            {
                _sessionFile = null;
                foreach (var file in OutboxFiles())
                {
                    TryDelete(file);
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Record(TelemetryEvent telemetryEvent)
    {
        try
        {
            var line = JsonSerializer.Serialize(telemetryEvent, Event) + "\n";
            lock (_sync)
            {
                if (!_settings.IsEnabled)
                {
                    return;
                }

                if (_sessionFile is null || !File.Exists(_sessionFile) || new FileInfo(_sessionFile).Length >= MaxFileBytes)
                {
                    _sessionFile = NewSessionFile();
                    DropOldest();
                }

                File.AppendAllText(_sessionFile, line, Encoding.UTF8);
            }
        }
        catch (Exception ex)
        {
            // Telemetry must never be why the app fails.
            Console.Error.WriteLine($"Recording telemetry failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Every outbox file, oldest first, ready to send: this session's file is closed off, so events from now on go
    /// to a new one and nothing is appended to a file while it is being sent.
    /// </summary>
    public IReadOnlyList<string> TakePending()
    {
        lock (_sync)
        {
            _sessionFile = null;
            return OutboxFiles();
        }
    }

    /// <summary>The events in an outbox file; lines that can't be read (a crash cut one short) are skipped.</summary>
    public static IReadOnlyList<TelemetryEvent> Read(string file)
    {
        var events = new List<TelemetryEvent>();
        foreach (var line in File.ReadLines(file))
        {
            try
            {
                if (JsonSerializer.Deserialize(line, Event) is { } telemetryEvent)
                {
                    events.Add(telemetryEvent);
                }
            }
            catch (JsonException)
            {
                // Skipped.
            }
        }

        return events;
    }

    /// <summary>Keeps only <paramref name="unsent"/> in the file, or deletes it when none are left or telemetry was turned off.</summary>
    public void Keep(string file, IReadOnlyList<TelemetryEvent> unsent)
    {
        lock (_sync)
        {
            if (unsent.Count == 0 || !_settings.IsEnabled)
            {
                TryDelete(file);
                return;
            }

            File.WriteAllLines(file, unsent.Select(telemetryEvent => JsonSerializer.Serialize(telemetryEvent, Event)), Encoding.UTF8);
        }
    }

    private string NewSessionFile()
    {
        Directory.CreateDirectory(_paths.OutboxDirectory);
        var name = $"{_time.GetUtcNow():yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.jsonl";
        return Path.Combine(_paths.OutboxDirectory, name);
    }

    // Deletes the oldest files until the outbox is back under its cap.
    private void DropOldest()
    {
        var files = OutboxFiles().Select(file => new FileInfo(file)).ToList();
        var total = files.Sum(file => file.Length);
        foreach (var file in files)
        {
            if (total <= MaxOutboxBytes - MaxFileBytes)
            {
                return;
            }

            total -= file.Length;
            TryDelete(file.FullName);
        }
    }

    // Named by when they were started, so by name is oldest first.
    private List<string> OutboxFiles() =>
        Directory.Exists(_paths.OutboxDirectory)
            ? Directory.GetFiles(_paths.OutboxDirectory, "*.jsonl").Order(StringComparer.Ordinal).ToList()
            : [];

    private void SaveSettings()
    {
        Directory.CreateDirectory(_paths.Directory);
        File.WriteAllText(_paths.SettingsPath, JsonSerializer.Serialize(_settings, Settings));
    }

    private static TelemetrySettings LoadSettings(string path)
    {
        try
        {
            if (File.Exists(path) &&
                JsonSerializer.Deserialize(File.ReadAllText(path), Settings) is { } saved &&
                Guid.TryParse(saved.InstallId, out _))
            {
                return saved;
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Reading the telemetry settings failed: {ex.Message}");
        }

        var settings = TelemetrySettings.New();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, JsonSerializer.Serialize(settings, Settings));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Saving the telemetry settings failed: {ex.Message}");
        }

        return settings;
    }

    private static void TryDelete(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Deleting {file} failed: {ex.Message}");
        }
    }
}
