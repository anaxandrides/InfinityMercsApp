namespace InfinityMercsApp.Services.CompanySelection.Teams;

/// <summary>A fireteam as a selection screen lists it, with the units it allows.</summary>
/// <param name="Team">The fireteam; null for the bucket gathering every fireteam's wildcard slots.</param>
public sealed record TeamListing<TUnit>(string Name, MergedFireteam? Team, IReadOnlyList<TeamSlotListing<TUnit>> Slots)
    where TUnit : class
{
    public bool IsWildcardBucket => Team is null;
}
