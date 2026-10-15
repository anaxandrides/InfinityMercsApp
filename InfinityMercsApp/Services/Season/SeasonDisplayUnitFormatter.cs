using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Infrastructure.Providers;

namespace InfinityMercsApp.Services.Season;

/// <summary>The season pages' display units: the inches preference and movement in the chosen unit.</summary>
internal static class SeasonDisplayUnitFormatter
{
    public static bool GetShowUnitsInInches(IAppSettingsProvider? appSettingsProvider)
    {
        if (appSettingsProvider is null)
        {
            return true;
        }

        try
        {
            return appSettingsProvider.GetShowUnitsInInches();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Season display unit preference load failed: {ex.Message}");
            return true;
        }
    }

    public static string FormatMoveValue(
        string? savedMove,
        int? firstCm,
        int? secondCm,
        bool showUnitsInInches)
    {
        if (firstCm is > 0 && secondCm is > 0)
        {
            return Movement.Format(Movement.From(firstCm, secondCm), showUnitsInInches);
        }

        if (Movement.TryParse(savedMove, out var parsed))
        {
            return parsed.Format(showUnitsInInches);
        }

        return string.IsNullOrWhiteSpace(savedMove) ? "-" : savedMove.Trim();
    }
}
