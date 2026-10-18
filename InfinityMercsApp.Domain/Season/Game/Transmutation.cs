using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Season.Game;

/// <summary>
/// Transmutation (X): once the trooper has taken X wounds, it switches to its second profile and keeps its wounds.
/// Each full VITA or STR it has gained raises X by as much. Symbio-Resonance armour stops it degrading at all.
/// </summary>
public static partial class Transmutation
{
    /// <summary>The armour that keeps a Transmutation (X) trooper in its first profile.</summary>
    public const string SymbioResonance = "Symbio - Resonance";

    /// <summary>X in "Transmutation (2)"; null for any other skill, or Transmutation (Auto), (WIP) or (Hatching).</summary>
    public static int? Threshold(string? skill) =>
        ThresholdPattern().Match(skill?.Trim() ?? string.Empty) is { Success: true } match &&
        int.TryParse(match.Groups["x"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var threshold)
            ? threshold
            : null;

    /// <summary>The threshold once the trooper's VITA has risen above its profile's, a wound more for each.</summary>
    public static int Raised(int threshold, int profileVitality, int vitality) => threshold + Math.Max(0, vitality - profileVitality);

    /// <summary>Whether the armour is Symbio-Resonance.</summary>
    public static bool IsSymbioResonance(string? armorName) =>
        string.Equals(Compact(armorName), Compact(SymbioResonance), StringComparison.OrdinalIgnoreCase);

    private static string Compact(string? text) => string.Concat((text ?? string.Empty).Where(c => !char.IsWhiteSpace(c) && c != '-'));

    [GeneratedRegex(@"^Transmutation\s*\(\s*(?<x>\d+)\s*\)$", RegexOptions.IgnoreCase)]
    private static partial Regex ThresholdPattern();
}
