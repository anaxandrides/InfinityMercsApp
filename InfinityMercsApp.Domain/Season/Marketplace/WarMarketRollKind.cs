namespace InfinityMercsApp.Domain.Season.Marketplace;

/// <summary>What a roll for the round's War Market came to.</summary>
public enum WarMarketRollKind
{
    /// <summary>A market the company could not have chosen for its factions.</summary>
    Found,

    /// <summary>One of the company's own faction markets, which it could have chosen; roll again.</summary>
    FactionMarket,

    /// <summary>19 or 20: the company picks any market.</summary>
    ChooseAny,

    /// <summary>Every reroll landed on a faction market; the company picks any market instead.</summary>
    Exhausted,

    /// <summary>A number the table has no market for.</summary>
    Invalid
}
