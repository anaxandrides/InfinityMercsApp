using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// Converts a distance such as <c>"+5cm"</c>, <c>"10"</c> or <c>"4\""</c> to the display unit.
/// </summary>
public static class DistanceText
{
    public static string Convert(string distanceText, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        var match = Regex.Match(
            distanceText,
            @"([+-]?)(\d+(?:\.\d+)?)(?:\s*(cm|""|in|inch|inches))?",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return distanceText;
        }

        var sign = match.Groups[1].Value;
        var numberText = match.Groups[2].Value;
        if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return distanceText;
        }

        var unitToken = match.Groups[3].Success
            ? match.Groups[3].Value.Trim().ToLowerInvariant()
            : string.Empty;
        var valueInCm = unitToken is "\"" or "in" or "inch" or "inches"
            ? parsedValue * 2.5
            : parsedValue;

        string replacement;
        if (showUnitsInInches)
        {
            var inches = (int)Math.Round(valueInCm / 2.5, MidpointRounding.AwayFromZero);
            replacement = $"{sign}{inches}\"";
        }
        else
        {
            var roundedCm = Math.Round(valueInCm, 2, MidpointRounding.AwayFromZero);
            var cmText = Math.Abs(roundedCm - Math.Round(roundedCm)) < 0.001
                ? ((int)Math.Round(roundedCm)).ToString(CultureInfo.InvariantCulture)
                : roundedCm.ToString("0.##", CultureInfo.InvariantCulture);
            replacement = $"{sign}{cmText}cm";
        }

        return distanceText.Remove(match.Index, match.Length).Insert(match.Index, replacement);
    }
}
