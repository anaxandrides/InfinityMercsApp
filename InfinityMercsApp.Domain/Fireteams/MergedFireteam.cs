using System.Globalization;
using InfinityMercsApp.Domain.Models.Army.Profiles;

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

/// <summary>
/// A named fireteam merged across the fireteam charts of every army a company draws from: the DUO,
/// HARIS and CORE counts add up, and each unit slot keeps the lowest Min and highest Max seen.
/// </summary>
public sealed class MergedFireteam(string name)
{
    public string Name { get; } = name;

    public int Duo { get; private set; }

    public int Haris { get; private set; }

    public int Core { get; private set; }

    /// <summary>Unit slots keyed by slot label (case-insensitive).</summary>
    public Dictionary<string, MergedUnitLimit> UnitLimits { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Merges every named team of a chart into <paramref name="target"/>, keyed by team name.</summary>
    public static void MergeChart(FireteamChart? chart, IDictionary<string, MergedFireteam> target)
    {
        foreach (var team in (chart ?? FireteamChart.Empty).Teams)
        {
            var name = team.CompanyName;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!target.TryGetValue(name, out var merged))
            {
                merged = new MergedFireteam(name);
                target[name] = merged;
            }

            var counts = team.TypeCounts;
            merged.Duo += counts.Duo;
            merged.Haris += counts.Haris;
            merged.Core += counts.Core;
            foreach (var unit in team.Units)
            {
                merged.MergeUnitLimit(
                    unit.CompanyDisplayName,
                    new MergedUnitLimit(unit.Min ?? 0, unit.Max ?? 0, unit.CompanySlug, unit.IsMandatory));
            }
        }
    }

    private void MergeUnitLimit(string unitName, MergedUnitLimit limit) =>
        UnitLimits[unitName] = UnitLimits.TryGetValue(unitName, out var existing) ? existing.MergeWith(limit) : limit;
}
