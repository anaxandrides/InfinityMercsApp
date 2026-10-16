using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.ViewModels.Season.Game.Items;

/// <summary>One kind of order token: how many are left this round and how many are spent. Tapping moves one across.</summary>
public sealed partial class OrderPoolItem(OrderKind kind) : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UseCommand))]
    private int _available;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RestoreCommand))]
    private int _used;

    public OrderKind Kind { get; } = kind;

    public string IconPath => Kind switch
    {
        OrderKind.Lieutenant => "SVGCache/CBIcons/lieutenant.svg",
        OrderKind.Impetuous => "SVGCache/CBIcons/impetuous.svg",
        OrderKind.Irregular => "SVGCache/CBIcons/irregular.svg",
        _ => "SVGCache/CBIcons/regular.svg"
    };

    /// <summary>A fresh round: <paramref name="count"/> available and none spent.</summary>
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
