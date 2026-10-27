namespace InfinityMercsApp.Services.CompanySelection.Factions;

/// <summary>A faction's Core fireteams a Cohesive company can field (CoreFireteamEligibility), by name.</summary>
public sealed record FactionFireteams(int FactionId, IReadOnlyList<string> ValidCoreFireteams);
