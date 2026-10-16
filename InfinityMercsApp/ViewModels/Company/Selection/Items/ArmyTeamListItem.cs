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

    [ObservableProperty]
    private bool _isTrackedTeam;

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isExpanded;
}
