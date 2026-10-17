namespace InfinityMercsApp.Domain.Models.Season.Gear;

/// <summary>One copy of an item in a trooper's slot.</summary>
public sealed record GearHolding(int EntryIndex, GearSlot Slot);
