namespace InfinityMercsApp.Domain.Season.Retirement;

/// <summary>A trooper leaving the company: who it is, the renown it keeps, and the peripherals that leave with it.</summary>
public sealed record RetiringTrooper(int EntryIndex, string Name, int Renown, IReadOnlyList<RetiringPeripheral> Peripherals);
