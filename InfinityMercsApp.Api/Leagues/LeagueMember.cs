namespace InfinityMercsApp.Api.Leagues;

/// <summary>A league member as the league keeps them.</summary>
/// <param name="MemberId">The member's id within the league, shown to other members instead of their account.</param>
/// <param name="UserId">The member's account, which only the API sees.</param>
public sealed record LeagueMember(string MemberId, string UserId, string DisplayName, DateTimeOffset JoinedAt);
