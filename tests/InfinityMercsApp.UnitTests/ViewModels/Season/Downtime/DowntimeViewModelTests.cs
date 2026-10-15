using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Downtime;
using InfinityMercsApp.Domain.Season.Experience;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Season.Downtime;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Downtime;

public sealed class DowntimeViewModelTests
{
    private const string SeasonPath = "/seasons/test.json";

    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly IDice _dice = Substitute.For<IDice>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly MissionDebrief _debrief = new();
    private readonly SeasonRound _round = new();

    public DowntimeViewModelTests()
    {
        // Every update is applied to one round so tests can read what was recorded.
        _seasonFiles
            .UpdateLatestRoundAsync(SeasonPath, Arg.Do<Action<SeasonRound>>(update => update(_round)))
            .Returns(true);

        _debrief.Units =
        [
            new ExperienceUnitResult
            {
                Name = "Alpha", IsLieutenant = true, Renown = 30, Skills = "Hacker",
                UnitCc = "14", UnitBs = "11", UnitPh = "10", UnitWip = "13", UnitArm = "1"
            },
            new ExperienceUnitResult
            {
                Name = "Bravo", IsMvp = true, Renown = 20,
                UnitCc = "12", UnitBs = "12", UnitPh = "11", UnitWip = "12", UnitArm = "2"
            }
        ];
    }

