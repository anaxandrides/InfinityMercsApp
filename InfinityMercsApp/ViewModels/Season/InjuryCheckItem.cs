using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// One trooper's injury check. It is settled by a virtual d20 roll, by PASS, or by FAIL followed by
/// choosing the injury; a failed virtual roll picks the injury from the table. Injuries are unique, so one
/// the trooper already carries is rolled again and is not offered.
/// </summary>
public partial class InjuryCheckItem : ObservableObject
{
    private readonly IDice _dice;
    private readonly Action<InjuryCheckItem> _onSettled;
    private readonly IReadOnlyCollection<Injury> _carried;

    public InjuryCheckItem(PendingInjury injury, IReadOnlyCollection<Injury> carried, IDice dice, Action<InjuryCheckItem> onSettled)
    {
        Injury = injury;
        _carried = carried;
        _dice = dice;
        _onSettled = onSettled;
        InjuryOptions = InjuryRules.Table.Where(entry => !carried.Contains(entry.Injury)).ToList();
        AvoidTarget = InjuryRules.AvoidTarget(injury.PhValue);
        LogoSource = UnitLogo.Source(injury.CachedLogoPath, injury.PackagedLogoPath);
    }

    public PendingInjury Injury { get; }

    public string Name => Injury.Name;

    public string? LogoSource { get; }

    public int? AvoidTarget { get; }

    /// <summary>The injuries the trooper can still take.</summary>
    public IReadOnlyList<InjuryTableEntry> InjuryOptions { get; }

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
            var rolled = index >= 0 && !_carried.Contains(InjuryRules.Table[index].Injury)
                ? InjuryRules.Table[index].Injury
                : InjuryRules.RollNew(_dice.RollD20, _carried);
            if (rolled is { } injury)
            {
                SelectedInjury = InjuryRules.EntryFor(injury);
                return;
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

    /// <summary>Settled once the injury is chosen, or straight away for a trooper that already carries every injury.</summary>
    [RelayCommand(CanExecute = nameof(CanPassOrFail))]
    private void Fail()
    {
        IsInjured = true;
        if (InjuryOptions.Count == 0)
        {
            Settle();
        }
    }

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
