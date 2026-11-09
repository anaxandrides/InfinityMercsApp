namespace InfinityMercsApp.Domain.Leagues;

/// <summary>What names and ids said in a league must look like.</summary>
public static class LeagueText
{
    /// <summary>Whether <paramref name="name"/> has something besides spaces, and fits <paramref name="maxLength"/>.</summary>
    public static bool IsName(string? name, int maxLength) =>
        name is not null && name.Trim() is { Length: > 0 } trimmed && trimmed.Length <= maxLength && !trimmed.Any(char.IsControl);

    /// <summary>Whether <paramref name="snapshotId"/> is null (whatever is current) or names a snapshot as ids are named.</summary>
    public static bool IsSnapshotId(string? snapshotId) =>
        snapshotId is null ||
        (snapshotId.Length is > 0 and <= LeagueLimits.SnapshotIdLength && snapshotId.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'));
}
