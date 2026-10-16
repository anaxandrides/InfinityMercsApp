using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.ViewModels.Components.UnitFilter;

/// <summary>A value chip of the open criterion, e.g. Mimetism under Skills.</summary>
public sealed partial class SelectableFilterOption(string value) : ObservableObject
{
    public string Value { get; } = value;

    [ObservableProperty]
    private bool _isSelected;
}
