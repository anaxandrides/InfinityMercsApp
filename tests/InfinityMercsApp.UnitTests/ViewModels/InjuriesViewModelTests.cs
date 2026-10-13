using InfinityMercsApp.Domain.Season;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class InjuriesViewModelTests
{
    private readonly FakeNavigationService _navigation = new();
    private readonly MissionDebrief _debrief = new();
    private readonly IDice _dice = Substitute.For<IDice>();

    // PH 14 gives an avoid target of 11.
    private async Task<InjuriesViewModel> OpenAsync(params string[] phValues)
    {
        _debrief.PendingInjuries = phValues
            .Select((ph, i) => new PendingInjury { EntryIndex = i, Name = $"Trooper {i}", PhValue = ph })
            .ToList();
        _debrief.Units = phValues.Select((_, i) => new ExperienceUnitResult { EntryIndex = i }).ToList();

        var viewModel = new InjuriesViewModel(_navigation, _debrief, _dice);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task APassingRollSettlesWithoutInjury()
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
    public async Task AFailedRollPicksTheInjuryFromTheTable()
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
    public async Task WithoutPhARollJustShowsTheNumber()
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
    public async Task PassAtTheTableLocksTheVirtualRoll()
    {
        var check = (await OpenAsync("14")).Injuries[0];

        check.PassCommand.Execute(null);

        Assert.True(check.IsPassed);
        Assert.True(check.IsRollLocked);
        Assert.False(check.RollCommand.CanExecute(null));
        Assert.True(check.IsSettled);
    }

    [Fact]
    public async Task FailAtTheTableSettlesOnceAnInjuryIsChosen()
    {
        var viewModel = await OpenAsync("14");
        var check = viewModel.Injuries[0];

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
    public async Task ContinuingNeedsEveryCheckSettledThenOpensMissionOutcome()
    {
        var viewModel = await OpenAsync("14", "12");
        viewModel.Injuries[0].PassCommand.Execute(null);
        Assert.False(viewModel.ContinueCommand.CanExecute(null));

        viewModel.Injuries[1].PassCommand.Execute(null);
        await viewModel.ContinueCommand.ExecuteAsync(null);

        Assert.Empty(_debrief.PendingInjuries);
        Assert.Equal("MissionOutcomePage", Assert.Single(_navigation.Navigations).Route);
    }
}