    private DowntimeViewModel Create()
    {
        var viewModel = new DowntimeViewModel(_navigation, _seasonFiles, _debrief, _dice, _dialogs);
        viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParameters.SeasonFilePath] = SeasonPath });
        return viewModel;
    }

    private static async Task EnterRollAsync(DowntimeViewModel viewModel, int roll)
    {
        viewModel.SelectedRealRoll = roll;
        await viewModel.RealCommand.ExecuteAsync(null);
    }

    private static DowntimeParticipantItem ParticipantNamed(DowntimeViewModel viewModel, string name)
    {
        var item = viewModel.Participants.FirstOrDefault(participant => participant.Unit.Name == name);
        Assert.NotNull(item);
        return item;
    }

    [Fact]
    public void StartsWaitingForTheEventRoll()
    {
        var viewModel = Create();

        Assert.Equal(DowntimeStage.AwaitingEventRoll, viewModel.Stage);
        Assert.Equal(DowntimeViewModel.EventRollPrompt, viewModel.RollPrompt);
        Assert.True(viewModel.CanRollDice);
        Assert.False(viewModel.HasEvent);
        Assert.False(viewModel.IsResolved);
        Assert.False(viewModel.CanGoBackToBase);
        Assert.Equal(Enumerable.Range(1, 20), viewModel.RealRolls);
        Assert.Equal(9, viewModel.EventOptions.Count);
        Assert.False(viewModel.NoEventCommand.CanExecute(null));
    }

    [Fact]
    public async Task RealWithoutAPickedValueAsksForOne()
    {
        var viewModel = Create();

        await viewModel.RealCommand.ExecuteAsync(null);

        await _dialogs.Received(1).ShowAlertAsync("Pick a number", "Choose a value 1-20 first.");
        Assert.False(viewModel.HasEvent);
    }

    [Fact]
    public async Task RollingShowsTheEventAndItsChoices()
    {
        _dice.RollD20().Returns(3);
        var viewModel = Create();

        await viewModel.RollCommand.ExecuteAsync(null);

        Assert.Equal("Rolled 3  →  Event 3-4", viewModel.RollHeader);
        Assert.True(viewModel.IsShowingEventDetails);
        Assert.Equal("Event Traits: SWC", viewModel.EventTraitsText);
        Assert.Equal([1, 2, 3], viewModel.ChoiceCards.Select(card => card.Number));
        Assert.Equal("CHOICE 2  (PS=10+ARM)", viewModel.ChoiceCards[1].Header);
        Assert.Equal(DowntimeStage.ChoosingChoice, viewModel.Stage);
        Assert.False(viewModel.CanRollDice);
        Assert.False(viewModel.RollCommand.CanExecute(null));
        Assert.Equal("Pick a choice above to continue.", viewModel.RollPrompt);
        Assert.Equal("3-4", _round.Downtime.EventId);
    }

    [Fact]
    public async Task AQuietWeekLetsAnEventBePicked()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 19);

        Assert.True(viewModel.IsChoosingFromEventList);
        Assert.False(viewModel.IsShowingEventDetails);
        Assert.Equal(DowntimeStage.ChoosingEvent, viewModel.Stage);

        await viewModel.PickEventCommand.ExecuteAsync(viewModel.EventOptions[2]);

        Assert.Equal("Rolled 5  →  Event 5-6", viewModel.RollHeader);
        Assert.False(viewModel.IsChoosingFromEventList);
        Assert.Equal(3, viewModel.ChoiceCards.Count);
        Assert.Equal("5-6", _round.Downtime.EventId);
    }

    [Fact]
    public async Task NoEventSettlesAQuietWeek()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 20);

        await viewModel.NoEventCommand.ExecuteAsync(null);

        Assert.True(viewModel.IsEventListClosed);
        Assert.True(viewModel.IsResolved);
        Assert.False(viewModel.PickEventCommand.CanExecute(viewModel.EventOptions[0]));
        Assert.Equal(("No event", "No event chosen"), (_round.Downtime.ChosenPlan, _round.Downtime.Result));
    }

    [Fact]
    public async Task PickingAChoiceKeepsOnlyThatCardAndListsTheMercs()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 1);

        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[1]);

        var card = Assert.Single(viewModel.ChoiceCards);
        Assert.Equal(2, card.Number);
        Assert.Equal("Select a Merc:", viewModel.ParticipantPrompt);
        Assert.Equal(["Alpha", "Bravo"], viewModel.Participants.Select(p => p.DisplayName));
        Assert.Equal(["Target 10", "Target 11"], viewModel.Participants.Select(p => p.TargetText));
        Assert.Equal(DowntimeStage.ChoosingParticipant, viewModel.Stage);
        Assert.Equal("Choice 2: ...you floor it and drive away", _round.Downtime.ChosenPlan);
    }

    [Fact]
    public async Task APassedTestRecordsTheOutcomeAndRewardsTheParticipant()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 1);
        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[1]);
        var alpha = ParticipantNamed(viewModel, "Alpha");

        await viewModel.PickParticipantCommand.ExecuteAsync(alpha);

        Assert.True(alpha.IsSelected);
        Assert.True(ParticipantNamed(viewModel, "Bravo").IsDimmed);
        Assert.Equal("Performed by: Alpha", viewModel.PerformedByText);
        Assert.Equal("Test: PH  target 10", viewModel.TestText);
        Assert.Equal(DowntimeStage.AwaitingTestRoll, viewModel.Stage);
        Assert.True(viewModel.CanRollDice);

        await EnterRollAsync(viewModel, 4);

        Assert.Equal("Rolled: 4 vs 10", viewModel.RolledText);
        Assert.Equal("OUTCOME: PASS", viewModel.OutcomeText);
        Assert.Equal(["Gain 1 Notoriety", "Alpha gains 2 XP"], viewModel.ConsequenceLines);
        Assert.False(viewModel.HasNoConsequences);
        Assert.True(viewModel.IsResolved);
        Assert.True(viewModel.CanGoBackToBase);
        Assert.Equal(2, _debrief.Units[0].XpData.DowntimeBonusXp);
        Assert.Equal("PASS — Alpha", _round.Downtime.Result);
        Assert.Equal("Alpha", _round.Downtime.ParticipantName);
        Assert.Equal((1, 2), (_round.Downtime.NotorietyGain, _round.Downtime.XpGain));
    }

    [Fact]
    public async Task AP2PChoiceLetsCrRaiseTheTarget()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 1);
        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[2]);
        await viewModel.PickParticipantCommand.ExecuteAsync(ParticipantNamed(viewModel, "Alpha"));

        Assert.True(viewModel.ShowsCrSpend);
        Assert.True(viewModel.IsSpendingCr);
        Assert.False(viewModel.CanRollDice);
        Assert.Equal("Target: 13", viewModel.CrSpendTargetText);

        viewModel.CrToSpend = 9;
        Assert.Equal("Target: 22  (crit on 20 and 1–2)", viewModel.CrSpendTargetText);

        await viewModel.ConfirmSpendCommand.ExecuteAsync(null);
        Assert.False(viewModel.ConfirmSpendCommand.CanExecute(null));
        Assert.Equal("Test: WIP  target 22  (crit on 20 and 1–2)", viewModel.TestText);
        Assert.Equal("Roll for WIP — target 22.", viewModel.RollPrompt);

        await EnterRollAsync(viewModel, 2);

        Assert.Equal(PassFailOutcome.CritPass, viewModel.Outcome);
        Assert.Equal(0, _round.Downtime.SpentCr);
        Assert.Equal(-2, _round.Downtime.NotorietyGain);
    }

    [Fact]
    public async Task CrSpentOnAFailedP2PTestIsRecorded()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 1);
        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[2]);
        await viewModel.PickParticipantCommand.ExecuteAsync(ParticipantNamed(viewModel, "Alpha"));
        viewModel.CrToSpend = 3;
        await viewModel.ConfirmSpendCommand.ExecuteAsync(null);

        await EnterRollAsync(viewModel, 18);

        Assert.Equal(PassFailOutcome.Fail, viewModel.Outcome);
        Assert.Equal(3, _round.Downtime.SpentCr);
    }

    [Fact]
    public async Task AMercMissingARequirementCannotBePicked()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 15);
        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[1]);

        var bravo = ParticipantNamed(viewModel, "Bravo");
        Assert.False(bravo.IsEligible);
        Assert.True(bravo.IsDimmed);
        Assert.Equal("Missing: Hacker", bravo.MissingText);
        Assert.False(bravo.ShowTarget);
        Assert.False(viewModel.PickParticipantCommand.CanExecute(bravo));
        Assert.True(viewModel.PickParticipantCommand.CanExecute(ParticipantNamed(viewModel, "Alpha")));
    }

    [Fact]
    public async Task ACaptainChoiceListsOnlyTheCaptain()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 11);

        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[2]);

        Assert.Equal("Select the Captain:", viewModel.ParticipantPrompt);
        Assert.Equal(["Alpha"], viewModel.Participants.Select(p => p.DisplayName));
    }

    [Fact]
    public async Task TheMostRenownedMercShowsTheirRenown()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 13);

        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[0]);

        Assert.Equal(["Alpha  (30 renown)"], viewModel.Participants.Select(p => p.DisplayName));
    }

    [Fact]
    public async Task NobodyEligibleBlocksTheFlow()
    {
        _debrief.Units = [];
        var viewModel = Create();
        await EnterRollAsync(viewModel, 1);

        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[0]);

        Assert.True(viewModel.HasNoCandidates);
        Assert.Equal(DowntimeStage.Blocked, viewModel.Stage);
        Assert.Equal("Nobody can take this choice.", viewModel.RollPrompt);
        Assert.False(viewModel.CanRollDice);
        Assert.True(viewModel.CanGoBackToBase);
    }

    [Fact]
    public async Task ATestWithoutATargetBlocksTheFlow()
    {
        _debrief.Units[0].UnitPh = "-";
        var viewModel = Create();
        await EnterRollAsync(viewModel, 1);
        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[1]);

        await viewModel.PickParticipantCommand.ExecuteAsync(ParticipantNamed(viewModel, "Alpha"));

        Assert.True(viewModel.IsTestUnresolvable);
        Assert.Equal("Test: PH — target could not be resolved.", viewModel.TestText);
        Assert.Equal("Unable to resolve test target.", viewModel.RollPrompt);
        Assert.Equal(DowntimeStage.Blocked, viewModel.Stage);
        Assert.True(viewModel.CanGoBackToBase);
    }

    [Fact]
    public async Task AWonPistolIsRolledAndRecorded()
    {
        _dice.RollD20().Returns(1);
        var viewModel = Create();
        await EnterRollAsync(viewModel, 1);
        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[0]);
        await viewModel.PickParticipantCommand.ExecuteAsync(ParticipantNamed(viewModel, "Alpha"));

        await EnterRollAsync(viewModel, 2);

        Assert.Equal(PassFailOutcome.Pass, viewModel.Outcome);
        var item = Assert.Single(_round.Downtime.WonItems);
        Assert.Equal((GearSlot.Sidearm.Key(), "Downtime"), (item.Category, item.Source));
        Assert.Contains(viewModel.ConsequenceLines, line => line.StartsWith("Roll a random pistol: ") && line.EndsWith("(rolled 1)"));
    }

    [Fact]
    public async Task AFailedAttackInjuresTheParticipant()
    {
        var viewModel = Create();
        await EnterRollAsync(viewModel, 1);
        await viewModel.PickChoiceCommand.ExecuteAsync(viewModel.ChoiceCards[0]);
        await viewModel.PickParticipantCommand.ExecuteAsync(ParticipantNamed(viewModel, "Alpha"));

        await EnterRollAsync(viewModel, 15);

        Assert.Equal("OUTCOME: FAIL", viewModel.OutcomeText);
        Assert.True(_debrief.Units[0].GainedInjury);
        Assert.Empty(_round.Downtime.WonItems);
    }

    [Fact]
    public async Task BackToBaseReturnsToTheRoot()
    {
        await Create().BackToBaseCommand.ExecuteAsync(null);

        await _navigation.Received(1).PopToRootAsync();
    }
}
