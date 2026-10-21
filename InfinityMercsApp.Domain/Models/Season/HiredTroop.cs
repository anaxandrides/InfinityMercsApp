namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>A trooper hired with inducements for one contract, in the army loadout picked.</summary>
public class HiredTroop
{
    /// <summary>Its place among the contract's troopers: negative, so it never clashes with a company entry.</summary>
    public int EntryIndex { get; set; }

    /// <summary>The Troops for Hire row it was hired under, e.g. "Digger".</summary>
    public string Offer { get; set; } = string.Empty;

    /// <summary>The loadout, e.g. "Digger: Rifle + Light Shotgun".</summary>
    public string Name { get; set; } = string.Empty;

    public int FactionId { get; set; }
    public int UnitId { get; set; }
    public int GroupId { get; set; }
    public int OptionId { get; set; }
    public int Cost { get; set; }

    /// <summary>Skills the hire adds to the profile, e.g. the Anaconda's Immunity (POS).</summary>
    public List<string> Skills { get; set; } = [];
}
