using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class ExperienceViewModelTests
{
    private const string SeasonPath = "season.json";

    private readonly FakeNavigationService _navigation = new();
    private readonly FakeSeasonFileService _seasonFiles = new();
    private readonly MissionDebrief _debrief = new();

    private async Task<ExperienceViewModel> OpenAsync(params ExperienceUnitResult[] units)
    {
        _debrief.Units = [.. units];
        _seasonFiles.Files[SeasonPath] = new SeasonFile { Rounds = [new SeasonRound { RoundIndex = 1 }] };
        var viewModel = new ExperienceViewModel(_navigation, _seasonFiles, _debrief);
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany("company.json", SeasonPath));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task ConfirmingWaitsForAnMvp()
    {
        var viewModel = await OpenAsync(new() { Name = "A", DeploymentXp = 1 }, new() { Name = "B", DeploymentXp = 1 });
        Assert.False(viewModel.ConfirmCommand.CanExecute(null));

        viewModel.Units[1].IsMvp = true;
        Assert.True(viewModel.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public async Task OnlyOneTrooperCanBeMvp()
    {
        var viewModel = await OpenAsync(new() { Name = "A", DeploymentXp = 1 }, new() { Name = "B", DeploymentXp = 1 });

        viewModel.Units[0].IsMvp = true;
        viewModel.Units[1].IsMvp = true;

        Assert.False(viewModel.Units[0].IsMvp);
        Assert.True(viewModel.Units[1].IsMvp);
        Assert.Equal("Total: 3 XP", viewModel.Units[1].TotalText);
        Assert.Equal("Total: 1 XP", viewModel.Units[0].TotalText);
    }

    [Fact]
    public async Task ConfirmingRecordsXpOnTheLatestRoundAndOpensDowntime()
    {
        var viewModel = await OpenAsync(
            new() { Name = "Benched", DeploymentXp = 0 },
            new() { Name = "Hero", DeploymentXp = 2, IsConsciousAtEnd = true });
        viewModel.Units[1].IsMvp = true;

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        var result = Assert.Single(_seasonFiles.Files[SeasonPath].Rounds[0].MissionResults.UnitResults);
        Assert.Equal(("Hero", 6), (result.UnitName, result.XpGained));
        Assert.Equal("DowntimePage", Assert.Single(_navigation.Navigations).Route);
    }

    [Fact]
    public async Task WithNoTroopersConfirmingIsAllowed()
    {
        var viewModel = await OpenAsync();
        Assert.True(viewModel.ConfirmCommand.CanExecute(null));
    }
}
