using InfinityMercsApp.Domain.Models.Metadata;

namespace InfinityMercsApp.Domain.Season.Loot;

/// <summary>One mode of a loot weapon, as the army data would describe it, and its ammunition.</summary>
public sealed record LootWeaponMode(Weapon Weapon, string Ammo);
