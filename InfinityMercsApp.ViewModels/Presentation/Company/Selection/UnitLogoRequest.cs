namespace InfinityMercsApp.Presentation.Company.Selection;

/// <summary>A unit whose logo is looked for, and the factions to look under after its own.</summary>
public sealed record UnitLogoRequest(int UnitId, int SourceFactionId, string? CachedLogoPath, string? PackagedLogoPath, IReadOnlyList<int> FactionIds);
