namespace InfinityMercsApp.Services.CompanySelection.Factions;

/// <summary>The sectorials a Cohesive company can be founded from at one points limit, with no unit filter.</summary>
/// <param name="Factions">Only the sectorials with at least one valid Core fireteam.</param>
public sealed record CohesiveListing(int Points, IReadOnlyList<FactionFireteams> Factions);
