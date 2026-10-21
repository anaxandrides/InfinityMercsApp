namespace InfinityMercsApp.Domain.Models.Metadata.Ranges;

/// <summary>A range band laid out on the range bar: where it starts and ends (in cm) and its modifier.</summary>
public sealed record RangeBand(string Label, int StartCm, int EndCm, string Mod);
