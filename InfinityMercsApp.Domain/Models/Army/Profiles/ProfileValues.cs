using System.Globalization;

namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// Shared interpretation of the API's loosely typed cost values.
/// </summary>
public static class ProfileValues
{
    /// <summary>
    /// Reads a cost such as <c>"25"</c> or <c>"x7"</c> as a number: the whole text when it is an integer,
    /// otherwise its first run of digits, otherwise 0.
    /// </summary>
    /// <summary>
    /// How many of a unit a company may field, from its AVA text; null when unlimited ("T", "-", a blank, or the
    /// 255 that stands for Total).
    /// </summary>
    public static int? ParseAvaLimit(string? ava) =>
        int.TryParse(ava?.Trim(), out var limit) && limit < 255 ? limit : null;

    public static int ParseCost(string? cost)
    {
        if (string.IsNullOrWhiteSpace(cost))
        {
            return 0;
        }

        if (int.TryParse(cost, out var parsed))
        {
            return parsed;
        }

        var match = System.Text.RegularExpressions.Regex.Match(cost, "\\d+");
        return match.Success && int.TryParse(match.Value, out var fallback) ? fallback : 0;
    }

    /// <summary>True when an SWC text such as <c>"0.5"</c> or <c>"+1"</c> is greater than zero.</summary>
    public static bool IsPositiveSwc(string? swc)
    {
        if (string.IsNullOrWhiteSpace(swc) || swc == "-")
        {
            return false;
        }

        return decimal.TryParse(swc, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0m;
    }

    /// <summary>
    /// True when an option costs SWC, such as <c>"0.5"</c>. A <c>"+1"</c> gives the army SWC instead (a
    /// lieutenant's bonus), so it costs none.
    /// </summary>
    public static bool CostsSwc(string? swc) => IsPositiveSwc(swc) && SwcGrant(swc) == 0;

    /// <summary>The SWC a profile gives rather than costs, written <c>"+1"</c>; 0 for anything else.</summary>
    public static double SwcGrant(string? swc)
    {
        var text = swc?.Trim() ?? string.Empty;
        return text.StartsWith('+') &&
               double.TryParse(text[1..], NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : 0;
    }
}
