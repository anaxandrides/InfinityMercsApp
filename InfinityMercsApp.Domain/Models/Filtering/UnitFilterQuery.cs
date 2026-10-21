namespace InfinityMercsApp.Domain.Models.Filtering;

public sealed class UnitFilterQuery
{
    public IReadOnlyList<UnitFilterTerm> Terms { get; init; } = [];
    public int? MinPoints { get; init; }
    public int? MaxPoints { get; init; }
    public bool LieutenantOnlyUnits { get; init; }
    public bool TeamsView { get; init; }

    public static UnitFilterQuery None { get; } = new();

    public static UnitFilterQuery FromCriteria(UnitFilterCriteria? criteria)
    {
        if (criteria is null)
        {
            return None;
        }

        var normalizedTerms = criteria.Terms
            .Where(term => term.Values.Count > 0)
            .Select(term => new UnitFilterTerm(
                term.Field,
                term.Values
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Select(value => value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                term.MatchMode))
            .Where(term => term.Values.Count > 0)
            .OrderBy(term => term.Field)
            .ToList();

        return new UnitFilterQuery
        {
            Terms = normalizedTerms,
            MinPoints = criteria.MinPoints,
            MaxPoints = criteria.MaxPoints,
            LieutenantOnlyUnits = criteria.LieutenantOnlyUnits,
            TeamsView = criteria.TeamsView
        };
    }

    public UnitFilterTerm? GetTerm(UnitFilterField field)
    {
        return Terms.FirstOrDefault(x => x.Field == field);
    }
}
