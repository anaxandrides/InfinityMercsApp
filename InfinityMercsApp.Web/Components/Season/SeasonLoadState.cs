namespace InfinityMercsApp.Web.Components.Season;

/// <summary>How far the season page has got opening its season.</summary>
public enum SeasonLoadState
{
    Loading,

    /// <summary>The season isn't in the account (or hasn't synced), or nothing was opened.</summary>
    Missing,

    Shown
}
