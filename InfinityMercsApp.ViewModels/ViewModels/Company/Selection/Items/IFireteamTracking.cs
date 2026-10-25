namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

/// <summary>What a fireteam's radio button tells: that the player ticked it to track.</summary>
public interface IFireteamTracking
{
    void Ticked(ArmyTeamListItem team);
}
