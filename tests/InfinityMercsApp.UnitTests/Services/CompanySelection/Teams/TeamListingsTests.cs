using InfinityMercsApp.Domain.Fireteams;
using InfinityMercsApp.Services.CompanySelection.Teams;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Teams;

public sealed class TeamListingsTests
{
    private static readonly ListedUnit Fusilier = new(1, "Fusilier", "fusilier");
    private static readonly ListedUnit Hero = new(2, "Hero", "hero", IsCharacter: true);
    private static readonly IReadOnlyList<ListedUnit> Units = [Fusilier, Hero];

    private static MergedFireteam Team(string name, params IEnumerable<string> slots)
    {
        var team = new MergedFireteam(name);
        foreach (var slot in slots)
        {
            team.UnitLimits[slot] = new MergedUnitLimit(0, 3, slot.ToLowerInvariant(), MinAsterisk: false);
        }

        return team;
    }

    [Fact]
    public void Build_TeamWithCharacter_ListsNamedSlotsWithoutIt()
    {
        var team = Team("Fusilier Team", "Fusilier", "Hero");

        var listing = Assert.Single(TeamListings.Build(Units, [team], [team]));

        Assert.Equal("Fusilier Team", listing.Name);
        Assert.False(listing.IsWildcardBucket);
        var slot = Assert.Single(listing.Slots);
        Assert.Equal(new TeamSlotListing<ListedUnit>("Fusilier", "0", "3", "fusilier", Fusilier), slot);
    }

    [Fact]
    public void Build_WildcardSlots_GoToOneBucketAtTheEnd()
    {
        var listed = Team("Fusilier Team", "Fusilier", "Wildcard");
        var unlisted = Team("Other Team", "Wild Trooper");

        var listings = TeamListings.Build(Units, [listed], [listed, unlisted]);

        Assert.Equal(["Fusilier Team", MergedUnitLimit.WildcardsTeamName], listings.Select(x => x.Name));
        Assert.True(listings[1].IsWildcardBucket);
        Assert.Equal(["Wild Trooper", "Wildcard"], listings[1].Slots.Select(x => x.Name));
    }

    [Fact]
    public void Build_TeamOfCharactersOnly_LeavesItOut() =>
        Assert.Empty(TeamListings.Build(Units, [Team("Hero Team", "Hero")], []));

    [Fact]
    public void UnitFor_Slot_MatchesResolvedThenSlugThenName()
    {
        var slot = new SlotFor("Somebody", "fusilier");

        Assert.Same(Fusilier, TeamSlots.UnitFor(Units, slot));
        Assert.Same(Hero, TeamSlots.UnitFor(Units, slot with { Slug = null, Name = "Hero" }));
        Assert.Same(Hero, TeamSlots.UnitFor(Units, slot with { ResolvedUnitId = 2, ResolvedSourceFactionId = 101 }));
    }

    [Fact]
    public void Shows_Slot_WhileItsUnitIsVisible()
    {
        var slot = new SlotFor("Fusilier", "fusilier");

        Assert.True(VisibleUnits.Of(Units).Shows(slot, inWildcardBucket: false));
        Assert.False(VisibleUnits.Of([Fusilier with { IsVisible = false }]).Shows(slot, inWildcardBucket: false));
        Assert.False(VisibleUnits.Of(Units).Shows(slot with { IsCharacter = true }, inWildcardBucket: false));
    }

    private sealed record SlotFor(string Name, string? Slug, bool IsCharacter = false, int? ResolvedUnitId = null, int? ResolvedSourceFactionId = null) : ITeamSlot;
}
