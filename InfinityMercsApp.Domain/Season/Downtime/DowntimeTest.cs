using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Season.Downtime;

/// <summary>The test a downtime choice asks for: a stat (optionally modified) or a fixed PS 10 roll.</summary>
public sealed record DowntimeTest(TestKind Kind, Stat? Stat = null, int Modifier = 0)
{
    public static DowntimeTest Ps10 { get; } = new(TestKind.FixedPs10);
    public static DowntimeTest Ps10PlusArm { get; } = new(TestKind.FixedPs10PlusArm);

    public static DowntimeTest For(Stat stat, int modifier = 0) => new(TestKind.Stat, stat, modifier);

    /// <summary>How the test reads on the event card, e.g. "CC-10", "PH", "PS=10" or "PS=10+ARM".</summary>
    public string Display => Kind switch
    {
        TestKind.FixedPs10 => "PS=10",
        TestKind.FixedPs10PlusArm => $"PS=10+{Models.Army.Stats.Stat.Arm.Key()}",
        _ when Stat is { } stat => Modifier == 0 ? stat.Key() : $"{stat.Key()}{Modifier:+0;-0}",
        _ => string.Empty
    };

    public bool NeedsParticipant => Kind is TestKind.Stat or TestKind.FixedPs10PlusArm;
}
