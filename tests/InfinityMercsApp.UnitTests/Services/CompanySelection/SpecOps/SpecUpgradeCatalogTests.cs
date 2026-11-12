using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.SpecOps;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.SpecOps;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.SpecOps;

public sealed class SpecUpgradeCatalogTests
{
    private const int PanOceania = 101;

    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();
    private readonly SpecUpgradeCatalog _catalog;

    public SpecUpgradeCatalogTests()
    {
        _armyData.GetFactionSnapshot(PanOceania, Arg.Any<CancellationToken>())
            .Returns(new Faction { FactionId = PanOceania, Filters = SpecialOperativesData.WithNames(ArmyFilterCatalog.Empty) });
        SpecialOperativesData.AddTo(_armyData, PanOceania);
        _catalog = new SpecUpgradeCatalog(_armyData);
    }

    [Fact]
    public void For_SpecOps_NamesEachUpgradeInTableOrder()
    {
        var upgrades = _catalog.For(PanOceania, SpecialOperativesData.SpecOpsId, showUnitsInInches: false);

        Assert.Equal(["+3 BTS", "+1 ARM, Dogged", "Spitfire", "Deployable Repeater"], upgrades.Select(upgrade => upgrade.Label));
        Assert.Equal(CaptainStatBonuses.None with { Arm = 1 }, upgrades[1].Bonuses);
        Assert.Equal(["Dogged"], upgrades[1].Skills);
        Assert.Equal(["Spitfire"], upgrades[2].Weapons);
        Assert.Equal(["Deployable Repeater"], upgrades[3].Equipment);
    }

    [Theory]
    [InlineData(false, "MOV 15-10, Dodge")]
    [InlineData(true, "MOV 6-4, Dodge")]
    public void For_TeamOpsMoveUpgrade_SetsTheMoveAndLeadsWithIt(bool inches, string label)
    {
        var move = _catalog.For(PanOceania, SpecialOperativesData.TeamOpsId, inches)[0];

        Assert.Equal(label, move.Label);
        Assert.Equal(15, move.MoveFirstCm);
        Assert.Equal(10, move.MoveSecondCm);
        Assert.Equal(CaptainStatBonuses.None, move.Bonuses);
    }

    [Fact]
    public void For_OrdinaryUnit_HasNoUpgrades()
    {
        _armyData.GetUnit(PanOceania, 5, Arg.Any<CancellationToken>()).Returns(new Unit { UnitId = 5 });

        Assert.Empty(_catalog.For(PanOceania, 5, showUnitsInInches: false));
    }

    [Theory]
    [InlineData(SpecialOperativeKind.SpecOps, SpecialOperativesData.SpecOpsId)]
    [InlineData(SpecialOperativeKind.TeamOps, SpecialOperativesData.TeamOpsId)]
    public void ResumesOf_Kind_ListsOnlyThatKind(SpecialOperativeKind kind, int unitId) =>
        Assert.Equal([unitId], _catalog.ResumesOf(PanOceania, kind).Select(resume => resume.UnitId));

    [Fact]
    public void SpecBotsFor_SpecOps_OffersEachSpecBotWithoutSwcWithItsCostAndWeapons()
    {
        var specBot = Assert.Single(_catalog.SpecBotsFor(PanOceania, SpecialOperativesData.SpecOpsId, showUnitsInInches: false));

        Assert.Equal("BLUEBERRY SPECBOT (6 pts): Flash Pulse", specBot.Label);
        Assert.Equal(6, specBot.Cost);
        Assert.Equal("Flash Pulse", specBot.Stats.RangedWeapons);
        Assert.Equal("Peripheral: BLUEBERRY SPECBOT", specBot.Stats.NameHeading);
    }
}
