namespace InfinityMercsApp.Web.Platform;

/// <summary>The component waiting for the army data, told once it is loaded.</summary>
public interface IArmyDataWatcher
{
    void ArmyDataLoaded();
}
