namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>A perk saved with the company that Forced Retraining replaced this season.</summary>
public class PerkSwap
{
    /// <summary>The perk node id saved with the company.</summary>
    public string Replaced { get; set; } = string.Empty;

    /// <summary>The perk node id the trooper has in its place.</summary>
    public string Replacement { get; set; } = string.Empty;
}
