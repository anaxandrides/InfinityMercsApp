using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InfinityMercsApp.ViewModels.Season.Game.Items;

/// <summary>The command tokens bought with inducements, for the whole game: tapping one spends it, or takes it back.</summary>
public sealed partial class CommandTokenPool : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTokens))]
    [NotifyCanExecuteChangedFor(nameof(UseCommand))]
    public partial int Available { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasTokens))]
    [NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
    public partial int Used { get; set; }

    /// <summary>Whether the company has any command tokens this game, spent or not.</summary>
    public bool HasTokens => Available + Used > 0;

    /// <summary>A new game with <paramref name="count"/> tokens and none spent.</summary>
    public void Reset(int count)
    {
        Available = count;
        Used = 0;
    }

    private bool CanUse() => Available > 0;

    [RelayCommand(CanExecute = nameof(CanUse))]
    private void Use()
    {
        Available--;
        Used++;
    }

    private bool CanRestore() => Used > 0;

    [RelayCommand(CanExecute = nameof(CanRestore))]
    private void Restore()
    {
        Used--;
        Available++;
    }
}
