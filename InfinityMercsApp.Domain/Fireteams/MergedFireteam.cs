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
