using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.ViewModels.Season.Game.Items;

/// <summary>A checkbox on the XP sheet.</summary>
public sealed partial class XpCheck(string label, bool isChecked, Action<XpCheck> changed) : ObservableObject
{
    [ObservableProperty]
    public partial bool IsChecked { get; set; } = isChecked;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    public string Label { get; } = label;

    partial void OnIsCheckedChanged(bool value) => changed(this);
}
