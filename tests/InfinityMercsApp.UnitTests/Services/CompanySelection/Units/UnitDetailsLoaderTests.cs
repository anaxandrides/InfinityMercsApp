using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Units;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Units;

public sealed class UnitDetailsLoaderTests
{
    private const int UnitId = 5;

    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();

    private UnitDetailsLoader Loader() => new(_armyData);

    private void Lists(int factionId, int? ava) =>
        _armyData.GetUnit(factionId, UnitId, Arg.Any<CancellationToken>()).Returns(new Unit
        {
            FactionId = factionId,
            UnitId = UnitId,
            ProfileGroups = new UnitProfileGroups([new ProfileGroup { Profiles = [new UnitProfile { Ava = ava }] }])
        });

    [Fact]
    public void HighestAva_UnitInBothSectorials_UsesTheHigher()
    {
        Lists(101, 2);
        Lists(102, 4);

        Assert.Equal("4", Loader().HighestAva(UnitId, [101, 102]));
    }

    [Fact]
    public void HighestAva_TotalInOne_IsTotal()
    {
        Lists(101, 255);
        Lists(102, 3);

        Assert.Equal("T", Loader().HighestAva(UnitId, [102, 101]));
    }

    [Fact]
    public void HighestAva_OnlyOneFactionListsIt_UsesThatFactions()
    {
        Lists(101, 1);

        Assert.Equal("1", Loader().HighestAva(UnitId, [101, 102]));
    }

    [Fact]
    public void HighestAva_NoFactionListsIt_IsNull() =>
        Assert.Null(Loader().HighestAva(UnitId, [101]));

    [Fact]
    public void Load_SpecOpsUnit_ShowsOnlyItsOwnGroupAsLieutenant()
    {
        Snapshot(101);
        var groups = Groups(Option(1, "0", Doctor));
        var specBot = new ProfileGroup { Id = 2, Profiles = [new UnitProfile { Id = 1, Name = "SpecBot" }], Options = [Option(1, "0", Camo)] };
        _armyData.GetUnit(101, UnitId, Arg.Any<CancellationToken>()).Returns(new Unit
        {
            UnitId = UnitId,
            ProfileGroups = new UnitProfileGroups([groups.Groups[0], specBot])
        });

        var details = Loader().Load(101, UnitId, isSpecOps: true, lieutenantOnly: false, showUnitsInInches: false);

        Assert.True(details.TreatAsSpecOps);
        Assert.Equal(1, Assert.Single(details.ProfileGroups.Groups).Id);
        Assert.Contains("Lieutenant", details.CommonSkills);
        Assert.Contains("Doctor", details.CommonSkills);
    }

    [Fact]
    public void Load_NoUnitAnywhere_HasNoProfiles()
    {
        var details = Loader().Load(101, UnitId, isSpecOps: false, lieutenantOnly: false, showUnitsInInches: false);

        Assert.True(details.ProfileGroups.IsEmpty);
        Assert.False(details.TreatAsSpecOps);
    }

    [Fact]
    public void Load_LieutenantOnly_SharesOnlyLieutenantOptionsAndHidesTheSkill()
    {
        Snapshot(101);
        _armyData.GetUnit(101, UnitId, Arg.Any<CancellationToken>()).Returns(new Unit
        {
            UnitId = UnitId,
            ProfileGroups = Groups(Option(1, "0", Lieutenant, Doctor), Option(2, "0", Camo), Option(3, "1", Lieutenant, Camo))
        });

        var details = Loader().Load(101, UnitId, isSpecOps: false, lieutenantOnly: true, showUnitsInInches: false);

        Assert.Equal(["Doctor"], details.CommonSkills);
    }

    private const int Camo = 1;
    private const int Doctor = 2;
    private const int Lieutenant = 3;

    private void Snapshot(int factionId) =>
        _armyData.GetFactionSnapshot(factionId, Arg.Any<CancellationToken>()).Returns(new Faction
        {
            FactionId = factionId,
            Filters = new Domain.Models.Army.Filters.ArmyFilterCatalog(new Dictionary<string, IReadOnlyList<Domain.Models.Army.Filters.ArmyFilterItem>>
            {
                [Domain.Models.Army.Filters.ArmyFilterSection.Skills] =
                [
                    new(Camo, "Camouflage", null, null),
                    new(Doctor, "Doctor", null, null),
                    new(Lieutenant, "Lieutenant", null, null)
                ]
            })
        });

    private static UnitProfileGroups Groups(params IEnumerable<ProfileOption> options) =>
        new([new ProfileGroup { Id = 1, Profiles = [new UnitProfile { Id = 1, Name = "Trooper" }], Options = options.ToList() }]);

    private static ProfileOption Option(int id, string swc, params IEnumerable<int> skills) => new()
    {
        Id = id,
        Swc = swc,
        Skills = skills.Select(ProfileEntry.ForId).ToList()
    };
}
