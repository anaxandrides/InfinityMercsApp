using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace InfinityMercsApp.Api.ArmyData;

/// <summary>Checks Corvus Belli for changed army data every six hours. The deploy workflow also runs it once.</summary>
public sealed class RefreshArmyDataFunction(ArmyMirror mirror, ILogger<RefreshArmyDataFunction> logger)
{
    [Function("RefreshArmyData")]
    public async Task RunAsync([TimerTrigger("0 0 */6 * * *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        var refresh = await mirror.RefreshAsync(cancellationToken);
        logger.LogInformation(
            "Army data checked: snapshot {SnapshotId} ({SnapshotState}), metadata {MetadataState}, {Updated} armies updated ({UpdatedIds}), {Failed} failed ({FailedIds}).",
            refresh.SnapshotId,
            refresh.NewSnapshot ? "new" : "unchanged",
            refresh.MetadataUpdated ? "updated" : "unchanged",
            refresh.UpdatedFactionIds.Count,
            string.Join(", ", refresh.UpdatedFactionIds),
            refresh.FailedFactionIds.Count,
            string.Join(", ", refresh.FailedFactionIds));
    }
}
