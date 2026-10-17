using InfinityMercsApp.ViewModels.Season.Overview.Items;

namespace InfinityMercsApp.ViewModels.Season.Overview;

/// <summary>The changes the Team tab makes to the company file, which the season page owns.</summary>
public interface ITeamChanges
{
    /// <summary>Rolls what the trooper still owes on the Booty or Metachemistry table.</summary>
    Task RollOwedAsync(int entryIndex);

    /// <summary>
    /// Detaches the peripherals of troopers losing the Peripheral (Controller) augment, then has the trooper gaining
    /// it pick one: an owned peripheral no one controls, or a new one bought at its cost.
    /// </summary>
    /// <returns>False when the pick is cancelled or can't be paid for; nothing is changed then.</returns>
    Task<bool> ChangePeripheralsAsync(PeripheralChange change);

    /// <summary>Sells a peripheral no trooper controls, once confirmed, for half its cost.</summary>
    Task SellPeripheralAsync(int entryIndex);
}
