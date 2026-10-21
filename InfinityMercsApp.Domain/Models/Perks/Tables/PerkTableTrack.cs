namespace InfinityMercsApp.Domain.Models.Perks.Tables;

/// <summary>One track of a perk table: its roll label and the perk text in each tier column.</summary>
public sealed record PerkTableTrack(int TrackNumber, string RollLabel, IReadOnlyList<string> TierTexts);
