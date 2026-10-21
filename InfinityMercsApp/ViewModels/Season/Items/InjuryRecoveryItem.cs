using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Season.Injuries;

namespace InfinityMercsApp.ViewModels.Season.Items;

/// <summary>
/// One injury shed after the contract: by a trooper that rested, or through the Captain's healing, which an elite
/// deployment lets the player give to another deployed trooper. The player picks which injury goes; the first is
/// picked to start with.
/// </summary>
public sealed partial class InjuryRecoveryItem : ObservableObject
{
    private InjuryRecoveryItem(IReadOnlyList<RecoveryRecipient> recipients, string reason)
    {
        Recipients = recipients;
        Reason = reason;
        SelectedRecipient = recipients[0];
    }

    /// <summary>A trooper that sat the contract out, shedding one of its injuries.</summary>
    public static InjuryRecoveryItem Rested(RecoveryRecipient trooper) => new([trooper], "Rested this contract");

    /// <summary>The Captain's healing, for the Captain or, when elite deployed, another of <paramref name="recipients"/>.</summary>
    public static InjuryRecoveryItem CaptainsHealing(IReadOnlyList<RecoveryRecipient> recipients, bool isEliteDeployment) =>
        new(recipients, isEliteDeployment
            ? "Captain's healing — elite deployment lets it go to any deployed trooper"
            : "Captain — recovers even when deployed");

    public IReadOnlyList<RecoveryRecipient> Recipients { get; }

    public bool HasRecipientChoice => Recipients.Count > 1;

    public string Reason { get; }

    /// <summary>Who sheds the injury.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EntryIndex), nameof(Name), nameof(Injuries))]
    public partial RecoveryRecipient SelectedRecipient { get; set; }

    public int EntryIndex => SelectedRecipient.EntryIndex;

    public string Name => SelectedRecipient.Name;

    public IReadOnlyList<InjuryTableEntry> Injuries => SelectedRecipient.Injuries;

    [ObservableProperty]
    public partial InjuryTableEntry? SelectedInjury { get; set; }

    partial void OnSelectedRecipientChanged(RecoveryRecipient value) => SelectedInjury = value.Injuries.FirstOrDefault();
}
