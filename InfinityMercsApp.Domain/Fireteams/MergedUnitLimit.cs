using System.Globalization;

namespace InfinityMercsApp.Domain.Fireteams;

/// <summary>
/// A unit slot in a fireteam: how many of the unit may join (Min to Max), the unit's slug, and whether the
/// minimum is written "*" (the unit is mandatory).
/// </summary>
public sealed record MergedUnitLimit(int Min, int Max, string? Slug, bool MinAsterisk)
{
    /// <summary>The fireteam the wildcard slots of every team are gathered into.</summary>
    public const string WildcardsTeamName = "Wildcards";

    public string MinText => MinAsterisk ? "*" : Min.ToString(CultureInfo.InvariantCulture);

    public string MaxText => Max.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The same slot seen in another chart: the lower Min and higher Max, this slot's slug unless it has
    /// none, and "*" when either slot has it.
    /// </summary>
    public MergedUnitLimit MergeWith(MergedUnitLimit other) => new(
        Math.Min(Min, other.Min),
        Math.Max(Max, other.Max),
        string.IsNullOrWhiteSpace(Slug) ? other.Slug : Slug,
        MinAsterisk || other.MinAsterisk);
}
