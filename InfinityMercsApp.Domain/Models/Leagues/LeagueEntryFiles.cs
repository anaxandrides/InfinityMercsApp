using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Domain.Models.Leagues;

/// <summary>A company entered in a league, with its season, for the league's other members to look at.</summary>
public class LeagueEntryFiles
{
    /// <summary>The company; null when its owner has since deleted it.</summary>
    public SavedCompanyFile? Company { get; set; }

    /// <summary>The season; null when its owner has since deleted it.</summary>
    public SeasonFile? Season { get; set; }
}
