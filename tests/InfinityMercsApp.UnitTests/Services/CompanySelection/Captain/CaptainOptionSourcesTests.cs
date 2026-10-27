using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.SpecOps;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.Dialogs;
using NSubstitute;
using ArmyFaction = InfinityMercsApp.Domain.Models.Army.Faction;
using MetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Captain;

/// <summary>Which faction's Spec-Ops a captain or TAG is offered, and what the popup calls it.</summary>
public sealed class CaptainOptionSourcesTests
{
    private const int PanOceania = 101;
    private const int MilitaryOrders = 102;
    private const int PanOceaniaReinforcements = 199;
    private const int Unlisted = 4242;

    private readonly ICaptainConfigurationDialog _dialog = Substitute.For<ICaptainConfigurationDialog>();
    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();
    private readonly ISpecOpsProvider _specOps = Substitute.For<ISpecOpsProvider>();
    private readonly CaptainSetup _setup;

    public CaptainOptionSourcesTests()
    {
        _setup = new CaptainSetup(_dialog, new CaptainOptionCatalog(_armyData, _specOps), _armyData);
        Metadata(PanOceania, PanOceania, "PanOceania");
        Metadata(MilitaryOrders, PanOceania, "Military Orders");
        Metadata(PanOceaniaReinforcements, PanOceaniaReinforcements, "PanOceania Reinforcements");
    }

    private static CaptainUnitPopupInfo Unit { get; } = new() { Name = "Trooper", Cost = 10, Statline = "-" };

    [Fact]
    public async Task ConfigureCaptainAsync_ParentHasNoSpecOps_UsesTheSectorialsOwn()
    {
        Skill(MilitaryOrders, "Courage");

        await _setup.ConfigureCaptainAsync(Unit, new CaptainFactions(MilitaryOrders, MilitaryOrders, []), isSpecOps: false, isLoneWolf: false, showUnitsInInches: false);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.OptionFactionId == MilitaryOrders && context.SkillOptions.Contains("(5) - Courage") && context.OptionFactionName == "Military Orders"));
    }

    [Fact]
    public async Task ConfigureCaptainAsync_ParentHasSpecOps_UsesTheParents()
    {
        Skill(PanOceania, "Doctor");

        await _setup.ConfigureCaptainAsync(Unit, new CaptainFactions(MilitaryOrders, null, []), isSpecOps: false, isLoneWolf: false, showUnitsInInches: false);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context => context.OptionFactionId == PanOceania));
    }

    [Fact]
    public async Task ConfigureCaptainAsync_FactionNobodyKnows_IsNamedByItsId()
    {
        await _setup.ConfigureCaptainAsync(Unit, new CaptainFactions(Unlisted, null, []), isSpecOps: false, isLoneWolf: false, showUnitsInInches: false);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context => context.OptionFactionName == $"Faction {Unlisted}"));
    }

    [Fact]
    public async Task ConfigureCaptainAsync_NoFactionAtAll_IsJustAFaction()
    {
        await _setup.ConfigureCaptainAsync(Unit, new CaptainFactions(0, null, []), isSpecOps: false, isLoneWolf: false, showUnitsInInches: false);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context => context.OptionFactionName == "Faction"));
    }

    [Fact]
    public async Task ConfigureTagSpecOpsAsync_Reinforcements_AddsItsVanillaFactionsOptionsWithoutBannedOnes()
    {
        Skill(PanOceania, "Infiltration");
        Skill(PanOceania, "Courage");

        await _setup.ConfigureTagSpecOpsAsync(
            Unit, new CaptainFactions(2003, 2003, [], PanOceaniaReinforcements), isLieutenant: true, baseExperience: 20, showUnitsInInches: false);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.OptionFactionId == PanOceania &&
            context.SkillOptions.SequenceEqual(new List<string> { "(5) - Courage" }) &&
            context.PopupTitle == "TAG Lieutenant Spec Ops" &&
            context.DefaultUnitCustomName == "Lieutenant" &&
            context.OptionFactionName == "PanOceania Reinforcements"));
    }

    [Fact]
    public async Task ConfigureTagSpecOpsAsync_SectorialAndParentBothOffer_TakesBothFromTheFirst()
    {
        Skill(MilitaryOrders, "Courage");
        Skill(PanOceania, "Doctor");

        await _setup.ConfigureTagSpecOpsAsync(
            new CaptainUnitPopupInfo { Name = "Trooper", Cost = 10, Statline = "-", CustomName = " Big Iron " }, new CaptainFactions(2003, 2003, [], MilitaryOrders), isLieutenant: false, baseExperience: -5, showUnitsInInches: false);

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.OptionFactionId == MilitaryOrders &&
            context.SkillOptions.Count == 2 &&
            context.BaseExperienceOverride == 0 &&
            context.PopupTitle == "TAG Spec Ops" &&
            context.DefaultUnitCustomName == "Big Iron"));
    }

    private void Metadata(int id, int parentId, string name) =>
        _armyData.GetMetadataFactionById(id, Arg.Any<CancellationToken>()).Returns(new MetadataFaction { Id = id, ParentId = parentId, Name = name });

    private readonly Dictionary<int, List<ArmyFilterItem>> _skillNames = [];
    private readonly Dictionary<int, List<SpecopsSkill>> _skills = [];

    private void Skill(int factionId, string name)
    {
        var id = _skillNames.Values.Sum(names => names.Count) + 1;
        var names = _skillNames.TryGetValue(factionId, out var known) ? known : _skillNames[factionId] = [];
        var skills = _skills.TryGetValue(factionId, out var offered) ? offered : _skills[factionId] = [];
        names.Add(new ArmyFilterItem(id, name, null, null));
        skills.Add(new SpecopsSkill { SkillId = id, Exp = 5 });
        _armyData.GetFactionSnapshot(factionId, Arg.Any<CancellationToken>()).Returns(new ArmyFaction
        {
            FactionId = factionId,
            Filters = new ArmyFilterCatalog(new Dictionary<string, IReadOnlyList<ArmyFilterItem>> { [ArmyFilterSection.Skills] = names.ToList() })
        });
        _specOps.GetSpecopsSkillsByFaction(factionId, Arg.Any<CancellationToken>()).Returns(skills.ToList());
    }
}
