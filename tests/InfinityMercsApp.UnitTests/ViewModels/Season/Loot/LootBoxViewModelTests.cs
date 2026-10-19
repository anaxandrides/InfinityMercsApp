using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Loot;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Rolls;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Season.Loot;
using InfinityMercsApp.ViewModels.Season.Loot.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Loot;

public sealed class LootBoxViewModelTests
{
    private const string SeasonPath = "/seasons/s.json";

    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly IDice _dice = Substitute.For<IDice>();
    private readonly SeasonFile _season = new() { Rounds = [new SeasonRound { RoundIndex = 1 }, new SeasonRound { RoundIndex = 2 }] };

    public LootBoxViewModelTests()
    {
        _seasonFiles.LoadAsync(SeasonPath).Returns(_ => _season);
        _seasonFiles.SaveAsync(SeasonPath, Arg.Any<SeasonFile?>()).Returns(true);
        _dialogs.ChooseAsync("Loot Box Gained", "Cancel", Arg.Any<IReadOnlyList<string>>())
            .Returns(LootBoxTiers.Of(LootBoxTier.BlackMarketCache).Display);
    }

    private async Task<LootBoxViewModel> OpenAsync()
    {
        var viewModel = new LootBoxViewModel(Substitute.For<INavigationService>(), _seasonFiles, _dialogs, _dice);
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany("/companies/c.json", SeasonPath));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static LootDiceChoice Dice(LootBoxViewModel viewModel, int count) => viewModel.DiceChoices.Single(choice => choice.Dice == count);

    [Fact]
    public async Task AddBoxCommand_TierPicked_AddsABoxGainedAfterTheRoundsPlayed()
    {
        var viewModel = await OpenAsync();

        await viewModel.AddBoxCommand.ExecuteAsync(null);

        var box = Assert.Single(_season.LootBoxes);
        Assert.Equal((LootBoxTier.BlackMarketCache, 2, 6), (box.Tier, box.GainedAfterRound, box.DiceLeft));
        Assert.True(viewModel.HasBox);
        Assert.All(viewModel.DiceChoices, choice => Assert.True(choice.IsEnabled));
        Assert.False(viewModel.CanTakeLoot);
    }

    [Fact]
    public async Task AddBoxCommand_Cancelled_AddsNothing()
    {
        _dialogs.ChooseAsync("Loot Box Gained", "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns((string?)null);
        var viewModel = await OpenAsync();

        await viewModel.AddBoxCommand.ExecuteAsync(null);

        Assert.Empty(_season.LootBoxes);
        Assert.False(viewModel.HasBox);
    }

    [Fact]
    public async Task RollCommand_Weapon_SavesTheRollAndShowsTheLoot()
    {
        _dice.RollD6().Returns(3);
        var viewModel = await OpenAsync();
        await viewModel.AddBoxCommand.ExecuteAsync(null);

        await viewModel.RollCommand.ExecuteAsync(Dice(viewModel, 3));

        Assert.Equal(15, _season.LootBoxes[0].Weapon?.Result);
        Assert.Equal(("Marksman Rifle [Loot]", "3 dice left"), (viewModel.LootText, viewModel.DiceText));
        Assert.Single(viewModel.Log);
        Assert.True(viewModel.CanTakeLoot);
        await _seasonFiles.Received(2).SaveAsync(SeasonPath, _season);
    }

    [Fact]
    public async Task TakeLootCommand_DiceLeftAndConfirmed_PutsTheLootInTheRewards()
    {
        _dice.RollD6().Returns(3);
        _dialogs.ConfirmAsync("Take the Loot", Arg.Any<string>()).Returns(true);
        var viewModel = await OpenAsync();
        await viewModel.AddBoxCommand.ExecuteAsync(null);
        await viewModel.RollCommand.ExecuteAsync(Dice(viewModel, 3));

        await viewModel.TakeLootCommand.ExecuteAsync(null);

        Assert.True(_season.LootBoxes[0].IsOpened);
        Assert.False(viewModel.HasBox);
        Assert.Equal("Marksman Rifle [Loot]", Assert.Single(viewModel.Rewards).Name);
    }

    [Fact]
    public async Task TakeLootCommand_Declined_KeepsOpeningTheBox()
    {
        _dice.RollD6().Returns(3);
        _dialogs.ConfirmAsync("Take the Loot", Arg.Any<string>()).Returns(false);
        var viewModel = await OpenAsync();
        await viewModel.AddBoxCommand.ExecuteAsync(null);
        await viewModel.RollCommand.ExecuteAsync(Dice(viewModel, 3));

        await viewModel.TakeLootCommand.ExecuteAsync(null);

        Assert.False(_season.LootBoxes[0].IsOpened);
        Assert.True(viewModel.HasBox);
    }
}
