namespace InfinityMercsApp.Services.CompanySelection.Units;

/// <summary>The units of every source faction, one per name, and their fireteams merged across the factions.</summary>
public sealed record MergedUnits(IReadOnlyList<SelectionUnitSource> Units, IReadOnlyList<MergedFireteam> Teams);
