// FROZEN LEGACY ORACLE — verbatim copy of production JSON rules from main@224e1e2.
// Do not edit or "fix" this file. Characterization tests run it against real Infinity Army
// payloads so replacement code can be proven to produce identical results.
// Source: Views/Controls/UnitFilterPopupView.xaml.cs (filter criteria/query types only)
namespace InfinityMercsApp.UnitTests.Legacy;

internal sealed class UnitFilterCriteria
{
    public IReadOnlyList<UnitFilterTerm> Terms { get; init; } = [];
    public int? MinPoints { get; init; }
    public int? MaxPoints { get; init; }
    public bool LieutenantOnlyUnits { get; init; }
    public bool TeamsView { get; init; }

    public static UnitFilterCriteria None { get; } = new();

    public bool IsActive => Terms.Count > 0
        || (MinPoints.HasValue && MinPoints.Value != 0)
        || (MaxPoints.HasValue && MaxPoints.Value != 200)
        || LieutenantOnlyUnits;

    public UnitFilterQuery ToQuery()
    {
        return UnitFilterQuery.FromCriteria(this);
    }
}

internal enum UnitFilterField
{
    Classification,
    Characteristics,
    Skills,
    Equipment,
    Weapons,
    Ammo
}

internal enum UnitFilterMatchMode
{
    Any,
    All
}

internal sealed record UnitFilterTerm(
    UnitFilterField Field,
    IReadOnlyList<string> Values,
    UnitFilterMatchMode MatchMode = UnitFilterMatchMode.Any);

internal sealed class UnitFilterQuery
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

