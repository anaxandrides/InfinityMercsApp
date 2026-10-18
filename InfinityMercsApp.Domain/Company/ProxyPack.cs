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

    public const string TransferConsciousness = "Transfer Consciousness";

    public const string TacticalAwareness = "Tactical Awareness";

    /// <summary>The skills the Lone Wolf gains on top of its profile's.</summary>
    public static IReadOnlyList<string> LoneWolfSkills { get; } = [TransferConsciousness, TacticalAwareness];

    /// <summary>Whether a saved company type is a Proxy Pack.</summary>
    public static bool Is(string? companyType) =>
        string.Equals(companyType?.Trim(), CompanyType, StringComparison.OrdinalIgnoreCase);
}
