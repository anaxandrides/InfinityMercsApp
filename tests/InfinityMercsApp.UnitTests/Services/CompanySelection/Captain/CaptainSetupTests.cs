using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Captain;

public sealed class CaptainSetupTests
{
    private readonly ICaptainConfigurationDialog _dialog = Substitute.For<ICaptainConfigurationDialog>();
    private readonly CaptainSetup _setup;

    public CaptainSetupTests()
    {
        var armyData = Army101Data.Create();
        _setup = new CaptainSetup(_dialog, new CaptainOptionCatalog(armyData, Substitute.For<ISpecOpsProvider>()), armyData);
    }

    private static CaptainFactions PanOceania { get; } = new(Army101Data.PanOceania, Army101Data.PanOceania, []);

    private static CaptainUnitPopupInfo Unit(string name, int cost) => new()
    {
        Name = name,
        Cost = cost,
        Statline = "CC 13 | BS 12",
        RangedWeapons = "Combi Rifle",
        CcWeapons = "Pistol",
        Skills = "Lieutenant"
    };

    [Fact]
    public async Task ConfigureCaptainAsync_NotSpecOps_UsesTheMercenariesChart()
    {
        await _setup.ConfigureCaptainAsync(Unit("Fusilier", 10), PanOceania, tookUpgrades: false, isLoneWolf: false, showUnitsInInches: true);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context => context.StatChart == CaptainStatChart.Mercenaries));
    }

    [Fact]
    public async Task ConfigureCaptainAsync_SpecOps_OffersNoUpgradesOrExperience()
    {
        await _setup.ConfigureCaptainAsync(Unit("Indigo", 20), PanOceania, tookUpgrades: true, isLoneWolf: false, showUnitsInInches: true);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.BaseExperienceOverride == 0 &&
            context.WeaponOptions.Count == 0 &&
            context.SkillOptions.Count == 0 &&
            context.EquipmentOptions.Count == 0 &&
            context.LockedStats.Contains(Stat.Cc) &&
            context.LockedStats.Contains(Stat.Vita)));
    }

    [Fact]
    public async Task ConfigureCaptainAsync_Captain_ShowsThePopupAndReturnsTheChoice()
    {
        var chosen = new SavedImprovedCaptainStats { IsEnabled = true, CaptainName = "Kestrel" };
        _dialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>()).Returns(chosen);

        var result = await _setup.ConfigureCaptainAsync(Unit("Fusilier", 10), PanOceania, tookUpgrades: false, isLoneWolf: false, showUnitsInInches: true);

        Assert.Same(chosen, result);
        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.Unit.Name == "Fusilier" && context.Unit.Cost == 10));
    }

    [Theory]
    [InlineData(10, 28)]
    [InlineData(35, 10)]
    public async Task ConfigureCaptainAsync_LoneWolf_GetsTenMoreSpecOpsXpWhateverItCosts(int cost, int experience)
    {
        await _setup.ConfigureCaptainAsync(Unit("Fusilier", cost), PanOceania, tookUpgrades: false, isLoneWolf: true, showUnitsInInches: true);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.BaseExperienceOverride == experience &&
            context.PopupTitle == "Lone Wolf Configuration" &&
            context.DefaultUnitCustomName == "Lone Wolf"));
    }

    [Fact]
    public async Task ConfigureCaptainAsync_Captain_UsesTheUsualExperience()
    {
        await _setup.ConfigureCaptainAsync(Unit("Fusilier", 10), PanOceania, tookUpgrades: false, isLoneWolf: false, showUnitsInInches: true);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context => context.BaseExperienceOverride == null));
    }

    [Fact]
    public async Task ConfigureTagSpecOpsAsync_Tag_ShowsTheTagPopup()
    {
        await _setup.ConfigureTagSpecOpsAsync(Unit("Squalo", 60), PanOceania, isLieutenant: false, baseExperience: 20, showUnitsInInches: true);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.PopupTitle == "TAG Spec Ops" &&
            context.ConfirmButtonText == "Finalize Tag" &&
            context.DefaultUnitCustomName == "TAG" &&
            context.BaseExperienceOverride == 20 &&
            context.StatChart == CaptainStatChart.SpecOps));
    }

    [Fact]
    public async Task ConfigureCaptainAsync_UnknownFaction_NamesItByIdWhenNothingKnowsIt()
    {
        await _setup.ConfigureCaptainAsync(Unit("Fusilier", 10), new CaptainFactions(4242, null, []), tookUpgrades: false, isLoneWolf: false, showUnitsInInches: true);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context => context.OptionFactionName == "Faction 4242"));
    }
}
