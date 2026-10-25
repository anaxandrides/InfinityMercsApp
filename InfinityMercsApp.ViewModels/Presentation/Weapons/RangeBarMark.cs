namespace InfinityMercsApp.Presentation.Weapons;

/// <summary>A distance under a band edge of a weapon's range bar, e.g. 8" at 0.25 of the way along.</summary>
public sealed record RangeBarMark(double Fraction, string Label);
