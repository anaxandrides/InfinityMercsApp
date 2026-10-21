namespace InfinityMercsApp.Services.CompanySelection.Profiles;

/// <summary>Where a configuration sits in the list, and how many peripherals it brings, for keeping the one with fewest.</summary>
internal sealed record ListedConfiguration(int Index, int PeripheralCount);
