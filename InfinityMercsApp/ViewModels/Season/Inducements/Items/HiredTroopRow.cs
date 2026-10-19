namespace InfinityMercsApp.ViewModels.Season.Inducements.Items;

/// <summary>A troop hired for the contract, which can be let go again.</summary>
/// <param name="Name">The loadout hired, e.g. "Diggers, Armed Prospectors: Rifle, Light Shotgun".</param>
public sealed record HiredTroopRow(int EntryIndex, string Name, int Cost);
