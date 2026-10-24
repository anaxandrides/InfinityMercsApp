namespace InfinityMercsApp.ViewModels.Season.Items;

/// <summary>What a trooper's deploy tick box tells, so the page can apply the deployment rules.</summary>
public interface IDeploymentChoice
{
    void CheckedChanged(DeploymentUnitItem unit);
}
