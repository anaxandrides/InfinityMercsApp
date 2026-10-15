using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Marketplace;

namespace InfinityMercsApp.UnitTests.Season.Marketplace;

public sealed class StoreLabelsTests
{
    [Fact]
    public void AMarketListsItsFactionsTypeAndAlignment()
    {
        var store = new Store
        {
            Name = "Jade Temu",
            AssociatedFactions = ["Yu Jing", "Japanese Secessionist Army", "Oban", "Yu Jing"],
            AssociatedType = "Mercenary, Criminal",
            Alignment = "Chaotic"
        };

        Assert.Equal("Jade Temu [YJ/JSA, Mercenary | Criminal, Chaotic]", StoreLabels.For(store));
    }

    [Fact]
    public void AnUnlistedFactionKeepsItsName()
    {
        var store = new Store { Name = "Outpost", AssociatedFactions = ["Corsairs"] };

        Assert.Equal("Outpost [Corsairs]", StoreLabels.For(store));
    }

    [Fact]
    public void AlwaysAvailableIsNotShownAsAnAlignment()
    {
        var store = new Store { Name = "Bazaar", AssociatedType = "General", Alignment = StoreAlignments.AlwaysAvailable };

        Assert.Equal("Bazaar [General]", StoreLabels.For(store));
    }

    [Fact]
    public void ANeutralStoreWithNoFactionsSaysSo()
    {
        Assert.Equal("Haven [Neutral]", StoreLabels.For(new Store { Name = "Haven", Alignment = StoreAlignments.Neutral }));
    }

    [Fact]
    public void AStoreWithNothingToAddIsJustItsName()
    {
        Assert.Equal("Corner Shop", StoreLabels.For(new Store { Name = "Corner Shop" }));
    }

    [Fact]
    public void ServicesAreListedByName()
    {
        Assert.Equal("🩺 Medical Services", StoreLabels.For(new Store { Name = ServiceStoreNames.MedicalServices }));
        Assert.Equal("Additional Recruitment", StoreLabels.For(new Store { Name = ServiceStoreNames.AdditionalRecruitment }));
    }

    [Fact]
    public void ATemporaryStoreNamesItsRound()
    {
        Assert.Equal("Jade Temu - Round 2 Store", StoreLabels.ForTemporary("Jade Temu", 2));
    }

    [Theory]
    [InlineData("Jade Temu [YJ/JSA, Chaotic]", "Jade Temu")]
    [InlineData("Jade Temu - Round 2 Store", "Jade Temu")]
    [InlineData("+ Medical Services", "Medical Services")]
    [InlineData("🩺 Medical Services", "Medical Services")]
    [InlineData("  Number One  ", "Number One")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void LabelsReadBackToTheStoreName(string? label, string expected)
    {
        Assert.Equal(expected, StoreLabels.StoreName(label));
    }
}
