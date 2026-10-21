namespace InfinityMercsApp.Domain.Models.Season;

public class SeasonMarketplace
{
    public List<string> Stores { get; set; } = [];
    public List<SeasonTransaction> Transactions { get; set; } = [];

    /// <summary>The War Market open for this round's shopping, once chosen or rolled.</summary>
    public string WarMarket { get; set; } = string.Empty;
}
