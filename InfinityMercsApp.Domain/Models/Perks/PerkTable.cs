using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Perks;

/// <summary>A perk list offered for display, labelled with the d20 range that selects it.</summary>
public sealed record PerkTableOption(string ListName, string DisplayName);

/// <summary>One track of a perk table: its roll label and the perk text in each tier column.</summary>
public sealed record PerkTableTrack(int TrackNumber, string RollLabel, IReadOnlyList<string> TierTexts);

/// <summary>A dependency within a track, from the tier that unlocks to the tier unlocked.</summary>
public readonly record struct PerkTableLink(int Track, int FromTier, int ToTier);

/// <summary>A perk list laid out as tracks (rows) by tiers (columns), with the dependencies between tiers.</summary>
public sealed partial record PerkTable(int TierCount, IReadOnlyList<PerkTableTrack> Tracks, IReadOnlyList<PerkTableLink> Links)
{
    /// <summary>Tables always show at least this many tier columns.</summary>
    public const int MinimumTierCount = 5;

    private static readonly IReadOnlyList<string> PreferredListOrder =
        ["Initiative", "Cool", "Body", "Reflex", "Intelligence", "Empathy", "Mecha"];

    /// <summary>Every perk list, attribute lists first in their usual order.</summary>
    public static IReadOnlyList<PerkTableOption> Options()
    {
        var names = CompanyPerkCatalog.GetPerkNodeLists()
            .Select(list => list.ListName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var ordered = PreferredListOrder
            .Where(name => names.Contains(name, StringComparer.OrdinalIgnoreCase))
            .Concat(names.Where(name => !PreferredListOrder.Contains(name, StringComparer.OrdinalIgnoreCase)));
        var definitions = CompanyPerkCatalog.GetPerkListCatalogEntries()
            .ToDictionary(list => list.Name, StringComparer.OrdinalIgnoreCase);

        return ordered
            .Select(name =>
            {
                var range = definitions.TryGetValue(name, out var definition) && definition.ListRollRanges.Count > 0
                    ? RollLabel(definition.ListRollRanges)
                    : "No Roll";
                return new PerkTableOption(name, $"{name} ({range})");
            })
            .ToList();
    }

    /// <summary>The table for a perk list, or null when the list is unknown or has no tracks.</summary>
    public static PerkTable? ForList(string listName)
    {
        var nodeList = CompanyPerkCatalog.GetPerkNodeLists()
            .FirstOrDefault(list => string.Equals(list.ListName, listName, StringComparison.OrdinalIgnoreCase));
        return nodeList is null ? null : Build(nodeList);
    }

    /// <summary>Lays out a node list; nodes whose ids carry no track and tier are left out.</summary>
    public static PerkTable? Build(PerkNodeList nodeList)
    {
        var nodes = nodeList.Roots
            .SelectMany(root => root.TraverseDepthFirst())
            .GroupBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var nodesByTrack = nodes
            .Where(node => TryParseTrackTier(node.Id, out _, out _))
            .GroupBy(node => { TryParseTrackTier(node.Id, out var track, out _); return track; })
            .OrderBy(group => group.Key)
            .ToList();
        if (nodesByTrack.Count == 0)
        {
            return null;
        }

        var tierCount = Math.Max(MinimumTierCount, nodes.Max(node => node.Tier));
        var definition = CompanyPerkCatalog.FindPerkListCatalogEntry(nodeList.ListId)
                         ?? CompanyPerkCatalog.FindPerkListCatalogEntry(nodeList.ListName);
        var rollLabels = definition?.Tracks.ToDictionary(track => track.TrackNumber, track => RollLabel(track.RollRanges));

        var tracks = nodesByTrack
            .Select(group =>
            {
                var textByTier = group
                    .GroupBy(node => node.Tier)
                    .ToDictionary(tier => tier.Key, tier => string.Join(Environment.NewLine, tier.Select(node => PerkText(node.Name))));
                var tierTexts = Enumerable.Range(1, tierCount)
                    .Select(tier => textByTier.GetValueOrDefault(tier) ?? string.Empty)
                    .ToList();
                var rollLabel = rollLabels?.GetValueOrDefault(group.Key) ?? group.Key.ToString();
                return new PerkTableTrack(group.Key, rollLabel, tierTexts);
            })
            .ToList();

        return new PerkTable(tierCount, tracks, CollectLinks(nodes));
    }

    /// <summary>Joins roll ranges as "1-5/11"; no ranges means the track is always available.</summary>
    public static string RollLabel(IReadOnlyList<CompanyPerkRollRange> ranges) =>
        ranges.Count == 0 ? "Always" : string.Join("/", ranges);

    /// <summary>Puts each comma-separated perk on its own line.</summary>
    public static string PerkText(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var parts = name.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length <= 1 ? name.Replace(",", string.Empty).Trim() : string.Join(Environment.NewLine, parts);
    }

    /// <summary>Reads the track and tier from an id such as "cool-track-4-tier-2".</summary>
    public static bool TryParseTrackTier(string? nodeId, out int track, out int tier)
    {
        track = 0;
        tier = 0;
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return false;
        }

        var match = TrackTierPattern().Match(nodeId);
        return match.Success &&
               int.TryParse(match.Groups["track"].Value, out track) &&
               int.TryParse(match.Groups["tier"].Value, out tier);
    }

    // Parent and child references describe the same dependency, so each link is kept once.
    private static List<PerkTableLink> CollectLinks(IEnumerable<PerkNode> nodes)
    {
        var links = new List<PerkTableLink>();
        foreach (var node in nodes)
        {
            if (!TryParseTrackTier(node.Id, out var track, out var tier))
            {
                continue;
            }

            if (TryParseTrackTier(node.ParentId, out var parentTrack, out var parentTier) && parentTrack == track)
            {
                links.Add(new PerkTableLink(track, parentTier, tier));
            }

            foreach (var child in node.Children)
            {
                if (TryParseTrackTier(child.Id, out var childTrack, out var childTier) && childTrack == track)
                {
                    links.Add(new PerkTableLink(track, tier, childTier));
                }
            }
        }

        return links.Distinct().ToList();
    }

    [GeneratedRegex(@"-track-(?<track>\d+)-tier-(?<tier>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex TrackTierPattern();
}
