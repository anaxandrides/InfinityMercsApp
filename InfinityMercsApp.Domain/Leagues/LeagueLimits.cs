namespace InfinityMercsApp.Domain.Leagues;

/// <summary>How large a league and what is said in it may be, so a league stays one document.</summary>
public static class LeagueLimits
{
    public const int NameLength = 60;

    public const int DisplayNameLength = 40;

    public const int MissionLength = 80;

    public const int Members = 64;

    public const int Entries = 256;

    public const int Games = 2000;

    public const int MinSides = 2;

    public const int MaxSides = 4;

    /// <summary>The most objective points a side can score in a game.</summary>
    public const int ObjectivePoints = 10;

    public const int Renown = 100_000;

    public const int SnapshotIdLength = 64;
}
