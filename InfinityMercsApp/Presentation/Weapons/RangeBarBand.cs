namespace InfinityMercsApp.Presentation.Weapons;

/// <summary>A band of a weapon's range bar: where it sits as fractions of the bar, and its modifier.</summary>
public sealed record RangeBarBand(double StartFraction, double EndFraction, string Modifier, RangeBandTone Tone);
