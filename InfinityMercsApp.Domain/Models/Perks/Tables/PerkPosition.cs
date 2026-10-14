using System.Globalization;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Perks.Tables;

/// <summary>Where a perk node sits in its list: its track and tier, read from ids such as "cool-track-4-tier-2".</summary>
public sealed partial record PerkPosition(int Track, int Tier)
{
    /// <summary>The position in a node id; null when the id has no "-track-N-tier-M" part.</summary>
    public static PerkPosition? Parse(string? nodeId)
    {
        var match = TrackTierPattern().Match(nodeId ?? string.Empty);
        return match.Success &&
               int.TryParse(match.Groups["track"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var track) &&
               int.TryParse(match.Groups["tier"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var tier)
            ? new PerkPosition(track, tier)
            : null;
    }

    [GeneratedRegex(@"-track-(?<track>\d+)-tier-(?<tier>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex TrackTierPattern();
}
