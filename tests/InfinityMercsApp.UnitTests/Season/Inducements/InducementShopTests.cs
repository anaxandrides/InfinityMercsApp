using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Inducements;

namespace InfinityMercsApp.UnitTests.Season.Inducements;

public sealed class InducementShopTests
{
    private static RentalOffer Rental(string name) => RentalChart.Offers.First(offer => offer.Name == name);

    private static HiredTroopOffer Digger => HiredTroopChart.Offers.First(offer => offer.Name == "Digger");

    [Fact]
    public void Left_Purchases_TakesEverythingBought()
    {
        var inducements = new ContractInducements
        {
            Received = 50,
            Hired = [new HiredTroop { EntryIndex = -1, Cost = 20 }],
            CommandTokens = 1,
            Rentals = [new RentedGear { ItemName = "Panzerfaust", Cost = 5 }]
        };

        Assert.Equal((35, 15), (InducementShop.Spent(inducements), InducementShop.Left(inducements)));
    }

    [Fact]
    public void CanHire_SevenTroopersDeployed_IsFalse()
    {
        var inducements = new ContractInducements { Received = 100, Hired = [new HiredTroop { EntryIndex = -1, Cost = 20 }] };

        Assert.True(InducementShop.CanHire(inducements, Digger, companySlots: 5));
        Assert.False(InducementShop.CanHire(inducements, Digger, companySlots: 6));
    }

    [Fact]
    public void CanHire_TooFewInducements_IsFalse() =>
        Assert.False(InducementShop.CanHire(new ContractInducements { Received = 15 }, Digger, companySlots: 1));

    [Fact]
    public void CanBuyCommandToken_TwoBought_IsFalse()
    {
        var inducements = new ContractInducements { Received = 100, CommandTokens = 2 };

        Assert.False(InducementShop.CanBuyCommandToken(inducements));
    }

    [Fact]
    public void CanRent_AvaUsedUp_IsFalse()
    {
        var inducements = new ContractInducements
        {
            Received = 100,
            Rentals = [new RentedGear { ItemName = "Heavy Machine Gun", Slot = "Primary", EntryIndex = 1, Cost = 15 }]
        };

        Assert.False(InducementShop.CanRent(inducements, Rental("Heavy Machine Gun")));
        Assert.True(InducementShop.CanRent(inducements, Rental("AP Marksman Rifle")));
    }

    [Fact]
    public void Dismiss_HiredTroop_ReturnsItsRentals()
    {
        var inducements = new ContractInducements
        {
            Hired = [new HiredTroop { EntryIndex = -1 }, new HiredTroop { EntryIndex = -2 }],
            Rentals = [new RentedGear { EntryIndex = -1 }, new RentedGear { EntryIndex = 3 }]
        };

        InducementShop.Dismiss(inducements, -1);

        Assert.Equal(-2, Assert.Single(inducements.Hired).EntryIndex);
        Assert.Equal(3, Assert.Single(inducements.Rentals).EntryIndex);
        Assert.Equal(-3, InducementShop.NextHiredIndex(inducements));
    }

    [Fact]
    public void KeepDeployed_TrooperLeftHome_ReturnsItsRentalButNotTheHires()
    {
        var inducements = new ContractInducements
        {
            Rentals = [new RentedGear { EntryIndex = 1 }, new RentedGear { EntryIndex = 2 }, new RentedGear { EntryIndex = -1 }]
        };

        InducementShop.KeepDeployed(inducements, new HashSet<int> { 0, 2 });

        Assert.Equal([2, -1], inducements.Rentals.Select(rental => rental.EntryIndex));
    }

    [Fact]
    public void WithRentals_RentalInAFilledSlot_TakesItsPlace()
    {
        var gear = new List<SeasonUnitGear>
        {
            new() { Slot = "Primary", ItemName = "Combi Rifle" },
            new() { Slot = "Sidearm", ItemName = "Grenades" }
        };

        var withRentals = InducementShop.WithRentals(gear, [new RentedGear { Slot = "Primary", ItemName = "Spitfire" }]);

        Assert.Equal(["Grenades", "Spitfire"], withRentals.Select(item => item.ItemName));
        Assert.Equal(GearSlot.Primary.Key(), withRentals[1].Slot);
    }
}
