using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Season.Inducements;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Season.Inducements;
using InfinityMercsApp.ViewModels.Season.Inducements.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Inducements;

public sealed class InducementShopViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ContractInducements _inducements = new() { Received = 40 };
    private readonly InducementShopViewModel _shop;

    public InducementShopViewModelTests()
    {
        _shop = new InducementShopViewModel(_dialogs, new HiredTroops(Army101Data.Create()));
        _shop.Update(_inducements, [new ContractTrooper(0, "Lt"), new ContractTrooper(1, "Bolt")], companySlots: 2);
    }

    private HireOfferItem Hire(string profile) => _shop.HireOffers.First(offer => offer.Offer.Profile == profile);

    private RentalOfferItem Rental(string name) => _shop.RentalOffers.First(offer => offer.Name == name);

    // The Bounty Hunter's SMG or BSG row has two PanOceania loadouts: Boarding Shotgun, then Submachine Gun.
    [Fact]
    public async Task HireCommand_SeveralLoadouts_HiresThePickedOne()
    {
        _dialogs.ChooseAsync("Hire Authorized Bounty Hunter (SMG or BSG)", "Cancel", Arg.Any<IReadOnlyList<string>>())
            .Returns(call => call.ArgAt<IReadOnlyList<string>>(2)[1]);

        await _shop.HireCommand.ExecuteAsync(Hire("SMG or BSG"));

        var troop = Assert.Single(_inducements.Hired);
        Assert.Equal((-1, 5, 10), (troop.EntryIndex, troop.OptionId, troop.Cost));
        Assert.Equal((30, 1), (_shop.Left, _shop.HiredCount));
        Assert.Equal("Hired: Authorized Bounty Hunter", _shop.Summary);
    }

    [Fact]
    public async Task HireCommand_PickCancelled_HiresNothing()
    {
        _dialogs.ChooseAsync(Arg.Any<string>(), "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns((string?)null);

        await _shop.HireCommand.ExecuteAsync(Hire("SMG or BSG"));

        Assert.Empty(_inducements.Hired);
    }

    [Fact]
    public async Task HireCommand_NoRoomLeft_HiresNothing()
    {
        _shop.Update(_inducements, [], companySlots: 6);
        _inducements.Hired.Add(new HiredTroop { EntryIndex = -1, Cost = 10 });
        _shop.Update(_inducements, [], companySlots: 6);

        await _shop.HireCommand.ExecuteAsync(Hire("SMG or BSG"));

        Assert.Single(_inducements.Hired);
        Assert.All(_shop.HireOffers, offer => Assert.False(offer.IsEnabled));
        await _dialogs.DidNotReceive().ChooseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task RentCommand_TrooperPicked_RentsIntoItsSlot()
    {
        _dialogs.ChooseAsync("Rent Heavy Machine Gun", "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns("Bolt");

        await _shop.RentCommand.ExecuteAsync(Rental("Heavy Machine Gun"));

        var rental = Assert.Single(_inducements.Rentals);
        Assert.Equal((1, "Primary", 15), (rental.EntryIndex, rental.Slot, rental.Cost));
        Assert.False(Rental("Heavy Machine Gun").IsEnabled);
        Assert.Equal("Heavy Machine Gun: Bolt (Primary)", Assert.Single(_shop.Rentals).Text);
    }

    [Fact]
    public async Task RentCommand_SlotAlreadyRented_SaysSoAndRentsNothing()
    {
        _inducements.Rentals.Add(new RentedGear { ItemName = "Spitfire", Slot = "Primary", EntryIndex = 1, Cost = 10 });
        _dialogs.ChooseAsync("Rent AP Marksman Rifle", "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns("Bolt");

        await _shop.RentCommand.ExecuteAsync(Rental("AP Marksman Rifle"));

        Assert.Single(_inducements.Rentals);
        await _dialogs.Received(1).ShowAlertAsync("Slot Already Rented", Arg.Is<string>(text => text.Contains("Spitfire")));
    }

    [Fact]
    public void BuyCommandTokenCommand_TwoBought_CannotBuyMore()
    {
        _shop.BuyCommandTokenCommand.Execute(null);
        _shop.BuyCommandTokenCommand.Execute(null);

        Assert.Equal(2, _inducements.CommandTokens);
        Assert.False(_shop.BuyCommandTokenCommand.CanExecute(null));
        Assert.Equal("2 command tokens", _shop.Summary);
    }

    [Fact]
    public void DismissCommand_HiredTroop_ReturnsItsGear()
    {
        _inducements.Hired.Add(new HiredTroop { EntryIndex = -1, Offer = "Digger", Cost = 20 });
        _inducements.Rentals.Add(new RentedGear { ItemName = "Panzerfaust", Slot = "Secondary", EntryIndex = -1, Cost = 5 });
        _shop.Update(_inducements, [], companySlots: 2);

        _shop.DismissCommand.Execute(_shop.Hired[0]);

        Assert.Empty(_inducements.Rentals);
        Assert.Equal(40, _shop.Left);
    }
}
