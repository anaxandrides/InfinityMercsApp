using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

public partial class ArmyTeamListItem : ObservableObject
{
    public string Name { get; init; } = string.Empty;
    public string TeamCountsText { get; init; } = string.Empty;
    public bool HasTeamCounts => !string.IsNullOrWhiteSpace(TeamCountsText);
    public bool IsWildcardBucket { get; init; }
    public ObservableCollection<ArmyTeamUnitLimitItem> AllowedProfiles { get; init; } = [];

    public bool ShowTrackingRadioButton => !IsWildcardBucket;

    /// <summary>Told when the player ticks this fireteam's radio button; null when no fireteam is tracked.</summary>
    public IFireteamTracking? Tracking { get; init; }

    [ObservableProperty]
    public partial bool IsTrackedTeam { get; set; }

    partial void OnIsTrackedTeamChanged(bool value)
    {
        if (value)
        {
            Tracking?.Ticked(this);
        }
    }

    [ObservableProperty]
    public partial bool IsVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }
}
