namespace InfinityMercsApp.Domain.Leagues;

/// <summary>How many may join a league, and how long what is said in it may be.</summary>
public static class LeagueLimits
{
    public const int NameLength = 60;

    public const int DisplayNameLength = 40;

    public const int MissionLength = 80;

    public const int Members = 64;

    public const int MinSides = 2;

    public const int MaxSides = 4;

    /// <summary>The most objective points a side can score in a game.</summary>
    public const int ObjectivePoints = 10;

    public const int Renown = 100_000;

    public const int SnapshotIdLength = 64;
}
