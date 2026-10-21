using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Services.CompanySelection.Profiles;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Profiles;

public sealed class ProfileAvailabilityTests
{
    private static readonly RosterRoom Room = new(PointsRemaining: 30, AvaReached: false, HasLieutenant: false);

    [Fact]
    public void Of_ProfileFits_IsListedAndAddable() =>
        Assert.Equal(new ProfileAvailability(true, false), ProfileAvailability.Of(20, false, Room, UnitFilterCriteria.None, lieutenantOnly: false));

    [Fact]
    public void Of_OverThePointsLeft_IsHiddenAndBlocked() =>
        Assert.Equal(new ProfileAvailability(false, true), ProfileAvailability.Of(31, false, Room, UnitFilterCriteria.None, lieutenantOnly: false));

    [Fact]
    public void Of_LieutenantsOnly_HidesTheRest()
    {
        Assert.False(ProfileAvailability.Of(20, false, Room, UnitFilterCriteria.None, lieutenantOnly: true).IsVisible);
        Assert.True(ProfileAvailability.Of(20, true, Room, UnitFilterCriteria.None, lieutenantOnly: true).IsVisible);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(25, false)]
    public void Of_OutsideTheFiltersPoints_IsHidden(int cost, bool visible) =>
        Assert.Equal(visible, ProfileAvailability.Of(cost, false, Room, UnitFilterCriteria.None with { MinPoints = 10, MaxPoints = 20 }, false).IsVisible);

    [Fact]
    public void Of_SecondLieutenant_IsBlocked()
    {
        var room = Room with { HasLieutenant = true };

        Assert.True(ProfileAvailability.Of(20, true, room, UnitFilterCriteria.None, false).IsBlocked);
        Assert.False(ProfileAvailability.Of(20, false, room, UnitFilterCriteria.None, false).IsBlocked);
    }

    [Fact]
    public void Of_AvaReached_IsBlocked() =>
        Assert.True(ProfileAvailability.Of(20, false, Room with { AvaReached = true }, UnitFilterCriteria.None, false).IsBlocked);

    [Theory]
    [InlineData("3", true)]
    [InlineData("4", false)]
    [InlineData("T", false)]
    [InlineData("-", false)]
    public void RosterRoomFor_UnitInCompanyFromEitherFaction_CountsItAgainstItsAva(string ava, bool reached)
    {
        List<MercsCompanyEntry> entries =
        [
            new() { SourceUnitId = 7, SourceFactionId = 101, IsLieutenant = true },
            new() { SourceUnitId = 7, SourceFactionId = 101 },
            new() { SourceUnitId = 7, SourceFactionId = 102 }
        ];

        var room = RosterRoom.For(entries, pointsLimit: 75, currentPoints: 40, ava, unitId: 7, factionId: 101);

        Assert.Equal(new RosterRoom(35, reached, HasLieutenant: true), room);
    }

    // The TAG Company's own TAG is unit 1 there, as Fusiliers are elsewhere.
    [Fact]
    public void RosterRoomFor_TagCompanyUnitWithAnotherUnitsId_CountsOnlyItsOwn()
    {
        List<MercsCompanyEntry> entries = [new() { SourceUnitId = 1, SourceFactionId = 101 }];

        Assert.False(RosterRoom.For(entries, 75, 0, "1", unitId: 1, factionId: TagCompanyFactionGenerator.TagCompanyFactionId).AvaReached);
        Assert.True(RosterRoom.For(entries, 75, 0, "1", unitId: 1, factionId: 102).AvaReached);
    }

    // An Inspiring company's captain is Fusiliers (unit 1); its generated faction lists Fusiliers as unit 12 and
    // another unit as 1.
    [Fact]
    public void RosterRoomFor_GeneratedFactionUnit_MatchesByItsArmyUnit()
    {
        List<MercsCompanyEntry> entries = [new() { SourceUnitId = 1, SourceFactionId = 101, IsLieutenant = true }];
        List<MercsCompanyEntry> recruited = [new() { SourceUnitId = 12, SourceFactionId = 2002, LogoSourceUnitId = 1, LogoSourceFactionId = 101 }];

        Assert.True(RosterRoom.For(entries, 75, 0, "1", unitId: 1, factionId: 101).AvaReached);
        Assert.True(RosterRoom.For(recruited, 75, 0, "1", unitId: 1, factionId: 102).AvaReached);
        Assert.False(RosterRoom.For(recruited, 75, 0, "1", unitId: 12, factionId: 101).AvaReached);
    }
}
