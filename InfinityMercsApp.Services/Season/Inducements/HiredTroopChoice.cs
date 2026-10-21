namespace InfinityMercsApp.Services.Season.Inducements;

/// <summary>An army loadout a Troops for Hire row can be hired in.</summary>
/// <param name="Name">The trooper and its weapons, e.g. "Diggers, Armed Prospectors: Rifle, Light Shotgun".</param>
public sealed record HiredTroopChoice(int FactionId, int UnitId, int GroupId, int OptionId, string Name);
