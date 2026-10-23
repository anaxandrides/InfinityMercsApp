namespace InfinityMercsApp.Services.Sync;

/// <summary>How many companies and seasons, e.g. online, or on this device but not in the account.</summary>
public sealed record FileCounts(int Companies, int Seasons)
{
    public static FileCounts None { get; } = new(0, 0);

    public int Total => Companies + Seasons;

    /// <summary>"3 companies and 1 season", leaving out a kind there are none of; "nothing" for none at all.</summary>
    public string Describe()
    {
        var parts = new List<string>();
        if (Companies > 0)
        {
            parts.Add($"{Companies} {(Companies == 1 ? "company" : "companies")}");
        }

        if (Seasons > 0)
        {
            parts.Add($"{Seasons} {(Seasons == 1 ? "season" : "seasons")}");
        }

        return parts.Count == 0 ? "nothing" : string.Join(" and ", parts);
    }
}
