using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Company;

/// <summary>What a TAG Company's TAG may never have or change.</summary>
public static class TagRestrictions
{
    /// <summary>
    /// The skills a TAG can't buy with its Spec-Ops XP and may never gain by any other means (perks, gear, rolls).
    /// "Combat Drop" is the older Spec-Ops name for Combat Jump.
    /// </summary>
    public static IReadOnlyList<string> BannedSkills { get; } =
    [
        "Forward Deployment",
        "Strategic Deployment",
        "Infiltration",
        "Impersonation",
        "Parachutist",
        "Combat Jump",
        "Combat Drop",
        "Engineer"
    ];

    /// <summary>The stats a TAG's Spec-Ops XP can't change: ARM, BTS and its STR (the vitality picker).</summary>
    public static IReadOnlyList<Stat> LockedStats { get; } = [Stat.Arm, Stat.Bts, Stat.Vita];

    /// <summary>The banned skill <paramref name="name"/> names or gives (e.g. "Engineer (+Gizmokit)"), if any.</summary>
    public static string? BannedSkillIn(string? name) =>
        string.IsNullOrWhiteSpace(name)
            ? null
            : BannedSkills.FirstOrDefault(skill => name.Contains(skill, StringComparison.OrdinalIgnoreCase));

    /// <summary>Whether <paramref name="name"/> names or gives a skill a TAG may never have.</summary>
    public static bool IsBanned(string? name) => BannedSkillIn(name) is not null;
}
