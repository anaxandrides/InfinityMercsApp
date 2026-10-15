using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Company;

/// <summary>Rewrites every distance written in profile text ("+5cm", "3\"") in the reader's preferred unit.</summary>
public static partial class TextDistances
{
    // A game inch is 2.5 cm.
    private const decimal CmPerGameInch = 2.5m;

    /// <summary>
    /// The text with every explicit distance in inches (rounded to whole inches) or centimetres, keeping any
    /// sign; blank text comes back empty.
    /// </summary>
    public static string Convert(string? text, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return ExplicitDistance().Replace(text, match =>
        {
            var sign = match.Groups["sign"].Value;
            if (!decimal.TryParse(match.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return match.Value;
            }

            if (sign == "-")
            {
                value = -value;
            }

            var unit = match.Groups["unit"].Value;
            var isInches = unit == "\"" || unit.StartsWith("in", StringComparison.OrdinalIgnoreCase);
            var converted = showUnitsInInches
                ? isInches ? value : Math.Round(value / CmPerGameInch, 0, MidpointRounding.AwayFromZero)
                : isInches ? value * CmPerGameInch : value;

            return $"{Signed(converted, sign)}{(showUnitsInInches ? "\"" : "cm")}";
        });
    }

    private static string Signed(decimal value, string originalSign)
    {
        var magnitude = Math.Abs(value);
        var number = decimal.Truncate(magnitude) == magnitude
            ? magnitude.ToString("0", CultureInfo.InvariantCulture)
            : magnitude.ToString("0.##", CultureInfo.InvariantCulture);
        return value < 0 ? $"-{number}" : originalSign == "+" ? $"+{number}" : number;
    }

    [GeneratedRegex("(?<sign>[+-]?)(?<value>\\d+(?:\\.\\d+)?)\\s*(?<unit>cm|inches|inch|in|\")", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitDistance();
}
