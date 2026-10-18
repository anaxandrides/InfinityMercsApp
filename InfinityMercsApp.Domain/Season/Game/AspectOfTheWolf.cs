using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// The profile a Lone Wolf's Aspects deploy as while the Lone Wolf is elsewhere: LI, WIP 13, ARM 1, BTS 3, S 2, and as
/// much STR as the Lone Wolf has VITA (wounds skills included). Transfer Consciousness moves the Lone Wolf into an
/// Aspect, leaving this profile, and any wounds and states, where it was.
/// </summary>
public static class AspectOfTheWolf
{
    /// <summary>Combat Jump (PH=20) on its own half of the table, Nanoscreen, and Inactive: no long or short skills.</summary>
    public static IReadOnlyList<string> Skills { get; } = ["Combat Jump (PH=20)", "Inactive", "Nanoscreen"];

    /// <summary>The statline, with STR equal to the Lone Wolf's VITA.</summary>
    public static IReadOnlyList<StatValue> Statline(string loneWolfVitality) =>
    [
        new(Stat.Mov, "-"),
        new(Stat.Cc, "-"),
        new(Stat.Bs, "-"),
        new(Stat.Ph, "-"),
        new(Stat.Wip, "13"),
        new(Stat.Arm, "1"),
        new(Stat.Bts, "3"),
        new(Stat.Str, loneWolfVitality),
        new(Stat.S, "2")
    ];
}
