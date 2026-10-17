using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.Presentation.Season.RankUp;

/// <summary>A perk the trooper can take, as listed: its text and where it sits.</summary>
public sealed record PerkOption(PerkPick Pick)
{
    public string Name => Pick.Name;

    public string PlaceText => $"{Pick.ListName} · Track {Pick.Track} · Tier {Pick.Tier}";
}
