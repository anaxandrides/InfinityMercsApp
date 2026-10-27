using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.Web.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>A page's unhandled exception, which Blazor logs as critical, is reported as a crash.</summary>
public sealed class TelemetryLoggerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 9, 0, 0, TimeSpan.Zero);

    private readonly ITelemetryRecorder _recorder = Substitute.For<ITelemetryRecorder>();
    private readonly ILogger _logger;

    public TelemetryLoggerTests()
    {
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(Now);
        var services = new ServiceCollection().AddSingleton(time).AddSingleton(_recorder).BuildServiceProvider();
        using var provider = new TelemetryLoggerProvider(services);
        _logger = provider.CreateLogger("SeasonPage");
    }

    [Fact]
    public void Log_CriticalWithException_RecordsACrash()
    {
        _logger.LogCritical(new InvalidOperationException("Broken."), "Unhandled.");

        _recorder.Received(1).Record(Arg.Is<TelemetryEvent>(e => e.Kind == TelemetryKind.Crash && e.At == Now && e.Detail == "SeasonPage: Broken."));
    }

    [Fact]
    public void Log_CriticalWithoutException_RecordsNothing()
    {
        _logger.LogCritical("Odd.");

        _recorder.DidNotReceive().Record(Arg.Any<TelemetryEvent>());
    }

    [Fact]
    public void Log_Error_RecordsNothing()
    {
        _logger.LogError(new InvalidOperationException("Broken."), "Handled.");

        _recorder.DidNotReceive().Record(Arg.Any<TelemetryEvent>());
    }

    [Theory]
    [InlineData(LogLevel.Critical, true)]
    [InlineData(LogLevel.Error, false)]
    public void IsEnabled_Level_OnlyCritical(LogLevel level, bool expected) => Assert.Equal(expected, _logger.IsEnabled(level));

    [Fact]
    public void BeginScope_Any_HasNone() => Assert.Null(_logger.BeginScope("scope"));
}
