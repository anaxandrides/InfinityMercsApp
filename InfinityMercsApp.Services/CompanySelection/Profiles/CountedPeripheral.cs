namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>A peripheral listed with how many come, e.g. "Traktor Mul (2)".</summary>
public sealed record CountedPeripheral(string Name, int Count);
