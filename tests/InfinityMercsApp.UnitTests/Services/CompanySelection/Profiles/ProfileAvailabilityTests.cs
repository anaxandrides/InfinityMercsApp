using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.Services.CompanySelection.Profiles;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Profiles;

public sealed class ProfileAvailabilityTests
{
    private static readonly RosterRoom Room = new(PointsRemaining: 30, AvaReached: false, HasLieutenant: false);

    [Fact]
    public void AProfileThatFitsIsListedAndAddable() =>
        Assert.Equal(new ProfileAvailability(true, false), ProfileAvailability.Of(20, false, Room, UnitFilterCriteria.None, lieutenantOnly: false));

    [Fact]
    public void AProfileOverThePointsLeftIsHiddenAndBlocked() =>
        Assert.Equal(new ProfileAvailability(false, true), ProfileAvailability.Of(31, false, Room, UnitFilterCriteria.None, lieutenantOnly: false));

    [Fact]
    public void LieutenantsOnlyHidesTheRest()
    {
        Assert.False(ProfileAvailability.Of(20, false, Room, UnitFilterCriteria.None, lieutenantOnly: true).IsVisible);
        Assert.True(ProfileAvailability.Of(20, true, Room, UnitFilterCriteria.None, lieutenantOnly: true).IsVisible);
    }

    [Theory]
    [InlineData(9, false)]
    [InlineData(10, true)]
    [InlineData(25, false)]
    public void TheFiltersPointsHideProfilesOutsideThem(int cost, bool visible) =>
        Assert.Equal(visible, ProfileAvailability.Of(cost, false, Room, UnitFilterCriteria.None with { MinPoints = 10, MaxPoints = 20 }, false).IsVisible);

    [Fact]
    public void ASecondLieutenantIsBlocked()
    {
        var room = Room with { HasLieutenant = true };

        Assert.True(ProfileAvailability.Of(20, true, room, UnitFilterCriteria.None, false).IsBlocked);
        Assert.False(ProfileAvailability.Of(20, false, room, UnitFilterCriteria.None, false).IsBlocked);
    }

    [Fact]
    public void EveryProfileIsBlockedOnceTheUnitsAvaIsReached() =>
        Assert.True(ProfileAvailability.Of(20, false, Room with { AvaReached = true }, UnitFilterCriteria.None, false).IsBlocked);

    [Theory]
    [InlineData("2", true)]
    [InlineData("3", false)]
    [InlineData("T", false)]
    [InlineData("-", false)]
    public void TheRoomCountsTheUnitAgainstItsAva(string ava, bool reached)
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
}
