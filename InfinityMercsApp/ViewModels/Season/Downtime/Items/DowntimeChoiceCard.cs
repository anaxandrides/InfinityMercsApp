using InfinityMercsApp.Domain.Season.Downtime;

namespace InfinityMercsApp.ViewModels.Season.Downtime.Items;

/// <summary>One of an event's choices, numbered from 1 as on the event card.</summary>
public sealed record DowntimeChoiceCard(int Number, DowntimeChoice Choice)
{
    public string Header => string.IsNullOrEmpty(Choice.TestLabel)
        ? $"CHOICE {Number}"
        : $"CHOICE {Number}  ({Choice.TestLabel})";

    public string Description => Choice.Description;

    public bool HasTraits => Choice.Traits.Count > 0;

    public string TraitsText => "Traits: " + string.Join(", ", Choice.Traits.Select(trait => trait.Display));
}
