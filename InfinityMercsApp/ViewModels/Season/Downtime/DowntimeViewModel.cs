using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.Downtime;
using InfinityMercsApp.Domain.Season.Experience;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Season.Downtime;

/// <summary>
/// The downtime event after a mission: roll the event, pick a choice and the merc who performs it,
/// optionally spend CR, roll the test, and record the outcome on the latest season round.
/// </summary>
public partial class DowntimeViewModel(
    INavigationService navigationService,
    ISeasonFileService seasonFiles,
    MissionDebrief debrief,
    IDice dice,
    IDialogService dialogs) : ViewModelBase(navigationService)
{
    public const string EventRollPrompt = "Press ROLL for a random event, or pick a value and press REAL.";

    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;
    private int _crSpent;
    private int _testTarget;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRollDice), nameof(IsResolved), nameof(CanGoBackToBase), nameof(IsSpendingCr))]
    [NotifyCanExecuteChangedFor(
        nameof(RollCommand), nameof(RealCommand), nameof(PickEventCommand), nameof(NoEventCommand),
        nameof(PickChoiceCommand), nameof(PickParticipantCommand), nameof(ConfirmSpendCommand))]
    private DowntimeStage _stage = DowntimeStage.AwaitingEventRoll;

    [ObservableProperty]
    private string _rollPrompt = EventRollPrompt;

    [ObservableProperty]
    private int? _selectedRealRoll;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEvent), nameof(IsShowingEventDetails), nameof(IsChoosingFromEventList), nameof(HasEventTraits), nameof(EventTraitsText))]
    private DowntimeEvent? _currentEvent;

    [ObservableProperty]
    private string _rollHeader = string.Empty;

    [ObservableProperty]
    private bool _isEventListClosed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasChoice))]
    private DowntimeChoiceCard? _selectedChoice;

    [ObservableProperty]
    private string _participantPrompt = string.Empty;

    [ObservableProperty]
    private bool _hasNoCandidates;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasParticipant), nameof(PerformedByText))]
    private ExperienceUnitResult? _participant;

    [ObservableProperty]
    private bool _showsCrSpend;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CrSpendTargetText))]
    private int _crSpendBaseTarget;

    /// <summary>The CR stepper's value; a double because that is what a stepper binds.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CrSpendTargetText))]
    private double _crToSpend;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTestText))]
    private string _testText = string.Empty;

    [ObservableProperty]
    private bool _isTestUnresolvable;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRolledText))]
    private string _rolledText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutcome), nameof(OutcomeText))]
    private PassFailOutcome _outcome = PassFailOutcome.None;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNoConsequences))]
    private IReadOnlyList<string> _consequenceLines = [];

    /// <summary>The values the REAL picker offers: 1 to 20.</summary>
    public IReadOnlyList<int> RealRolls { get; } = Enumerable.Range(1, 20).ToList();

    public IReadOnlyList<DowntimeEventOption> EventOptions { get; } =
        DowntimeEventTable.AllEvents.Select(downtimeEvent => new DowntimeEventOption(downtimeEvent)).ToList();

    public ObservableCollection<DowntimeChoiceCard> ChoiceCards { get; } = [];

    public ObservableCollection<DowntimeParticipantItem> Participants { get; } = [];

    public bool CanRollDice => Stage is DowntimeStage.AwaitingEventRoll or DowntimeStage.AwaitingTestRoll;

    public bool IsResolved => Stage == DowntimeStage.Resolved;

    /// <summary>Back to Base is offered once the event is settled, or when it can't go on, so nobody is left stuck.</summary>
    public bool CanGoBackToBase => Stage is DowntimeStage.Resolved or DowntimeStage.Blocked;

    public bool IsSpendingCr => Stage == DowntimeStage.SpendingCr;

    public bool HasEvent => CurrentEvent is not null;

    /// <summary>A quiet week shows the list of events instead of the event's details.</summary>
    public bool IsShowingEventDetails => CurrentEvent is { IsNoIncident: false };

    public bool IsChoosingFromEventList => CurrentEvent is { IsNoIncident: true };

    public bool HasEventTraits => CurrentEvent is { IsNoIncident: false, EventTraits.Count: > 0 };

    public string EventTraitsText => CurrentEvent is { } downtimeEvent
        ? "Event Traits: " + string.Join(", ", downtimeEvent.EventTraits)
        : string.Empty;

    public bool HasChoice => SelectedChoice is not null;

    public bool HasParticipant => Participant is not null;

    public string PerformedByText => Participant is { } participant ? $"Performed by: {participant.Name}" : string.Empty;

    public string CrSpendTargetText => "Target: " + DowntimeRules.TargetText(CrSpendBaseTarget + (int)CrToSpend);

    public bool HasTestText => TestText.Length > 0;

    public bool HasRolledText => RolledText.Length > 0;

    public bool HasOutcome => Outcome != PassFailOutcome.None;

    public string OutcomeText => $"OUTCOME: {DowntimeRules.OutcomeLabel(Outcome)}";

    public bool HasNoConsequences => HasOutcome && ConsequenceLines.Count == 0;

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    // ── Dice ────────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanRollDice))]
    private Task RollAsync() => DispatchRollAsync(dice.RollD20());

    /// <summary>Uses the value picked from the REAL picker, for a roll made with physical dice.</summary>
    [RelayCommand(CanExecute = nameof(CanRollDice))]
    private async Task RealAsync()
    {
        if (SelectedRealRoll is not { } roll)
        {
            await dialogs.ShowAlertAsync("Pick a number", "Choose a value 1-20 first.");
            return;
        }

        await DispatchRollAsync(roll);
    }

    private Task DispatchRollAsync(int roll) => Stage switch
    {
        DowntimeStage.AwaitingEventRoll => StartEventAsync(roll),
        DowntimeStage.AwaitingTestRoll => ResolveTestRollAsync(roll),
        _ => Task.CompletedTask
    };

    // ── Event ───────────────────────────────────────────────────────────────

    private async Task StartEventAsync(int roll)
    {
        var downtimeEvent = DowntimeEventTable.LookupByRoll(roll);
        ClearResolution();
        ChoiceCards.Clear();
        IsEventListClosed = false;
        CurrentEvent = downtimeEvent;
        RollHeader = $"Rolled {roll}  →  Event {downtimeEvent.RollRange}";

        if (downtimeEvent.IsNoIncident)
        {
            RollPrompt = "Select an event or choose No Event below.";
            Stage = DowntimeStage.ChoosingEvent;
        }
        else
        {
            for (var i = 0; i < downtimeEvent.Choices.Count; i++)
            {
                ChoiceCards.Add(new DowntimeChoiceCard(i + 1, downtimeEvent.Choices[i]));
            }

            RollPrompt = "Pick a choice above to continue.";
            Stage = DowntimeStage.ChoosingChoice;
        }

        await seasonFiles.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.Downtime.EventId = downtimeEvent.RollRange;
            round.Downtime.Result = downtimeEvent.Description;
        });
    }

    private bool IsChoosingEvent() => Stage == DowntimeStage.ChoosingEvent;

    [RelayCommand(CanExecute = nameof(IsChoosingEvent))]
    private Task PickEventAsync(DowntimeEventOption option) => StartEventAsync(option.Event.RepresentativeRoll);

    [RelayCommand(CanExecute = nameof(IsChoosingEvent))]
    private async Task NoEventAsync()
    {
        IsEventListClosed = true;
        RollPrompt = "No event chosen.";
        Stage = DowntimeStage.Resolved;

        await seasonFiles.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.Downtime.ChosenPlan = "No event";
            round.Downtime.Result = "No event chosen";
        });
    }

    // ── Choice and participant ──────────────────────────────────────────────

    private bool IsChoosingChoice() => Stage == DowntimeStage.ChoosingChoice;

    [RelayCommand(CanExecute = nameof(IsChoosingChoice))]
    private async Task PickChoiceAsync(DowntimeChoiceCard card)
    {
        if (CurrentEvent is not { } downtimeEvent)
        {
            return;
        }

        foreach (var other in ChoiceCards.Where(candidate => !ReferenceEquals(candidate, card)).ToList())
        {
            ChoiceCards.Remove(other);
        }

        SelectedChoice = card;
        var kind = DowntimeRules.RequiredParticipant(downtimeEvent, card.Choice);
        ParticipantPrompt = $"Select {DowntimeRules.ParticipantLabel(kind)}:";
        Participants.Clear();
        foreach (var unit in DowntimeRules.Candidates(debrief.Units, kind))
        {
            Participants.Add(DowntimeParticipantItem.For(unit, card.Choice, kind));
        }

        HasNoCandidates = Participants.Count == 0;
        if (HasNoCandidates)
        {
            RollPrompt = "Nobody can take this choice.";
        }

        Stage = HasNoCandidates ? DowntimeStage.Blocked : DowntimeStage.ChoosingParticipant;

        await seasonFiles.UpdateLatestRoundAsync(_seasonFilePath, round =>
            round.Downtime.ChosenPlan = $"Choice {card.Number}: {card.Choice.Description}");
    }

    private bool CanPickParticipant(DowntimeParticipantItem? item) =>
        Stage == DowntimeStage.ChoosingParticipant && item is { IsEligible: true };

    [RelayCommand(CanExecute = nameof(CanPickParticipant))]
    private async Task PickParticipantAsync(DowntimeParticipantItem item)
    {
        if (SelectedChoice is not { } card)
        {
            return;
        }

        foreach (var row in Participants)
        {
            row.IsSelected = ReferenceEquals(row, item);
            row.IsDimmed = !row.IsSelected;
        }

        Participant = item.Unit;

        if (DowntimeRules.HasP2P(card.Choice))
        {
            CrSpendBaseTarget = card.Choice.Test is { } test ? DowntimeRules.TestTarget(test, item.Unit) ?? 0 : 0;
            CrToSpend = 0;
            ShowsCrSpend = true;
            Stage = DowntimeStage.SpendingCr;
            return;
        }

        await BeginTestAsync(0);
    }

    [RelayCommand(CanExecute = nameof(IsSpendingCr))]
    private Task ConfirmSpendAsync() => BeginTestAsync(Math.Max(0, (int)CrToSpend));

    // ── Test and outcome ────────────────────────────────────────────────────

    private async Task BeginTestAsync(int crSpent)
    {
        if (SelectedChoice is not { } card)
        {
            return;
        }

        _crSpent = crSpent;
        if (card.Choice.Test is not { } test)
        {
            await ShowOutcomeAsync(card.Choice, PassFailOutcome.Pass);
            return;
        }

        if (DowntimeRules.TestTarget(test, Participant) is not { } baseTarget)
        {
            TestText = $"Test: {test.Display} — target could not be resolved.";
            IsTestUnresolvable = true;
            RollPrompt = "Unable to resolve test target.";
            Stage = DowntimeStage.Blocked;
            return;
        }

        _testTarget = baseTarget + crSpent;
        TestText = $"Test: {test.Display}  target {DowntimeRules.TargetText(_testTarget)}";
        RollPrompt = $"Roll for {test.Display} — target {_testTarget}.";
        Stage = DowntimeStage.AwaitingTestRoll;
    }

    private Task ResolveTestRollAsync(int roll)
    {
        if (SelectedChoice is not { } card)
        {
            return Task.CompletedTask;
        }

        RolledText = $"Rolled: {roll} vs {_testTarget}";
        return ShowOutcomeAsync(card.Choice, DowntimeRules.Classify(roll, _testTarget));
    }

    private async Task ShowOutcomeAsync(DowntimeChoice choice, PassFailOutcome outcome)
    {
        var participant = Participant;
        var pistol = DowntimeRules.WinsPistol(choice, outcome)
            ? RollPistol(outcome == PassFailOutcome.CritPass ? 1 : 0)
            : null;
        List<SeasonInventoryItem> wonItems = pistol is null
            ? []
            : [new SeasonInventoryItem { Name = pistol.Name, Category = GearSlot.Sidearm.Key(), Source = "Downtime" }];

        Outcome = outcome;
        ConsequenceLines = DowntimeRules.ConsequenceLines(choice, outcome, participant?.Name, pistol);
        if (participant is not null)
        {
            DowntimeRules.ApplyToParticipant(choice, outcome, participant);
        }

        Stage = DowntimeStage.Resolved;

        var totals = DowntimeRules.Totals(choice, outcome, _crSpent);
        var participantName = participant?.Name;
        await seasonFiles.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.Downtime.Result = participantName is null
                ? DowntimeRules.OutcomeLabel(outcome)
                : $"{DowntimeRules.OutcomeLabel(outcome)} — {participantName}";
            round.Downtime.ParticipantName = participantName ?? string.Empty;
            round.Downtime.CrGain = totals.CrGain;
            round.Downtime.NotorietyGain = totals.NotorietyGain;
            round.Downtime.XpGain = totals.XpGain;
            round.Downtime.SpentCr = totals.SpentCr;
            round.Downtime.SwcGain = totals.SwcGain;
            round.Downtime.OtherEffects = string.Join("; ", totals.OtherEffects);
            round.Downtime.WonItems = wonItems;
        });
    }

    private PistolRoll RollPistol(int sdBonus)
    {
        var roll = dice.RollD20();
        return new PistolRoll(SeasonRollTables.Pistols.Lookup(roll) ?? "Unknown pistol", roll, sdBonus);
    }

    [RelayCommand]
    private Task BackToBaseAsync() => NavigationService.PopToRootAsync();

    private void ClearResolution()
    {
        SelectedChoice = null;
        Participants.Clear();
        ParticipantPrompt = string.Empty;
        HasNoCandidates = false;
        Participant = null;
        ShowsCrSpend = false;
        CrSpendBaseTarget = 0;
        CrToSpend = 0;
        TestText = string.Empty;
        IsTestUnresolvable = false;
        RolledText = string.Empty;
        Outcome = PassFailOutcome.None;
        ConsequenceLines = [];
        _crSpent = 0;
        _testTarget = 0;
    }
}
