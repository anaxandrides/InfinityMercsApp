namespace InfinityMercsApp.Api.Leagues;

/// <summary>A league as read, with the ETag a replacement must match.</summary>
public sealed record StoredLeague(LeagueDocument League, string ETag);
