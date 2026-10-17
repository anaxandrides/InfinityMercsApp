using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Byes;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Season.Bye;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;
using InfinityMercsApp.ViewModels.Season;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Bye;

public sealed class ByeRoundViewModelTests : IDisposable
{
    private const string SeasonPath = "/seasons/s.json";

    private const string Company = """
        {
          "entries": [
            { "entryIndex": 1, "name": "Trooper", "customName": "Kestrel", "isLieutenant": true, "sourceFactionId": 101 },
            { "entryIndex": 2, "name": "Fusilier", "baseUnitName": "Fusilier", "sourceFactionId": 101 },
            { "entryIndex": 3, "isPeripheralUnit": true, "sourceFactionId": 101, "Custom Characteristics": [ "Peripheral (Servant)" ] },
            { "entryIndex": 4, "name": "Bolt", "baseUnitName": "Bolt", "sourceFactionId": 101 },
            { "entryIndex": 5, "name": "Alpha", "baseUnitName": "Alpha", "sourceFactionId": 101 },
            { "entryIndex": 6, "name": "Bravo", "baseUnitName": "Bravo", "sourceFactionId": 101 },
            { "entryIndex": 7, "name": "Charlie", "baseUnitName": "Charlie", "sourceFactionId": 101 }
          ]
        }
        """;

    private readonly TempRecords _records = new();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly MissionDebrief _debrief = new();
    private readonly SeasonFile _season = new() { Rounds = [new SeasonRound { RoundIndex = 1 }, new SeasonRound { RoundIndex = 2 }] };
    private readonly string _companyPath;

    public ByeRoundViewModelTests()
    {
        _companyPath = _records.WriteCompany("c.json", Company);
        _season.TrooperFor(2).Injuries.AddRange([Injury.Arms, Injury.EyesInjury]);
        _seasonFiles.LoadAsync(SeasonPath).Returns(_ => _season);
        _seasonFiles.UpdateAsync(SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(call =>
        {
            call.Arg<Action<SeasonFile>>()(_season);
            return true;
        });
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).ReturnsForAnyArgs(true);
    }

    public void Dispose() => _records.Dispose();

    private async Task<ByeRoundViewModel> OpenAsync()
    {
        var roster = new DeploymentRoster(Army101Data.Create(), _seasonFiles, new StoreCatalog(Substitute.For<IStoreProvider>()));
        var viewModel = new ByeRoundViewModel(_navigation, _dialogs, _seasonFiles, roster, _debrief);
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(_companyPath, SeasonPath));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static void Tick(ByeRoundViewModel viewModel, params IEnumerable<int> entryIndices)
    {
        foreach (var index in entryIndices)
        {
            viewModel.ToggleTrooperCommand.Execute(viewModel.Troopers.Single(trooper => trooper.EntryIndex == index));
        }
    }

    [Fact]
    public async Task ListsTheTroopersAndTheirInjuries()
    {
        var viewModel = await OpenAsync();

        Assert.Equal(3, viewModel.RoundNumber);
        Assert.Equal(30 + 12, viewModel.Credits);
        Assert.Equal([1, 2, 4, 5, 6, 7], viewModel.Troopers.Select(trooper => trooper.EntryIndex));
        Assert.All(viewModel.Troopers, trooper => Assert.False(trooper.IsSelected));
        Assert.True(viewModel.HasInjuries);
        Assert.Equal([Injury.Arms, Injury.EyesInjury], viewModel.InjuryOptions.Select(option => option.Injury));
        Assert.Equal(viewModel.InjuryOptions[0], viewModel.SelectedInjury);
        Assert.Equal("0 / 4 troopers · 7 XP each", viewModel.SelectionText);
        Assert.True(viewModel.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public async Task NoMoreThanFourCanBeTickedAtSevenXp()
    {
        var viewModel = await OpenAsync();

        Tick(viewModel, 1, 2, 4, 5, 6);

        Assert.Equal([1, 2, 4, 5], viewModel.Troopers.Where(trooper => trooper.IsSelected).Select(trooper => trooper.EntryIndex));
        Assert.Equal("4 / 4 troopers · 7 XP each", viewModel.SelectionText);
    }

    [Fact]
    public async Task TickingAgainUnticks()
    {
        var viewModel = await OpenAsync();

        Tick(viewModel, 2, 2);

        Assert.All(viewModel.Troopers, trooper => Assert.False(trooper.IsSelected));
    }

    [Fact]
    public async Task GoingBackToFourWithSixTickedCannotBeConfirmed()
    {
        var viewModel = await OpenAsync();
        viewModel.SelectSplitCommand.Execute(ByeXpSplit.SixTroopers);
        Tick(viewModel, 1, 2, 4, 5, 6, 7);
        Assert.Equal("6 / 6 troopers · 5 XP each", viewModel.SelectionText);
        Assert.True(viewModel.IsSixTroopers);

        viewModel.SelectSplitCommand.Execute(ByeXpSplit.FourTroopers);

        Assert.False(viewModel.IsValid);
        Assert.False(viewModel.ConfirmCommand.CanExecute(null));
    }

    [Fact]
    public async Task ConfirmingRecordsTheByeAndGoesToDowntime()
    {
        var viewModel = await OpenAsync();
        Tick(viewModel, 2, 4);
        viewModel.SelectedInjury = viewModel.InjuryOptions[1];

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        var bye = _season.Rounds[^1];
        Assert.Equal(3, bye.RoundIndex);
        Assert.True(bye.MissionResults.IsBye);
        Assert.Equal([2, 4], bye.MissionResults.UnitResults.Select(result => result.EntryIndex ?? 0));
        Assert.All(bye.MissionResults.UnitResults, result => Assert.Equal(7, result.XpGained));
        Assert.Equal([Injury.Arms], _season.Troopers[2].Injuries);
        Assert.Equal([1, 2, 4, 5, 6, 7], _debrief.Units.Select(unit => unit.EntryIndex));
        Assert.All(_debrief.Units, unit => Assert.Equal(0, unit.DeploymentXp));
        await _navigation.Received(1).PushAsync("DowntimePage", Arg.Is<IDictionary<string, object>>(parameters =>
            (string)parameters[NavigationParameters.SeasonFilePath] == SeasonPath));
    }

    [Fact]
    public async Task DecliningTheConfirmationRecordsNothing()
    {
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>()).ReturnsForAnyArgs(false);
        var viewModel = await OpenAsync();

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        await _seasonFiles.DidNotReceiveWithAnyArgs().UpdateAsync(Arg.Any<string?>(), Arg.Any<Action<SeasonFile>>());
        await _navigation.DidNotReceiveWithAnyArgs().PushAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>?>());
    }

    [Fact]
    public async Task AFailedSaveSaysSoAndStays()
    {
        _seasonFiles.UpdateAsync(SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(false);
        var viewModel = await OpenAsync();

        await viewModel.ConfirmCommand.ExecuteAsync(null);

        await _dialogs.Received(1).ShowAlertAsync("Bye Not Recorded", SeasonMessages.NotSaved);
        await _navigation.DidNotReceiveWithAnyArgs().PushAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>?>());
    }
}
