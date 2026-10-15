namespace InfinityMercsApp.Domain.Models.Season;

/// <summary>What a company brings into its season: CR left over from recruiting, and its captain's SWC bonus.</summary>
public class SeasonStartingFunds
{
    public int Credits { get; set; }
    public double Swc { get; set; }
}
