namespace InfinityMercsApp.Domain.Models.Filtering;

public sealed record UnitFilterTerm(
    UnitFilterField Field,
    IReadOnlyList<string> Values,
    UnitFilterMatchMode MatchMode = UnitFilterMatchMode.Any);
