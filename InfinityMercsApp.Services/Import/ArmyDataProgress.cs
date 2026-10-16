namespace InfinityMercsApp.Services.Import;

/// <summary>How far an army data check or update has got: a line to show, and the fraction done from 0 to 1.</summary>
public sealed record ArmyDataProgress(string Message, double Fraction);
