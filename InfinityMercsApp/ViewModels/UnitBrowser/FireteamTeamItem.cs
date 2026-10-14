namespace InfinityMercsApp.ViewModels.UnitBrowser;

public class FireteamTeamItem
{
    public string Name { get; init; } = string.Empty;
    public string TeamTypes { get; init; } = "-";
    public IReadOnlyList<FireteamUnitLimitItem> UnitLimits { get; init; } = [];
}
