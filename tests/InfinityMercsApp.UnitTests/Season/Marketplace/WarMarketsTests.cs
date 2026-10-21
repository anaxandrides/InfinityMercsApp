using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Marketplace;

namespace InfinityMercsApp.UnitTests.Season.Marketplace;

public sealed class WarMarketsTests
{
    private static readonly Store BaseMarket = new() { Name = "Base Market", Alignment = StoreAlignments.Neutral };
    private static readonly Store Medical = new() { Name = ServiceStoreNames.MedicalServices };
    private static readonly Store NumberOne = new() { Name = "Number One", AssociatedFactions = ["PanOceania", "White Company"] };
    private static readonly Store JadeTemu = new() { Name = "Jade Temu", AssociatedFactions = ["Yu Jing"] };

    [Fact]
    public void IsWarMarket_MarketWithFactions_IsTrue()
    {
        Assert.True(NumberOne.IsWarMarket);
        Assert.False(BaseMarket.IsWarMarket);
        Assert.False(Medical.IsWarMarket);
    }

    [Fact]
    public void FactionMarkets_CompanyFactions_ReturnsTheirWarMarkets()
    {
        var factions = new HashSet<string>(["White Company"], StringComparer.OrdinalIgnoreCase);

        Assert.Equal([NumberOne], WarMarkets.FactionMarkets([BaseMarket, Medical, NumberOne, JadeTemu], factions));
    }

    [Fact]
    public void Current_NewSeason_ReturnsNull()
    {
        Assert.Null(WarMarkets.Current(new SeasonFile()));
        Assert.Null(WarMarkets.Current(null));
    }

    [Fact]
    public void Current_BeforeFirstContract_ReadsTheInitialPurchases()
    {
        var season = new SeasonFile { InitialPurchases = new SeasonMarketplace { WarMarket = "Number One" } };

        Assert.Equal("Number One", WarMarkets.Current(season));
    }

    [Fact]
    public void Current_AfterAContract_ReadsTheLatestRound()
    {
        var season = new SeasonFile
        {
            InitialPurchases = new SeasonMarketplace { WarMarket = "Number One" },
            Rounds = [new SeasonRound { RoundIndex = 1, Marketplace = new SeasonMarketplace { WarMarket = "Jade Temu" } }]
        };

        Assert.Equal("Jade Temu", WarMarkets.Current(season));
    }

    [Fact]
    public void Current_NewRoundWithoutChoice_ReturnsNull()
    {
        var season = new SeasonFile
        {
            Rounds =
            [
                new SeasonRound { RoundIndex = 1, Marketplace = new SeasonMarketplace { WarMarket = "Jade Temu" } },
                new SeasonRound { RoundIndex = 2 }
            ]
        };

        Assert.Null(WarMarkets.Current(season));
    }

    [Fact]
    public void Current_OlderSeasonTemporaryStore_CountsAsTheMarket()
    {
        var season = new SeasonFile { Rounds = [new SeasonRound { RoundIndex = 1, TemporaryStore = "Jade Temu" }] };

        Assert.Equal("Jade Temu", WarMarkets.Current(season));
    }

    [Fact]
    public void CanRoll_Season_TrueAfterTheFirstContract()
    {
        Assert.False(WarMarkets.CanRoll(new SeasonFile()));
        Assert.False(WarMarkets.CanRoll(null));
        Assert.True(WarMarkets.CanRoll(new SeasonFile { Rounds = [new SeasonRound { RoundIndex = 1 }] }));
    }
}
