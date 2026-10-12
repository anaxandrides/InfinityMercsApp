using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.Views.Common;

internal sealed record CompanyFireteamChartEntry(
    string Name,
    int Duo,
    int Haris,
    int Core,
    IReadOnlyList<(string Name, int Min, int Max, string? Slug, bool MinAsterisk)> UnitLimits);

internal static class CompanyUnitDetailsFireteamCommon
{
    /// <summary>
    /// The named fireteams of an army's fireteam chart, with their DUO/HARIS/CORE counts and unit slots.
    /// </summary>
    internal static IReadOnlyList<CompanyFireteamChartEntry> ParseEntries(string? fireteamChartJson, Action<string>? logError = null)
    {
        var entries = new List<CompanyFireteamChartEntry>();
        foreach (var team in ArmyJsonReader.Shared.ReadFireteamChart(fireteamChartJson).Teams)
        {
            var name = team.CompanyName;
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var (duo, haris, core) = team.TypeCounts;
            var unitLimits = team.Units
                .Select(unit => (unit.CompanyDisplayName, unit.Min ?? 0, unit.Max ?? 0, unit.CompanySlug, unit.IsMandatory))
                .ToList();
            entries.Add(new CompanyFireteamChartEntry(name, duo, haris, core, unitLimits));
        }

        return entries;
    }
}
