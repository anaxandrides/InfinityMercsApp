using System.Globalization;
using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.Season;

internal static class SeasonDisplayUnitFormatter
{
    private const decimal CmPerGameInch = 2.5m;

    private static readonly Regex ExplicitDistancePattern = new(
        "(?<sign>[+-]?)(?<value>\\d+(?:\\.\\d+)?)\\s*(?<unit>cm|inches|inch|in|\")",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

    public static string ConvertExplicitDistances(string? text, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        }

        return ExplicitDistancePattern.Replace(text, match =>
        {
            var signToken = match.Groups["sign"].Value;
            var valueToken = match.Groups["value"].Value;
            var unitToken = match.Groups["unit"].Value;

            if (!decimal.TryParse(valueToken, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return match.Value;
            }

            if (signToken == "-")
            {
                value = -value;
            }

            var isInches = unitToken is "\"" ||
                           unitToken.Equals("in", StringComparison.OrdinalIgnoreCase) ||
                           unitToken.Equals("inch", StringComparison.OrdinalIgnoreCase) ||
                           unitToken.Equals("inches", StringComparison.OrdinalIgnoreCase);

            var converted = showUnitsInInches
                ? isInches ? value : Math.Round(value / CmPerGameInch, 0, MidpointRounding.AwayFromZero)
                : isInches ? value * CmPerGameInch : value;

            return $"{FormatSignedValue(converted, signToken)}{(showUnitsInInches ? "\"" : "cm")}";
        });
    }

    private static string FormatSignedValue(decimal value, string originalSign)
    {
        var formatted = FormatNumber(Math.Abs(value));
        if (value < 0)
        {
            return $"-{formatted}";
        }

        return originalSign == "+" ? $"+{formatted}" : formatted;
    }

    private static string FormatNumber(decimal value)
    {
        return decimal.Truncate(value) == value
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }
}
