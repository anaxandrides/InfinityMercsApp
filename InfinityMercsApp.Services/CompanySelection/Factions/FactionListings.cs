namespace InfinityMercsApp.Services.CompanySelection.Factions;

/// <summary>
/// Which factions the company types that judge every army list, worked out ahead from a snapshot's armies (the API
/// does it when it copies them), so listing them needn't read every army: the sectorials with a lieutenant (Airborne
/// and Inspiring captains), and those with a valid Core fireteam at each starting points limit (Cohesive).
/// </summary>
public sealed record FactionListings
{
    /// <summary>The version of how these are worked out; raise it when that changes, so the API works them out again.</summary>
    public const string Version = "1";

    /// <summary>The starting points a new company picks from.</summary>
    public static IReadOnlyList<int> PointsLimits { get; } = [60, 75, 90];

    public IReadOnlyList<int> LieutenantFactionIds { get; init; } = [];

    public IReadOnlyList<CohesiveListing> Cohesive { get; init; } = [];

    /// <summary>The Cohesive listing at <paramref name="points"/>; null when none was worked out for it.</summary>
    public CohesiveListing? CohesiveAt(int points) => Cohesive.FirstOrDefault(listing => listing.Points == points);
}
