namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>A profile and the name it is shown under (e.g. a peripheral's name).</summary>
public sealed record NamedProfile(string Name, UnitProfile Profile);
