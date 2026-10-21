namespace InfinityMercsApp.Domain.Season.Downtime;

/// <summary>One way to resolve a downtime event: the test to roll, what happens, and its traits.</summary>
public sealed class DowntimeChoice
{
    public DowntimeTest? Test { get; init; }
    public string TestLabel => Test?.Display ?? string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<DowntimeTrait> Traits { get; init; } = [];
}
