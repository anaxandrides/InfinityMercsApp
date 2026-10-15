using InfinityMercsApp.Domain.Season.Downtime;

namespace InfinityMercsApp.ViewModels.Season.Downtime;

/// <summary>An event the player can pick instead of rolling, after a quiet week.</summary>
public sealed record DowntimeEventOption(DowntimeEvent Event)
{
    public string Text => $"{Event.RollRange}  —  {Event.Description}";
}
