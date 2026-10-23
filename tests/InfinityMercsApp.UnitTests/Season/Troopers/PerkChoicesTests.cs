using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Perks.Catalog;
using InfinityMercsApp.Domain.Season.Troopers;

namespace InfinityMercsApp.UnitTests.Season.Troopers;

public sealed class PerkChoicesTests
{
    private static IReadOnlySet<string> Owned(params IEnumerable<string> ids) => new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void Selectable_RankOne_OffersTierOne()
    {
        var picks = PerkChoices.Selectable("body-8-13", 1, Owned());

        Assert.Contains(picks, pick => pick.NodeId == "body-track-1-tier-1");
        Assert.All(picks, pick => Assert.Equal(1, pick.Tier));
        Assert.All(picks, pick => Assert.Equal("Body", pick.ListName));
    }

    [Fact]
    public void Selectable_HigherTier_NeedsThePerkBeforeIt()
    {
        Assert.DoesNotContain(PerkChoices.Selectable("body-8-13", 3, Owned()), pick => pick.NodeId == "body-track-1-tier-3");
        Assert.Contains(PerkChoices.Selectable("body-8-13", 3, Owned("body-track-1-tier-1")), pick => pick.NodeId == "body-track-1-tier-3");
    }

    [Fact]
    public void Selectable_OwnedPerk_IsNotOfferedAgain()
    {
        Assert.DoesNotContain(PerkChoices.Selectable("body-8-13", 1, Owned("body-track-1-tier-1")), pick => pick.NodeId == "body-track-1-tier-1");
    }

    [Fact]
    public void ListsFor_TagPoints_OnlyTheMechaTree()
    {
        Assert.DoesNotContain(PerkChoices.ListsFor(PerkPointKind.Standard), list => list.ListId == PerkChoices.TagListId);
        Assert.Equal([PerkChoices.TagListId], PerkChoices.ListsFor(PerkPointKind.Tag).Select(list => list.ListId));
        Assert.True(PerkChoices.IsTagPerk("mecha-track-1-tier-1"));
        Assert.False(PerkChoices.IsTagPerk("body-track-1-tier-1"));
    }

    // The rules' Mecha tree: Tactical Awareness (level 3) and the Ancillary's Explode (level 4) need no other perk,
    // only the rank.
    [Theory]
    [InlineData("mecha-track-5-tier-3", 3)]
    [InlineData("mecha-track-6-tier-4", 4)]
    public void Selectable_StandaloneMechaPerk_IsOfferedFromItsLevelWithoutTheOneBefore(string perk, int level)
    {
        var below = PerkChoices.Selectable(PerkChoices.TagListId, maxTier: level - 1, owned: new HashSet<string>(), isTag: true);
        var at = PerkChoices.Selectable(PerkChoices.TagListId, maxTier: level, owned: new HashSet<string>(), isTag: true);

        Assert.DoesNotContain(below, pick => pick.NodeId == perk);
        Assert.Contains(at, pick => pick.NodeId == perk);
    }

    [Fact]
    public void ListsForRoll_OverlappingRanges_OffersAChoice()
    {
        Assert.Equal(["Initiative", "Cool"], PerkChoices.ListsForRoll(5).Select(list => list.Name));
        Assert.Equal(["Empathy"], PerkChoices.ListsForRoll(20).Select(list => list.Name));
    }

    [Fact]
    public void ForTrackRoll_Roll_OffersTracksUpToOneTierAboveRank()
    {
        // Body tracks 1 (1-4) and 2 (3-6) both cover a 4; an unranked trooper may roll a tier 1 perk.
        var picks = PerkChoices.ForTrackRoll("body-8-13", 4, 0, Owned());

        Assert.Equal(["body-track-1-tier-1", "body-track-2-tier-1"], picks.Select(pick => pick.NodeId));
    }

    [Fact]
    public void Selectable_Tag_LeavesOutPerksGivingBannedSkills()
    {
        var list = CompanyPerkCatalog.GetPerkNodeLists().Single(candidate => candidate.ListName == "Initiative");
        var nodes = list.Roots.SelectMany(root => root.TraverseDepthFirst()).ToList();
        var owned = Owned(nodes.Where(node => !TagRestrictions.IsBanned(node.Name)).Select(node => node.Id));

        Assert.Contains(PerkChoices.Selectable(list.ListId, 6, owned), pick => TagRestrictions.IsBanned(pick.Name));
        Assert.DoesNotContain(PerkChoices.Selectable(list.ListId, 6, owned, isTag: true), pick => TagRestrictions.IsBanned(pick.Name));
    }
}
