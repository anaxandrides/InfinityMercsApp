using System.Text.Json;
using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Services.Telemetry;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Telemetry;

public sealed class TelemetrySenderTests : IDisposable
{
    private static readonly TelemetrySource Source = new("0.1.0", "Android", "16", "Google Pixel 10");

    private readonly TempTelemetry _folder = new();
    private readonly ITelemetryApi _api = Substitute.For<ITelemetryApi>();
    private readonly ISignInService _signIn = Substitute.For<ISignInService>();
    private readonly TelemetryStore _store;
    private readonly TelemetrySender _sender;

    public TelemetrySenderTests()
    {
        _store = _folder.NewStore();
        _api.SendAsync(Arg.Any<TelemetryBatch>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(TelemetrySendResult.Delivered);
        _signIn.IsAvailable.Returns(true);
        _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns("token-1");
        _sender = new TelemetrySender(_store, _api, _signIn, Source);
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task SendPendingAsync_Delivered_EmptiesTheOutbox()
    {
        _store.Record(TempTelemetry.Page("SettingsPage"));

        await _sender.SendPendingAsync();

        await _api.Received(1).SendAsync(
            Arg.Is<TelemetryBatch>(batch => batch.InstallId == _store.InstallId && batch.DeviceModel == "Google Pixel 10" && batch.Events.Count == 1),
            "token-1",
            Arg.Any<CancellationToken>());
        Assert.Empty(_store.TakePending());
    }

    [Fact]
    public async Task SendPendingAsync_Refused_DropsTheBatch()
    {
        _api.SendAsync(Arg.Any<TelemetryBatch>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(TelemetrySendResult.Refused);
        _store.Record(TempTelemetry.Page("SettingsPage"));

        await _sender.SendPendingAsync();

        Assert.Empty(_store.TakePending());
    }

    [Fact]
    public async Task SendPendingAsync_Unavailable_KeepsItForTheNextTry()
    {
        _api.SendAsync(Arg.Any<TelemetryBatch>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(TelemetrySendResult.Unavailable);
        _store.Record(TempTelemetry.Page("SettingsPage"));

        await _sender.SendPendingAsync();

        var file = Assert.Single(_store.TakePending());
        Assert.Single(TelemetryStore.Read(file));
    }

    [Fact]
    public async Task SendPendingAsync_SignedOut_SendsWithoutAToken()
    {
        _signIn.GetAccessTokenAsync(Arg.Any<CancellationToken>()).Returns((string?)null);
        _store.Record(TempTelemetry.Page("SettingsPage"));

        await _sender.SendPendingAsync();

        await _api.Received(1).SendAsync(Arg.Any<TelemetryBatch>(), null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPendingAsync_TelemetryOff_SendsNothing()
    {
        _store.Record(TempTelemetry.Page("SettingsPage"));
        _store.SetEnabled(false);

        await _sender.SendPendingAsync();

        await _api.DidNotReceive().SendAsync(Arg.Any<TelemetryBatch>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Batches_MoreThanTheLimit_SplitsByCount()
    {
        var events = Enumerable.Repeat(TempTelemetry.Page("SettingsPage"), TelemetryLimits.MaxEvents + 1).ToList();

        Assert.Equal([TelemetryLimits.MaxEvents, 1], TelemetrySender.Batches(events).Select(batch => batch.Count));
    }

    [Fact]
    public void Batches_LargeEvents_SplitsBySize()
    {
        var crash = TelemetryEvents.Crash("Test", new InvalidOperationException(new string('m', TelemetryLimits.MaxDetailLength)), TempTelemetry.Now)
            with { StackTrace = new string('s', TelemetryLimits.MaxStackTraceLength) };
        var events = Enumerable.Repeat(crash, 40).ToList();

        var batches = TelemetrySender.Batches(events);

        Assert.True(batches.Count > 1);
        Assert.Equal(events.Count, batches.Sum(batch => batch.Count));
        Assert.All(batches, batch => Assert.InRange(
            JsonSerializer.SerializeToUtf8Bytes(batch, new JsonSerializerOptions(JsonSerializerDefaults.Web)).Length, 1, TelemetrySender.MaxBatchBytes));
    }
}
