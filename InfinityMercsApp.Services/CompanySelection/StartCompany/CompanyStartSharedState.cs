using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanySelection.Units;

namespace InfinityMercsApp.Services.CompanySelection.StartCompany;

internal static class CompanyStartSharedState
{
    internal static bool IsCompanyNameValid(string? value) => !string.IsNullOrWhiteSpace(value);

    internal static string ExtractUnitTypeCode(string? subtitle)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return string.Empty;
        }

        var firstToken = subtitle
            .Split([' ', '-', '\u2013', '\u2014'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstToken) ? string.Empty : firstToken.Trim().ToUpperInvariant();
    }

    internal static string ComputeTotalCostText<TEntry>(IEnumerable<TEntry> entries)
        where TEntry : ICompanyMercsEntry
    {
        return entries.Sum(x => x.CostValue).ToString();
    }

    internal static void RefreshMercsCompanyEntryDistanceDisplays<TEntry>(
        IEnumerable<TEntry> entries,
        bool showUnitsInInches)
        where TEntry : ICompanyMercsEntry
    {
        foreach (var entry in entries)
        {
            var moveDisplay = Movement.Format(Movement.From(entry.UnitMoveFirstCm, entry.UnitMoveSecondCm), showUnitsInInches);
            entry.UnitMoveDisplay = moveDisplay;
            entry.Subtitle = CompanyUnitDetailsShared.ReplaceSubtitleMoveDisplay(entry.Subtitle, moveDisplay);

            if (entry.HasPeripheralStatBlock)
            {
                entry.PeripheralMov = Movement.Format(Movement.From(entry.PeripheralMoveFirstCm, entry.PeripheralMoveSecondCm), showUnitsInInches);
            }
        }
    }

    internal static bool IsSeasonValid<TEntry>(
        IEnumerable<TEntry> entries,
        string selectedStartSeasonPoints,
        string seasonPointsCapText)
        where TEntry : ICompanyMercsEntry
    {
        var hasLieutenant = entries.Any(x => x.IsLieutenant);
        var pointsLimit = int.TryParse(selectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(seasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        return hasLieutenant && currentPoints <= pointsLimit;
    }
}

