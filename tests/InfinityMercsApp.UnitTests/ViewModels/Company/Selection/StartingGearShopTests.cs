using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.SpecOps;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.ViewModels.Company.Selection;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class StartingGearShopTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private static StartingGearOption Option(string name, int cost) =>
        new(name, cost, new TrooperUpgrade(name, CaptainStatBonuses.None, null, null, [name], [], [], "Starting gear"));

    private static IReadOnlyList<StartingGearOption> Options { get; } = [Option("Stun Pistol", 1), Option("Combi Rifle", 5), Option("Boarding Shotgun", 10)];

    [Fact]
    public async Task BuyAsync_PicksUntilNothingFits_OffersOnlyWhatIsLeftToAfford()
    {
        _dialogs.ChooseAsync(Arg.Any<string>(), StartingGearShop.Done, Arg.Any<IReadOnlyList<string>>())
            .Returns(call => call.Arg<IReadOnlyList<string>>()[^1]);

        var bought = await new StartingGearShop(_dialogs).BuyAsync("Lone Wolf", 10, Options);

        Assert.Equal(["Boarding Shotgun"], bought.Select(upgrade => upgrade.Label));
    }

    [Fact]
    public async Task BuyAsync_CheapPicks_KeepsBuyingWhileCreditsLast()
    {
        _dialogs.ChooseAsync(Arg.Any<string>(), StartingGearShop.Done, Arg.Any<IReadOnlyList<string>>())
            .Returns(call => call.Arg<IReadOnlyList<string>>()[0]);

        var bought = await new StartingGearShop(_dialogs).BuyAsync("Lone Wolf", 10, Options);

        Assert.Equal(["Stun Pistol", "Combi Rifle"], bought.Select(upgrade => upgrade.Label));
        await _dialogs.Received(1).ChooseAsync(Arg.Is<string>(title => title.Contains("9 of 10")), StartingGearShop.Done,
            Arg.Is<IReadOnlyList<string>>(labels => labels.SequenceEqual(new[] { "Combi Rifle" })));
        await _dialogs.Received(2).ChooseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task BuyAsync_Done_KeepsWhatWasBought()
    {
        var bought = await new StartingGearShop(_dialogs).BuyAsync("Lone Wolf", 10, Options);

        Assert.Empty(bought);
    }
}
