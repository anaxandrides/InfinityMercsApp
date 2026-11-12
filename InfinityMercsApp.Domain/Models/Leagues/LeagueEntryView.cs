namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>A company season entered in a league.</summary>
public class LeagueEntryView
{
    public string SeasonId { get; set; } = string.Empty;

    /// <summary>The member whose company it is.</summary>
    public string MemberId { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    /// <summary>The league season it was entered in.</summary>
    public int LeagueSeason { get; set; }

    /// <summary>The company's renown as its owner's app last reported it.</summary>
    public int Renown { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Whether it belongs to the user asking.</summary>
    public bool IsYours { get; set; }
}
