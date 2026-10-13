using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class MissionOutcomeViewModelTests
{
    private const string SeasonPath = "season.json";
    private const string CompanyPath = "company.json";

    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private Action<SeasonRound>? _roundUpdate;

    public MissionOutcomeViewModelTests() =>
        _seasonFiles.UpdateLatestRoundAsync(SeasonPath, Arg.Do<Action<SeasonRound>>(update => _roundUpdate = update)).Returns(true);

    private async Task<MissionOutcomeViewModel> OpenAsync(params int[] roundIndices)
    {
        _seasonFiles.LoadAsync(SeasonPath).Returns(new SeasonFile
        {
            Rounds = roundIndices.Select(i => new SeasonRound { RoundIndex = i }).ToList()
        });

        var viewModel = new MissionOutcomeViewModel(_navigation, _seasonFiles);
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(CompanyPath, SeasonPath));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task ShowsTheLatestRoundAndItsMissionCredits()
    {
        var viewModel = await OpenAsync(1, 2, 3);

        Assert.Equal(3, viewModel.RoundNumber);
        Assert.Equal(30, viewModel.MissionCredits);
        Assert.Equal(30, viewModel.CreditsTotal);
        Assert.Equal("0.5", viewModel.SwcText);
    }

    [Fact]
    public async Task WithoutASeasonFileScoresAsRoundOne()
    {
        var viewModel = new MissionOutcomeViewModel(_navigation, _seasonFiles);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        Assert.Equal(1, viewModel.RoundNumber);
        Assert.Equal(10, viewModel.MissionCredits);
    }

    [Fact]
    public async Task VictoryAndObjectivesAddToTheTotal()
    {
        var viewModel = await OpenAsync(2);

        viewModel.SelectVictoryCommand.Execute(null);
        viewModel.PointsScored = 3;

        Assert.True(viewModel.IsVictory);
        Assert.False(viewModel.IsDefeat);
        Assert.Equal(12, viewModel.ObjectiveCredits);
        Assert.Equal(10, viewModel.VictoryBonus);
        Assert.Equal(20 + 12 + 10, viewModel.CreditsTotal);

        viewModel.SelectDefeatCommand.Execute(null);

        Assert.True(viewModel.IsDefeat);
        Assert.Equal(0, viewModel.VictoryBonus);
        Assert.Equal(20 + 12, viewModel.CreditsTotal);
    }

    [Fact]
    public async Task ContinuingNeedsAResultAndAScore()
    {
        var viewModel = await OpenAsync(1);
        Assert.False(viewModel.GoToExperienceCommand.CanExecute(null));

        viewModel.SelectDefeatCommand.Execute(null);
        Assert.False(viewModel.GoToExperienceCommand.CanExecute(null));

        viewModel.PointsScored = 0;
        Assert.True(viewModel.GoToExperienceCommand.CanExecute(null));
    }

    [Fact]
    public async Task ContinuingSavesTheResultToTheLatestRoundAndOpensExperience()
    {
        var viewModel = await OpenAsync(1, 2);
        viewModel.SelectVictoryCommand.Execute(null);
        viewModel.PointsScored = 4;

        await viewModel.GoToExperienceCommand.ExecuteAsync(null);

        var round = new SeasonRound();
        _roundUpdate!(round);
        Assert.True(round.MissionResults.Won);
        Assert.Equal(4, round.MissionResults.OpScored);

        await _navigation.Received(1).PushAsync(
            "ExperiencePage",
            Arg.Is<IDictionary<string, object>>(p =>
                (string)p[NavigationParameters.CompanyFilePath] == CompanyPath &&
                (string)p[NavigationParameters.SeasonFilePath] == SeasonPath));
    }
}
