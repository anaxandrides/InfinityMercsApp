namespace InfinityMercsApp.Services.Sync;

/// <summary>How many companies and seasons on this device aren't in the signed-in account yet.</summary>
public sealed record UnclaimedFiles(int Companies, int Seasons)
{
    public static UnclaimedFiles None { get; } = new(0, 0);

    public int Total => Companies + Seasons;
}
