using InfinityMercsApp.Api.Telemetry;
using InfinityMercsApp.ApiClient.Telemetry;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.Telemetry;

public sealed class TelemetryBatchValidatorTests
{
    private readonly TelemetryBatchValidator _validator;

    public TelemetryBatchValidatorTests()
    {
        var time = Substitute.For<TimeProvider>();
        time.GetUtcNow().Returns(TelemetryBatches.Now);
        _validator = new TelemetryBatchValidator(time);
    }

    [Fact]
    public void Validate_KnownEvents_IsValid() =>
        Assert.True(_validator.Validate(TelemetryBatches.With(TelemetryBatches.PageShown, TelemetryBatches.Crash)).IsValid);

    [Fact]
    public void Validate_InstallIdNotAGuid_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With(TelemetryBatches.PageShown) with { InstallId = "Dan's phone" }).IsValid);

    [Fact]
    public void Validate_NoEvents_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With()).IsValid);

    [Fact]
    public void Validate_TooManyEvents_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With(Enumerable.Repeat(TelemetryBatches.PageShown, TelemetryLimits.MaxEvents + 1))).IsValid);

    [Fact]
    public void Validate_UnknownKind_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With(TelemetryBatches.PageShown with { Kind = (TelemetryKind)99 })).IsValid);

    [Fact]
    public void Validate_NameTooLong_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With(
            TelemetryBatches.PageShown with { Name = new string('a', TelemetryLimits.MaxNameLength + 1) })).IsValid);

    [Fact]
    public void Validate_StackTraceTooLong_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With(
            TelemetryBatches.Crash with { StackTrace = new string('a', TelemetryLimits.MaxStackTraceLength + 1) })).IsValid);

    [Fact]
    public void Validate_NegativeValue_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With(TelemetryBatches.PageShown with { Value = -1 })).IsValid);

    [Fact]
    public void Validate_NotANumber_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With(TelemetryBatches.PageShown with { Value = double.NaN })).IsValid);

    [Fact]
    public void Validate_EventOlderThanThirtyDays_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With(TelemetryBatches.PageShown with { At = TelemetryBatches.Now.AddDays(-31) })).IsValid);

    [Fact]
    public void Validate_EventDaysAhead_IsInvalid() =>
        Assert.False(_validator.Validate(TelemetryBatches.With(TelemetryBatches.PageShown with { At = TelemetryBatches.Now.AddDays(2) })).IsValid);
}
