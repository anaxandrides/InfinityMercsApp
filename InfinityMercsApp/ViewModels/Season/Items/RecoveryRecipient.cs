using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.ViewModels.Season.Items;

/// <summary>A trooper who can shed an injury, with the injuries it carries from before the contract.</summary>
public sealed record RecoveryRecipient(int EntryIndex, string Name, IReadOnlyList<InjuryTableEntry> Injuries)
{
    public static RecoveryRecipient From(int entryIndex, string name, IReadOnlyList<Injury> injuries) =>
        new(entryIndex, name, injuries.Select(InjuryRules.EntryFor).ToList());

    /// <summary>The name, as a picker lists it.</summary>
    public override string ToString() => Name;
}
