using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Experience;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Presentation.Company.Viewer;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Season;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season;

public sealed class InjuriesViewModelTests
{
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly MissionDebrief _debrief = new();
    private readonly IDice _dice = Substitute.For<IDice>();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly ICompanyFileService _companyFiles = Substitute.For<ICompanyFileService>();
    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();
    private readonly SeasonFile _season = new() { Rounds = [new SeasonRound { RoundIndex = 1 }] };

    private bool _elite;

    private const string CompanyPath = "/companies/test.json";
    private const string SeasonPath = "/seasons/test.json";

    public InjuriesViewModelTests()
    {
        _seasonFiles.LoadAsync(SeasonPath).Returns(_ => _season);
        _seasonFiles.UpdateAsync(SeasonPath, Arg.Do<Action<SeasonFile>>(update => update(_season))).Returns(true);
        _companyFiles.Exists(CompanyPath).Returns(true);
        _companyFiles.LoadAsync(CompanyPath).Returns(new SavedCompanyFile
        {
            Entries =
            [
                new SavedCompanyEntry { EntryIndex = 0, BaseUnitName = "Trooper 0", IsLieutenant = true, CarriesCaptainUpgrades = true },
                new SavedCompanyEntry { EntryIndex = 1, BaseUnitName = "Trooper 1" },
                new SavedCompanyEntry { EntryIndex = 7, BaseUnitName = "Resting" }
            ]
        });
    }

    // PH 14 gives an avoid target of 11.
    private async Task<InjuriesViewModel> OpenAsync(params IReadOnlyList<string> phValues)
    {
        _debrief.PendingInjuries = phValues
            .Select((ph, i) => new PendingInjury { EntryIndex = i, Name = $"Trooper {i}", PhValue = ph })
            .ToList();
        _debrief.Units = phValues.Select((_, i) => new ExperienceUnitResult { EntryIndex = i, IsEliteDeployment = _elite }).ToList();

        var viewModel = new InjuriesViewModel(_navigation, _debrief, _dice, _seasonFiles, _companyFiles, new CompanyRoster(_armyData));
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(CompanyPath, SeasonPath));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task RollCommand_PassingRoll_SettlesWithoutInjury()
    {
        _dice.RollD20().Returns(11);
        var check = (await OpenAsync("14")).Injuries[0];

        check.RollCommand.Execute(null);

        Assert.Equal("Rolled: 11 — PASS", check.RollResultText);
        Assert.True(check.RollPassed);
        Assert.True(check.IsSettled);
        Assert.False(check.IsInjured);
        Assert.False(check.PassCommand.CanExecute(null));
        Assert.False(_debrief.Units[0].GainedInjury);
    }

    [Fact]
    public async Task RollCommand_FailedRoll_PicksTheInjuryFromTheTable()
    {
        _dice.RollD20().Returns(13);
        var check = (await OpenAsync("14")).Injuries[0];

        check.RollCommand.Execute(null);

        Assert.Equal("Rolled: 13 — FAIL", check.RollResultText);
        Assert.True(check.IsInjured);
        Assert.Equal("Injury Incurred", check.AvoidText);
        Assert.Equal("Legs Injury", check.SelectedInjury?.Label);
        Assert.True(check.IsSettled);
        Assert.Equal((true, "Legs Injury"), (_debrief.Units[0].GainedInjury, _debrief.Units[0].InjuryName));
    }

    [Fact]
    public async Task RollCommand_NoPh_JustShowsTheNumber()
    {
        _dice.RollD20().Returns(4);
        var check = (await OpenAsync("-")).Injuries[0];
        Assert.Equal("Avoid Injury: -", check.AvoidText);

        check.RollCommand.Execute(null);

        Assert.Equal("Rolled: 4", check.RollResultText);
        Assert.False(check.IsInjured);
        Assert.True(check.IsSettled);
    }

    [Fact]
    public async Task PassCommand_AtTheTable_LocksTheVirtualRoll()
    {
        var check = (await OpenAsync("14")).Injuries[0];

        check.PassCommand.Execute(null);

        Assert.True(check.IsPassed);
        Assert.True(check.IsRollLocked);
        Assert.False(check.RollCommand.CanExecute(null));
        Assert.True(check.IsSettled);
    }

    [Fact]
    public async Task FailCommand_AtTheTable_SettlesOnceAnInjuryIsChosen()
    {
        var viewModel = await OpenAsync("14");
        var check = viewModel.Injuries[0];
        Assert.True(viewModel.HasInjuryChecks);
        Assert.False(viewModel.CanContinue);

        check.FailCommand.Execute(null);
        Assert.True(check.IsInjured);
        Assert.False(check.IsSettled);
        Assert.False(viewModel.ContinueCommand.CanExecute(null));

        check.SelectedInjury = InjuryRules.Table[0];

        Assert.True(check.IsSettled);
        Assert.True(viewModel.ContinueCommand.CanExecute(null));
        Assert.Equal("Battle Fury", _debrief.Units[0].InjuryName);
    }

