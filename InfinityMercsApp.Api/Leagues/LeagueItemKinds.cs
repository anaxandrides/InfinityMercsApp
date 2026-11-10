namespace InfinityMercsApp.Api.Leagues;

/// <summary>What each document in a league's partition is, kept on it as "kind" so queries can pick one sort.</summary>
public static class LeagueItemKinds
{
    public const string League = "league";

    public const string Entry = "entry";

    public const string Game = "game";
}
