using InfinityMercsApp.Domain.Models.Perks;

namespace InfinityMercsApp.UnitTests;

public sealed class UnitTestPerks
{
    [Fact]
    public void GetPerkNodeLists_AllNodes_HaveUniqueStableIds()
    {
        var ids = CompanyPerkCatalog.GetPerkNodeLists()
            .SelectMany(list => Flatten(list.Roots))
            .Select(node => node.Id)
            .ToList();

        Assert.DoesNotContain(ids, string.IsNullOrWhiteSpace);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void GetValidRollOptions_AllLists_AllRolls_ReturnsExpectedOptions()
    {
        foreach (var list in CompanyPerkCatalog.GetPerkListCatalogEntries())
        {
            int? listRoll = list.IsRandomlyGenerated
                ? list.ListRollRanges.First().Min
                : null;

            for (var trackRoll = 1; trackRoll <= 20; trackRoll++)
            {
                var actual = CompanyPerkCatalog
                    .GetValidRollOptions(list.Name, listRoll, trackRoll)
                    .Where(option => string.Equals(option.ListId, list.Id, StringComparison.OrdinalIgnoreCase))
                    .Select(option => (option.TrackNumber, option.Tier, option.PerkText, option.RequiredTier))
                    .OrderBy(option => option.TrackNumber)
                    .ThenBy(option => option.Tier)
                    .ToList();

                var expected = BuildExpectedOptions(list, trackRoll)
                    .OrderBy(option => option.TrackNumber)
                    .ThenBy(option => option.Tier)
                    .ToList();

                Assert.Equal(expected, actual);
            }
        }
    }

    private static List<(int TrackNumber, int Tier, string PerkText, int? RequiredTier)> BuildExpectedOptions(
        PerkListCatalogEntry list,
        int trackRoll)
    {
        var expected = new List<(int TrackNumber, int Tier, string PerkText, int? RequiredTier)>();
        var nodeList = CompanyPerkCatalog
            .GetPerkNodeLists()
            .First(x => string.Equals(x.ListId, list.Id, StringComparison.OrdinalIgnoreCase));
        var nodesByTrack = Flatten(nodeList.Roots)
            .Where(node => PerkPosition.Parse(node.Id) is not null)
            .GroupBy(node => (PerkPosition.Parse(node.Id)?.Track ?? 0))
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var track in list.Tracks.Where(track => track.RollRanges.Any(range => range.Contains(trackRoll))))
        {
            if (!nodesByTrack.TryGetValue(track.TrackNumber, out var trackNodes))
            {
                continue;
            }

            foreach (var tierGroup in trackNodes.GroupBy(node => node.Tier).OrderBy(group => group.Key))
            {
                var tier = tierGroup.Key;
                int? requiredTier = tierGroup
                    .Select(node =>
                    {
                        if (string.IsNullOrWhiteSpace(node.ParentId))
                        {
                            return (int?)null;
                        }

                        return PerkPosition.Parse(node.ParentId) is (var parentTrack, var parentTier) &&
                               parentTrack == track.TrackNumber &&
                               parentTier < tier
                            ? parentTier
                            : (int?)null;
                    })
                    .Where(value => value.HasValue)
                    .Select(value => value!.Value)
                    .DefaultIfEmpty()
                    .Min();

                if (requiredTier is > 0)
                {
                    continue;
                }

                var perkText = string.Join(
                    " OR ",
                    tierGroup.Select(node => node.Name).Distinct(StringComparer.OrdinalIgnoreCase));
                expected.Add((track.TrackNumber, tier, perkText, requiredTier is > 0 ? requiredTier : null));
            }
        }

        return expected;
    }

    private static IEnumerable<PerkNode> Flatten(IEnumerable<PerkNode> roots)
    {
        var stack = new Stack<PerkNode>(roots.Reverse());
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(node.Children[i]);
            }
        }
    }
}
