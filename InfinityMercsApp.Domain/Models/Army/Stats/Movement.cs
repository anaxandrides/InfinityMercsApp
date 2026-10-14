using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Army.Stats;

/// <summary>A trooper's two movement values in centimetres, e.g. 10-5.</summary>
public sealed partial record Movement(int FirstCm, int SecondCm)
{
    private const double CmPerInch = 2.54d;

    // Saved movement written in game inches converts at the game's 2.5 cm per inch.
    private const double CmPerGameInch = 2.5d;

    /// <summary>The movement when both values are given.</summary>
    public static Movement? From(int? firstCm, int? secondCm) =>
        firstCm is int first && secondCm is int second ? new Movement(first, second) : null;

    /// <summary>
    /// Reads saved movement text such as "10-5", "15cm-5cm", "6\"-2\"" or "4-2". Without units, values of 8 or
    /// less are taken as inches.
    /// </summary>
    public static bool TryParse(string? text, [NotNullWhen(true)] out Movement? movement)
    {
        movement = null;
        var match = MovePattern().Match(text?.Trim() ?? string.Empty);
        if (!match.Success ||
            !int.TryParse(match.Groups["a"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var a) ||
            !int.TryParse(match.Groups["b"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var b))
        {
            return false;
        }

        var firstUnit = match.Groups["au"].Value.Trim().ToLowerInvariant();
        var secondUnit = match.Groups["bu"].Value.Trim().ToLowerInvariant();
        var inInches = firstUnit.Length > 0 || secondUnit.Length > 0
            ? IsInchUnit(firstUnit) || IsInchUnit(secondUnit)
            : a <= 8 && b <= 8;

        movement = inInches ? new Movement(FromGameInches(a), FromGameInches(b)) : new Movement(a, b);
        return true;
    }

    /// <summary>"10-5" in centimetres, or the rounded inches ("4-2"); "-" when either value is not positive.</summary>
    public string Format(bool inInches)
    {
        if (FirstCm <= 0 || SecondCm <= 0)
        {
            return "-";
        }

        return inInches
            ? $"{Math.Round(FirstCm / CmPerInch):0}-{Math.Round(SecondCm / CmPerInch):0}"
            : $"{FirstCm.ToString(CultureInfo.InvariantCulture)}-{SecondCm.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>The formatted movement, or "-" when there is none.</summary>
    public static string Format(Movement? movement, bool inInches) => movement?.Format(inInches) ?? "-";

    private static int FromGameInches(int inches) => (int)Math.Round(inches * CmPerGameInch, MidpointRounding.AwayFromZero);

    private static bool IsInchUnit(string unit) => unit is "\"" or "in" or "inch" or "inches";

    [GeneratedRegex(@"^(?<a>\d+)\s*(?<au>cm|""|in|inch|inches)?\s*[-/]\s*(?<b>\d+)\s*(?<bu>cm|""|in|inch|inches)?$", RegexOptions.IgnoreCase)]
    private static partial Regex MovePattern();
}
