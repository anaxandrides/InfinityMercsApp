using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.SpecOps;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.SpecOps;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.ViewModels.Company.Selection;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class SpecialOperativeChoicesTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly SpecialOperativeChoices _choices;

    public SpecialOperativeChoicesTests() => _choices = new SpecialOperativeChoices(_dialogs);

    private static TrooperUpgrade Upgrade(string label) => new(label, CaptainStatBonuses.None, null, null, [], [], []);

    private static IReadOnlyList<TrooperUpgrade> Table { get; } = [Upgrade("+3 BTS"), Upgrade("+1 WIP"), Upgrade("Spitfire")];

    [Fact]
    public async Task ChooseUpgradesAsync_Two_NeverOffersTheFirstAgain()
    {
        _dialogs.ChooseAsync(Arg.Any<string>(), "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns(call => call.Arg<IReadOnlyList<string>>()[0]);

        var picked = await _choices.ChooseUpgradesAsync("Indigo", Table, 2);

        Assert.Equal(["+3 BTS", "+1 WIP"], picked?.Select(upgrade => upgrade.Label));
        await _dialogs.Received(1).ChooseAsync("Indigo: upgrade 2 of 2", "Cancel",
            Arg.Is<IReadOnlyList<string>>(labels => labels.SequenceEqual(new[] { "+1 WIP", "Spitfire" })));
    }

    [Fact]
    public async Task ChooseUpgradesAsync_Cancelled_IsNull() =>
        Assert.Null(await _choices.ChooseUpgradesAsync("Indigo", Table, 2));

    [Fact]
    public async Task ChooseUpgradesAsync_TableSmallerThanAsked_TakesWhatThereIs()
    {
        _dialogs.ChooseAsync(Arg.Any<string>(), "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns(call => call.Arg<IReadOnlyList<string>>()[0]);

        var picked = await _choices.ChooseUpgradesAsync("Indigo", [Upgrade("+3 BTS")], 2);

        Assert.Equal(["+3 BTS"], picked?.Select(upgrade => upgrade.Label));
    }

    [Fact]
    public async Task ChooseUpgradesAsync_OneUpgrade_AsksForAnUpgrade()
    {
        _dialogs.ChooseAsync(Arg.Any<string>(), "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns("+1 WIP");

        var picked = await _choices.ChooseUpgradesAsync("Indigo-1", Table, 1);

        Assert.Equal("+1 WIP", Assert.Single(picked ?? []).Label);
        await _dialogs.Received(1).ChooseAsync("Indigo-1: choose an upgrade", "Cancel", Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task ChooseSpecBotAsync_NoneOffered_DoesNotAsk()
    {
        Assert.Null(await _choices.ChooseSpecBotAsync("Indigo", []));
        await _dialogs.DidNotReceive().ChooseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IReadOnlyList<string>>());
    }

    [Fact]
    public async Task ChooseSpecBotAsync_Picked_ReturnsIt()
    {
        var specBot = new SpecBotOption("BLUEBERRY SPECBOT (6 pts)", 6, new PeripheralMercsCompanyStats());
        _dialogs.ChooseAsync("Does Indigo bring a SpecBot?", "No SpecBot", Arg.Any<IReadOnlyList<string>>()).Returns(specBot.Label);

        Assert.Same(specBot, await _choices.ChooseSpecBotAsync("Indigo", [specBot]));
    }
}
