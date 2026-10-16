using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.ViewModels.Season.Items;

namespace InfinityMercsApp.ViewModels.Season.Bye.Items;

/// <summary>A trooper on the bye round page, ticked when it gains the bye's XP.</summary>
public sealed partial class ByeTrooperItem(DeploymentUnitItem unit) : ObservableObject
{
    public DeploymentUnitItem Unit { get; } = unit;

    public int EntryIndex => Unit.EntryIndex;

    public string Name => Unit.Name;

    public string? CachedLogoPath => Unit.CachedLogoPath;

    public string? PackagedLogoPath => Unit.PackagedLogoPath;

    [ObservableProperty]
    private bool _isSelected;
}
