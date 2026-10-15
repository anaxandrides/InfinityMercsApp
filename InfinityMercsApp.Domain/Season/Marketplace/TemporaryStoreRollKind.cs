namespace InfinityMercsApp.Domain.Season.Marketplace;

/// <summary>What a roll for the round's temporary store came to.</summary>
public enum TemporaryStoreRollKind
{
    /// <summary>A store the company does not already have.</summary>
    Found,

    /// <summary>A store the company already has; another is needed.</summary>
    AlreadyOwned,

    /// <summary>19 or 20: the company picks any store.</summary>
    ChooseAny,

    /// <summary>Every reroll landed on a store the company has; it picks any store instead.</summary>
    Exhausted,

    /// <summary>A number the table has no store for.</summary>
    Invalid
}
