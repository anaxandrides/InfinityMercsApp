using InfinityMercsApp.Domain.Models.Metadata;

namespace InfinityMercsApp.Presentation.Armory;

/// <summary>A weapon and all of its firing modes.</summary>
public sealed record WeaponGroup(string Name, IReadOnlyList<Weapon> Modes);
