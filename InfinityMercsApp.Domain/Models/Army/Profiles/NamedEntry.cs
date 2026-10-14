namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>A looked-up entry name with the id it came from (used to find its wiki link).</summary>
public sealed record NamedEntry(int Id, string Name);
