namespace InfinityMercsApp.Infrastructure.Providers.CompanyFactions;

/// <summary>What the generated companies' factions share.</summary>
public static class GeneratedCompanies
{
    /// <summary>
    /// The version of the generated companies' rules (who may join the Airborne, Inspiring and TAG companies). Raise it
    /// whenever a generator changes, so the app rebuilds its stored companies and the API the ones it serves.
    /// </summary>
    public const string RulesVersion = "2";

    /// <summary>The generated factions' ids.</summary>
    public static IReadOnlyList<int> FactionIds { get; } =
    [
        AirborneCompanyFactionGenerator.AirborneCompanyFactionId,
        InspiringCompanyFactionGenerator.InspiringCompanyFactionId,
        TagCompanyFactionGenerator.TagCompanyFactionId
    ];

    public static bool IsGenerated(int factionId) => FactionIds.Contains(factionId);
}
