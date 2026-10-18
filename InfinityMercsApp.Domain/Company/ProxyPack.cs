namespace InfinityMercsApp.Domain.Company;

/// <summary>
/// A Proxy Pack: one captain from a single sectorial or vanilla army, the Lone Wolf, and never another trooper. The
/// Lone Wolf needs Regular training rather than the Lieutenant skill, gains 10 more Spec-Ops XP whatever it costs,
/// Transfer Consciousness and Tactical Awareness, and is always Elite Deployed.
/// </summary>
public static class ProxyPack
{
    /// <summary>The company type a Proxy Pack is saved with.</summary>
    public const string CompanyType = "Proxy Pack";

    public const int ExtraSpecOpsXp = 10;

    /// <summary>The Lone Wolf earns two perks a rank, where other troopers earn one.</summary>
    public const int PerksPerRank = 2;

    /// <summary>The renown the Lone Wolf adds each time it ranks up.</summary>
    public const int RenownPerRank = 10;

    public const int MaxAspects = 4;

    public const string TransferConsciousness = "Transfer Consciousness";

    public const string TacticalAwareness = "Tactical Awareness";

    /// <summary>The skills the Lone Wolf gains on top of its profile's.</summary>
    public static IReadOnlyList<string> LoneWolfSkills { get; } = [TransferConsciousness, TacticalAwareness];

    /// <summary>
    /// How many Aspects (loadouts) the Lone Wolf has at a rank level, its own included: 1 to start, 2 at level 2,
    /// 3 at level 3 and 4 at level 5.
    /// </summary>
    public static int AspectsAt(int rankLevel) => rankLevel switch
    {
        >= 5 => MaxAspects,
        >= 3 => 3,
        2 => 2,
        _ => 1
    };

    /// <summary>Whether a saved company type is a Proxy Pack.</summary>
    public static bool Is(string? companyType) =>
        string.Equals(companyType?.Trim(), CompanyType, StringComparison.OrdinalIgnoreCase);
}
