using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.ViewModels.Season.Game;

/// <summary>A checkbox on the XP sheet.</summary>
public sealed partial class XpCheck(string label, bool isChecked, Action<XpCheck> changed) : ObservableObject
{
    [ObservableProperty]
    private bool _isChecked = isChecked;

    [ObservableProperty]
    private bool _isEnabled = true;

    public string Label { get; } = label;

    partial void OnIsCheckedChanged(bool value) => changed(this);
}
