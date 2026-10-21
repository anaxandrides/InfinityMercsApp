using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>
/// The display label of a captain upgrade choice: its experience cost and name, e.g. <c>"(2) - Sniper"</c>.
/// </summary>
public static partial class CaptainChoiceLabel
{
    /// <summary>The label of an empty choice slot.</summary>
    public const string None = "(None)";

    public static string Format(int cost, string name) => $"({Math.Max(0, cost)}) - {name}";

    /// <summary>The experience cost in a label's leading "(n)"; 0 for <see cref="None"/> or a label without one.</summary>
    public static int Cost(string? label)
    {
        if (string.IsNullOrWhiteSpace(label) || string.Equals(label, None, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var match = CostPrefix().Match(label);
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cost)
            ? cost
            : 0;
    }

    /// <summary>
    /// The chosen item's name without the cost prefix; empty for <see cref="None"/>. "Lt" reads as "Lieutenant".
    /// </summary>
    public static string Name(string? label)
    {
        var value = label ?? string.Empty;
        if (string.Equals(value, None, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var name = CostAndDashPrefix().Replace(value, string.Empty).Trim();
        return string.Equals(name, "Lt", StringComparison.OrdinalIgnoreCase) ? "Lieutenant" : name;
    }

    [GeneratedRegex(@"^\s*\(([-+]?\d+)\)")]
    private static partial Regex CostPrefix();

    [GeneratedRegex(@"^\s*\([-+]?\d+\)\s*-\s*")]
    private static partial Regex CostAndDashPrefix();
}
