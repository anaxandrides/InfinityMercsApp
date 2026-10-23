using FluentValidation;
using InfinityMercsApp.ApiClient.Telemetry;

namespace InfinityMercsApp.Api.Telemetry;

/// <summary>
/// What a telemetry batch must be before it is kept: anyone can post one, so only the known shape within
/// <see cref="TelemetryLimits"/> gets through, with times near now.
/// </summary>
public sealed class TelemetryBatchValidator : AbstractValidator<TelemetryBatch>
{
    /// <summary>How old an event can be: a report waits on the device until it can be sent.</summary>
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);

    /// <summary>How far ahead of the API's clock a device's clock can be.</summary>
    public static readonly TimeSpan MaxClockSkew = TimeSpan.FromDays(1);

    public TelemetryBatchValidator(TimeProvider time)
    {
        RuleFor(batch => batch.InstallId).Must(id => Guid.TryParse(id, out _)).WithMessage("The install id must be a GUID.");
        RuleFor(batch => batch.AppVersion).NotEmpty().MaximumLength(TelemetryLimits.MaxFieldLength);
        RuleFor(batch => batch.Platform).NotEmpty().MaximumLength(TelemetryLimits.MaxFieldLength);
        RuleFor(batch => batch.OsVersion).MaximumLength(TelemetryLimits.MaxFieldLength);
        RuleFor(batch => batch.DeviceModel).MaximumLength(TelemetryLimits.MaxFieldLength);
        RuleFor(batch => batch.Events).NotEmpty()
            .Must(events => events.Count <= TelemetryLimits.MaxEvents)
            .WithMessage($"A batch holds at most {TelemetryLimits.MaxEvents} events.");
        RuleForEach(batch => batch.Events).ChildRules(telemetryEvent =>
        {
            telemetryEvent.RuleFor(e => e.Kind).IsInEnum();
            telemetryEvent.RuleFor(e => e.Name).NotEmpty().MaximumLength(TelemetryLimits.MaxNameLength);
            telemetryEvent.RuleFor(e => e.Detail).MaximumLength(TelemetryLimits.MaxDetailLength);
            telemetryEvent.RuleFor(e => e.StackTrace).MaximumLength(TelemetryLimits.MaxStackTraceLength);
            telemetryEvent.RuleFor(e => e.Value).Must(value => value is null || (double.IsFinite(value.Value) && value.Value >= 0))
                .WithMessage("An event's value must be a number, zero or more.");
            telemetryEvent.RuleFor(e => e.At)
                .Must(at => at > time.GetUtcNow() - MaxAge && at < time.GetUtcNow() + MaxClockSkew)
                .WithMessage("An event's time must be within the last 30 days.");
        });
    }
}