    [Fact]
    public async Task ContinueCommand_EveryCheckSettled_OpensMissionOutcome()
    {
        var viewModel = await OpenAsync("14", "12");
        viewModel.Injuries[0].PassCommand.Execute(null);
        Assert.False(viewModel.ContinueCommand.CanExecute(null));

        viewModel.Injuries[1].PassCommand.Execute(null);
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.Empty(_debrief.PendingInjuries);
        await _navigation.Received(1).PushAsync("MissionOutcomePage", Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task ContinueCommand_InjuriesTaken_SavesThem()
    {
        _dice.RollD20().Returns(13);
        var viewModel = await OpenAsync("14", "14");
        viewModel.Injuries[0].RollCommand.Execute(null);
        viewModel.Injuries[1].PassCommand.Execute(null);

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.Equal([Injury.LegsInjury], _season.Troopers[0].Injuries);
        Assert.False(_season.Troopers.ContainsKey(1));
    }

    [Fact]
    public async Task InjuryOptions_InjuryAlreadyCarried_NotOfferedAndRolledAgain()
    {
        _season.TrooperFor(1).Injuries.Add(Injury.LegsInjury);
        _dice.RollD20().Returns(13, 13, 19);
        var viewModel = await OpenAsync("14", "14");
        var check = viewModel.Injuries[1];

        Assert.DoesNotContain(check.InjuryOptions, entry => entry.Injury == Injury.LegsInjury);
        check.RollCommand.Execute(null);

        Assert.Equal(Injury.ShellShocked, check.SelectedInjury?.Injury);
        Assert.True(check.IsSettled);
    }

    [Fact]
    public async Task FailCommand_EveryInjuryCarried_Settles()
    {
        _season.TrooperFor(0).Injuries.AddRange(Enum.GetValues<Injury>());
        var check = (await OpenAsync("14")).Injuries[0];

        check.FailCommand.Execute(null);

        Assert.Empty(check.InjuryOptions);
        Assert.True(check.IsSettled);
    }

    [Fact]
    public async Task Recoveries_RestingTroopersAndCaptain_ShedAnInjury()
    {
        _season.TrooperFor(7).Injuries.AddRange([Injury.Arms, Injury.BodyInjury]);
        _season.TrooperFor(0).Injuries.Add(Injury.EyesInjury);
        _season.TrooperFor(1).Injuries.Add(Injury.ShellShocked);
        var viewModel = await OpenAsync("14", "14");

        Assert.True(viewModel.HasRecoveries);
        Assert.Equal(["Captain", "Resting"], viewModel.Recoveries.Select(recovery => recovery.Name));
        var resting = viewModel.Recoveries[1];
        Assert.Equal(Injury.Arms, resting.SelectedInjury?.Injury);
        resting.SelectedInjury = resting.Injuries[1];
        viewModel.Injuries[0].PassCommand.Execute(null);
        viewModel.Injuries[1].PassCommand.Execute(null);

        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.Equal([Injury.Arms], _season.Troopers[7].Injuries);
        Assert.Empty(_season.Troopers[0].Injuries);
        Assert.Equal([Injury.ShellShocked], _season.Troopers[1].Injuries);
    }

    [Fact]
    public async Task Recoveries_NotElite_CaptainsHealingIsTheCaptainsOnly()
    {
        _season.TrooperFor(1).Injuries.Add(Injury.ShellShocked);

        var viewModel = await OpenAsync("14", "14");

        Assert.False(viewModel.HasRecoveries);
    }

    [Fact]
    public async Task Recoveries_EliteDeployment_CaptainsHealingCanGoToAnotherDeployedTrooper()
    {
        _elite = true;
        _season.TrooperFor(0).Injuries.Add(Injury.EyesInjury);
        _season.TrooperFor(1).Injuries.Add(Injury.ShellShocked);
        var viewModel = await OpenAsync("14", "14");
        var healing = Assert.Single(viewModel.Recoveries);
        Assert.True(healing.HasRecipientChoice);
        Assert.Equal(["Captain", "Trooper 1"], healing.Recipients.Select(recipient => recipient.Name));

        healing.SelectedRecipient = healing.Recipients[1];
        viewModel.Injuries[0].PassCommand.Execute(null);
        viewModel.Injuries[1].PassCommand.Execute(null);
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.Equal(Injury.ShellShocked, healing.Injuries[0].Injury);
        Assert.Empty(_season.Troopers[1].Injuries);
        Assert.Equal([Injury.EyesInjury], _season.Troopers[0].Injuries);
    }

    [Fact]
    public async Task Recoveries_EliteDeploymentCaptainUninjured_HealsTheOnlyInjuredTrooper()
    {
        _elite = true;
        _season.TrooperFor(1).Injuries.Add(Injury.ShellShocked);

        var healing = Assert.Single((await OpenAsync("14", "14")).Recoveries);

        Assert.False(healing.HasRecipientChoice);
        Assert.Equal("Trooper 1", healing.Name);
    }
}
