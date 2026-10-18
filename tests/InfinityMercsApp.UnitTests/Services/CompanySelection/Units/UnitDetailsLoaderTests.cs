using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Units;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Units;

public sealed class UnitDetailsLoaderTests
{
    private const int UnitId = 5;

    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();

    private UnitDetailsLoader Loader() => new(_armyData, Substitute.For<ISpecOpsProvider>());

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
}
