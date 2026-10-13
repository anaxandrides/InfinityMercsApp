using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.Fireteams;

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
    public Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> UnitLimits { get; } = new(StringComparer.OrdinalIgnoreCase);

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

            var (duo, haris, core) = team.TypeCounts;
            merged.Duo += duo;
            merged.Haris += haris;
            merged.Core += core;
            foreach (var unit in team.Units)
            {
                merged.MergeUnitLimit(unit.CompanyDisplayName, unit.Min ?? 0, unit.Max ?? 0, unit.CompanySlug, unit.IsMandatory);
            }
        }
    }

    private void MergeUnitLimit(string unitName, int min, int max, string? slug, bool minAsterisk)
    {
        if (UnitLimits.TryGetValue(unitName, out var existing))
        {
            UnitLimits[unitName] = (
                Math.Min(existing.Min, min),
                Math.Max(existing.Max, max),
                string.IsNullOrWhiteSpace(existing.Slug) ? slug : existing.Slug,
                existing.MinAsterisk || minAsterisk);
            return;
        }

        UnitLimits[unitName] = (min, max, slug, minAsterisk);
    }
}
