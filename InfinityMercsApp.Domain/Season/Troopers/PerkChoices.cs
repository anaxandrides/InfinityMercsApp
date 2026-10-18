using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Perks.Catalog;
using InfinityMercsApp.Domain.Models.Perks.Tables;

namespace InfinityMercsApp.Domain.Season.Troopers;

/// <summary>
/// Which perks a trooper may take. A perk needs a tier no higher than the trooper's rank (one higher when
/// rolled), the perk it follows on from already owned (from the profile or an earlier perk), and must not be
/// owned already. The Mecha list is for TAG perk points only.
/// </summary>
public static class PerkChoices
{
    public const string TagListId = "mecha";

    /// <summary>The lists a perk point can be spent on.</summary>
    public static IReadOnlyList<PerkNodeList> ListsFor(PerkPointKind kind) =>
        CompanyPerkCatalog.GetPerkNodeLists()
            .Where(list => IsTagList(list.ListId) == (kind == PerkPointKind.Tag))
            .ToList();

    /// <summary>
    /// Every perk in the list the trooper may choose, by track then tier. A TAG (<paramref name="isTag"/>) is never
    /// offered a perk giving a skill it may not have.
    /// </summary>
    public static IReadOnlyList<PerkPick> Selectable(string listId, int maxTier, IReadOnlySet<string> owned, bool isTag = false)
    {
        if (CompanyPerkCatalog.GetPerkNodeLists().FirstOrDefault(list => SameId(list.ListId, listId)) is not { } list)
        {
            return [];
        }

        return list.Roots
            .SelectMany(root => root.TraverseDepthFirst())
            .Where(node => node.Tier >= 1 && node.Tier <= maxTier && !owned.Contains(node.Id))
            .Where(node => string.IsNullOrWhiteSpace(node.ParentId) || owned.Contains(node.ParentId))
            .Where(node => !(isTag && TagRestrictions.IsBanned(node.Name)))
            .Select(node => PerkPosition.Parse(node.Id) is { } position
                ? new PerkPick(node.Id, node.Name, list.ListId, list.ListName, position.Track, node.Tier)
                : null)
            .OfType<PerkPick>()
            .OrderBy(pick => pick.Track)
            .ThenBy(pick => pick.Tier)
            .ToList();
    }

    /// <summary>The perk with this node id, as a pick; null when there is no such perk.</summary>
    public static PerkPick? PickFor(string nodeId)
    {
        foreach (var list in CompanyPerkCatalog.GetPerkNodeLists())
        {
            var node = list.Roots.Select(root => root.FindById(nodeId)).FirstOrDefault(found => found is not null);
            if (node is not null && PerkPosition.Parse(node.Id) is { } position)
            {
                return new PerkPick(node.Id, node.Name, list.ListId, list.ListName, position.Track, node.Tier);
            }
        }

        return null;
    }

    /// <summary>The lists a first d20 picks from when rolling for a perk; overlapping ranges offer a choice.</summary>
    public static IReadOnlyList<PerkListCatalogEntry> ListsForRoll(int roll) =>
        CompanyPerkCatalog.GetPerkListCatalogEntries()
            .Where(list => list.IsRandomlyGenerated && list.ListRollRanges.Any(range => range.Contains(roll)))
            .ToList();

    /// <summary>
    /// The perks a second d20 offers in the list: those on the tracks the roll lands on, up to one tier above
    /// the trooper's rank. When none can be taken the roll is made again.
    /// </summary>
    public static IReadOnlyList<PerkPick> ForTrackRoll(string listId, int trackRoll, int rank, IReadOnlySet<string> owned, bool isTag = false)
    {
        var tracks = CompanyPerkCatalog.FindPerkListCatalogEntry(listId)?.Tracks
            .Where(track => track.RollRanges.Any(range => range.Contains(trackRoll)))
            .Select(track => track.TrackNumber)
            .ToHashSet() ?? [];

        return Selectable(listId, rank + 1, owned, isTag).Where(pick => tracks.Contains(pick.Track)).ToList();
    }

    /// <summary>The perk node's list, e.g. "mecha" for "mecha-track-1-tier-2".</summary>
    public static bool IsTagPerk(string nodeId) => nodeId.StartsWith(TagListId + "-", StringComparison.OrdinalIgnoreCase);

    private static bool IsTagList(string listId) => SameId(listId, TagListId);

    private static bool SameId(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
