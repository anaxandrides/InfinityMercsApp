using System.Text.RegularExpressions;
using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Perks;

namespace InfinityMercsApp.Domain.Company;

/// <summary>How a saved company's troopers are named and labelled when shown.</summary>
public static partial class SavedUnitText
{
    public const string CaptainIconPath = "SVGCache/NonCBIcons/noun-captain-8115950.svg";

    /// <summary>The unit's name without its bracketed parts, e.g. "Fusilier (Lieutenant)" → "Fusilier"; "Unit" when nothing is left.</summary>
    public static string BaseDisplayName(string? baseUnitName)
    {
        if (string.IsNullOrWhiteSpace(baseUnitName))
        {
            return "Unit";
        }

        var withoutBrackets = Bracketed().Replace(baseUnitName, " ").Trim();
        var collapsed = RepeatedSpaces().Replace(withoutBrackets, " ").Trim();
        return collapsed.Length == 0 ? "Unit" : collapsed;
    }

    /// <summary>The unit type (LI, MI, HI, TAG, REM…) in upper case; empty when missing or when MOV was saved in its place.</summary>
    public static string UnitTypeCode(string? unitTypeCode)
    {
        if (string.IsNullOrWhiteSpace(unitTypeCode))
        {
            return string.Empty;
        }

        var normalized = unitTypeCode.Trim().ToUpperInvariant();
        return StatKeys.TryParse(normalized, out var stat) && stat == Stat.Mov ? string.Empty : normalized;
    }

    /// <summary>The star badge for a trooper's experience rank; empty below the first rank.</summary>
    public static string ExperienceIconPath(int experiencePoints)
    {
        var level = CompanyUnitExperienceRanks.GetRankLevel(experiencePoints);
        return level <= 0 ? string.Empty : $"SVGCache/NonCBIcons/Experience/noun-{level}-stars.svg";
    }

    /// <summary>The names of a captain's chosen upgrades, without cost prefixes, blanks, "-" or repeats.</summary>
    public static IReadOnlyList<string> CaptainChoiceNames(params IEnumerable<string?> choices) => choices
        .Select(choice => choice?.Trim() ?? string.Empty)
        .Where(choice => choice.Length > 0 && choice != ProfileText.Empty)
        .Select(choice => CostPrefix().Replace(choice, string.Empty).Trim())
        .Where(choice => choice.Length > 0)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <summary>A saved profile section tidied to one item per line, or "-" when empty.</summary>
    public static string Section(string? savedText)
    {
        var lines = ProfileText.Lines(savedText).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return lines.Count == 0 ? ProfileText.Empty : string.Join(Environment.NewLine, lines);
    }

    [GeneratedRegex(@"\s*\([^)]*\)\s*")]
    private static partial Regex Bracketed();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex RepeatedSpaces();

    [GeneratedRegex(@"^\s*\([-+]?\d+\)\s*-\s*")]
    private static partial Regex CostPrefix();
}
