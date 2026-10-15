using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// A trooper shedding one injury after the contract: one that rested, or the captain. The player picks which
/// injury goes; the first is picked to start with.
/// </summary>
public sealed partial class InjuryRecoveryItem : ObservableObject
{
    public InjuryRecoveryItem(int entryIndex, string name, bool isCaptain, IReadOnlyList<Injury> injuries)
    {
        EntryIndex = entryIndex;
        Name = name;
        Reason = isCaptain ? "Captain — recovers even when deployed" : "Rested this contract";
        Injuries = injuries.Select(InjuryRules.EntryFor).ToList();
        _selectedInjury = Injuries.FirstOrDefault();
    }

    public int EntryIndex { get; }

    public string Name { get; }

    public string Reason { get; }

    public IReadOnlyList<InjuryTableEntry> Injuries { get; }

    [ObservableProperty]
    private InjuryTableEntry? _selectedInjury;
}
