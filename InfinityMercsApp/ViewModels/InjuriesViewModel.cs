using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>
/// One trooper's injury check. It is settled by a virtual d20 roll, by PASS, or by FAIL followed by
/// choosing the injury; a failed virtual roll picks the injury from the table.
/// </summary>
public partial class InjuryCheckItem : ObservableObject
{
    private readonly IDice _dice;
    private readonly Action<InjuryCheckItem> _onSettled;

    public InjuryCheckItem(PendingInjury injury, IDice dice, Action<InjuryCheckItem> onSettled)
    {
        Injury = injury;
        _dice = dice;
        _onSettled = onSettled;
        AvoidTarget = InjuryRules.AvoidTarget(injury.PhValue);
        LogoSource = UnitLogo.Source(injury.CachedLogoPath, injury.PackagedLogoPath);
    }

    public PendingInjury Injury { get; }

    public string Name => Injury.Name;

    public string? LogoSource { get; }

    public int? AvoidTarget { get; }

    public IReadOnlyList<InjuryTableEntry> InjuryOptions => InjuryRules.Table;

    public string AvoidText => IsInjured ? "Injury Incurred" : $"Avoid Injury: {AvoidTarget?.ToString() ?? "-"}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvoidText), nameof(IsRollLocked), nameof(CanChooseOutcome))]
    [NotifyCanExecuteChangedFor(nameof(RollCommand), nameof(PassCommand), nameof(FailCommand))]
    private bool _isInjured;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRollLocked), nameof(CanChooseOutcome))]
    [NotifyCanExecuteChangedFor(nameof(RollCommand), nameof(PassCommand), nameof(FailCommand))]
    private bool _isPassed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRolled), nameof(CanChooseOutcome))]
    [NotifyCanExecuteChangedFor(nameof(RollCommand), nameof(PassCommand), nameof(FailCommand))]
    private string? _rollResultText;

    [ObservableProperty]
    private bool _rollPassed;

    [ObservableProperty]
    private bool _isSettled;

    [ObservableProperty]
    private InjuryTableEntry? _selectedInjury;

    public bool HasRolled => RollResultText is not null;

    /// <summary>The virtual roll is greyed out once the outcome was decided at the table.</summary>
    public bool IsRollLocked => IsPassed || IsInjured;

    private bool CanChooseOutcome => !HasRolled && !IsPassed && !IsInjured;

    private bool CanRoll() => CanChooseOutcome;

    [RelayCommand(CanExecute = nameof(CanRoll))]
    private void Roll()
    {
        var roll = _dice.RollD20();
        var passed = AvoidTarget is { } target && roll <= target;
        RollPassed = passed;
        RollResultText = AvoidTarget is null ? $"Rolled: {roll}" : $"Rolled: {roll} — {(passed ? "PASS" : "FAIL")}";

        if (!passed && AvoidTarget is not null)
        {
            IsInjured = true;
            var index = InjuryRules.TableIndexForRoll(roll);
            if (index >= 0)
            {
                SelectedInjury = InjuryRules.Table[index];
            }
        }

        Settle();
    }

    private bool CanPassOrFail() => CanChooseOutcome;

    [RelayCommand(CanExecute = nameof(CanPassOrFail))]
    private void Pass()
    {
        IsPassed = true;
        Settle();
    }

    /// <summary>Settled once the injury is chosen.</summary>
    [RelayCommand(CanExecute = nameof(CanPassOrFail))]
    private void Fail() => IsInjured = true;

    partial void OnSelectedInjuryChanged(InjuryTableEntry? value)
    {
        if (value is not null)
        {
            Settle();
        }
    }

    private void Settle()
    {
        IsSettled = true;
        _onSettled(this);
    }
}

/// <summary>Injury checks for every trooper taken out; continuing needs all of them settled.</summary>
public partial class InjuriesViewModel(
    INavigationService navigationService,
    MissionDebrief debrief,
    IDice dice) : ViewModelBase(navigationService)
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;

    public ObservableCollection<InjuryCheckItem> Injuries { get; } = [];

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    public override Task InitializeAsync()
    {
        if (!IsInitialized)
        {
            foreach (var injury in debrief.PendingInjuries)
            {
                Injuries.Add(new InjuryCheckItem(injury, dice, OnSettled));
            }

            ContinueCommand.NotifyCanExecuteChanged();
        }

        return Task.CompletedTask;
    }

    // An injured trooper's experience card shows the injury and its XP.
    private void OnSettled(InjuryCheckItem item)
    {
        if (item.IsInjured && item.SelectedInjury is { } injury
            && debrief.Units.FirstOrDefault(u => u.EntryIndex == item.Injury.EntryIndex) is { } unit)
        {
            unit.GainedInjury = true;
            unit.InjuryName = injury.Label;
        }

        ContinueCommand.NotifyCanExecuteChanged();
    }

    private bool CanContinue() => Injuries.All(i => i.IsSettled);

    [RelayCommand(CanExecute = nameof(CanContinue))]
    private Task ContinueAsync()
    {
        debrief.PendingInjuries.Clear();
        return NavigationService.PushAsync(
            "MissionOutcomePage",
            NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath));
    }
}
