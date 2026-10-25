using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.SpecOps;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Captain;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Captain;

/// <summary>A captain's Spec-Ops options are labelled with their XP cost, names from the army's filters and their extras.</summary>
public sealed class CaptainOptionCatalogTests
{
    private const int Faction = 101;
    private const int TagCompany = 2003;

    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();
    private readonly ISpecOpsProvider _specOps = Substitute.For<ISpecOpsProvider>();
    private readonly CaptainOptionCatalog _catalog;

    public CaptainOptionCatalogTests() => _catalog = new CaptainOptionCatalog(_armyData, _specOps);

    [Fact]
    public void Load_NoFaction_IsEmpty() => Assert.True(_catalog.Load(0, showUnitsInInches: false, CancellationToken.None).IsEmpty);

    [Fact]
    public void Load_SpecOps_LabelsEachInEntryOrderOnce()
    {
        Filters(Faction,
            Section(ArmyFilterSection.Skills, new ArmyFilterItem(1, "Camouflage", null, null), new ArmyFilterItem(2, " Doctor ", null, null)),
            Section(ArmyFilterSection.Equip, new ArmyFilterItem(3, "MediKit", null, null)),
            Section(ArmyFilterSection.Weapons, new ArmyFilterItem(4, "Spitfire", null, null)));
        _specOps.GetSpecopsSkillsByFaction(Faction, Arg.Any<CancellationToken>()).Returns(
        [
            new SpecopsSkill { EntryOrder = 2, SkillId = 2, Exp = 5 },
            new SpecopsSkill { EntryOrder = 1, SkillId = 1, Exp = 10 },
            new SpecopsSkill { EntryOrder = 3, SkillId = 1, Exp = 10 }
        ]);
        _specOps.GetSpecopsEquipsByFaction(Faction, Arg.Any<CancellationToken>()).Returns([new SpecopsEquipment { EquipmentId = 3, Exp = 2 }]);
        _specOps.GetSpecopsWeaponsByFaction(Faction, Arg.Any<CancellationToken>()).Returns([new SpecopsWeapon { WeaponId = 4, Exp = 15, Extras = [9] }]);

        var options = _catalog.Load(Faction, showUnitsInInches: false, CancellationToken.None);

        Assert.Equal(["(10) - Camouflage", "(5) - Doctor"], options.Skills);
        Assert.Equal(["(2) - MediKit"], options.Equipment);
        Assert.Equal(["(15) - Spitfire"], options.Weapons);
    }

    [Fact]
    public void Load_UnnamedChoice_FallsBackToItsKindAndId()
    {
        Filters(Faction);
        _specOps.GetSpecopsSkillsByFaction(Faction, Arg.Any<CancellationToken>()).Returns([new SpecopsSkill { SkillId = 77, Exp = 3 }]);

        Assert.Equal(["(3) - Skill 77"], _catalog.Load(Faction, showUnitsInInches: false, CancellationToken.None).Skills);
    }

    [Fact]
    public void Load_ChoiceWithExtras_NamesThemAndConvertsDistances()
    {
        Filters(Faction,
            Section(ArmyFilterSection.Skills, new ArmyFilterItem(1, "Sixth Sense", null, null)),
            Section(ArmyFilterSection.Extras, new ArmyFilterItem(7, "+5cm", "DISTANCE", null), new ArmyFilterItem(8, "Burst", "TEXT", null)));
        _specOps.GetSpecopsSkillsByFaction(Faction, Arg.Any<CancellationToken>()).Returns([new SpecopsSkill { SkillId = 1, Exp = 4, Extras = [7, 8, 8, 99] }]);

        var skill = Assert.Single(_catalog.Load(Faction, showUnitsInInches: true, CancellationToken.None).Skills);

        Assert.Equal("(4) - Sixth Sense (+2\", Burst, 99)", skill);
    }

    [Fact]
    public void Load_OldExtraSection_IsStillRead()
    {
        Filters(Faction,
            Section(ArmyFilterSection.Equip, new ArmyFilterItem(3, "Mimetism", null, null)),
            Section("extra", new ArmyFilterItem(6, "-6", null, null), new ArmyFilterItem(5, " ", null, null)));
        _specOps.GetSpecopsEquipsByFaction(Faction, Arg.Any<CancellationToken>()).Returns([new SpecopsEquipment { EquipmentId = 3, Exp = 1, Extras = [6] }]);

        Assert.Equal(["(1) - Mimetism (-6)"], _catalog.Load(Faction, showUnitsInInches: false, CancellationToken.None).Equipment);
    }

    [Fact]
    public void Load_TagCompanyWithoutLieutenant_OffersItFirstForFree()
    {
        Filters(TagCompany, Section(ArmyFilterSection.Skills, new ArmyFilterItem(1, "Camouflage", null, null)));
        _specOps.GetSpecopsSkillsByFaction(TagCompany, Arg.Any<CancellationToken>()).Returns([new SpecopsSkill { SkillId = 1, Exp = 10 }]);

        Assert.Equal(["(0) - Lieutenant", "(10) - Camouflage"], _catalog.Load(TagCompany, showUnitsInInches: false, CancellationToken.None).Skills);
    }

    [Fact]
    public void Load_TagCompanyWithLieutenant_OffersItOnce()
    {
        Filters(TagCompany, Section(ArmyFilterSection.Skills, new ArmyFilterItem(1, "Lieutenant", null, null)));
        _specOps.GetSpecopsSkillsByFaction(TagCompany, Arg.Any<CancellationToken>()).Returns([new SpecopsSkill { SkillId = 1, Exp = 5 }]);

        Assert.Equal(["(5) - Lieutenant"], _catalog.Load(TagCompany, showUnitsInInches: false, CancellationToken.None).Skills);
    }

    [Fact]
    public void Load_NoSnapshot_LabelsByKindAndId()
    {
        _specOps.GetSpecopsWeaponsByFaction(Faction, Arg.Any<CancellationToken>()).Returns([new SpecopsWeapon { WeaponId = 4, Exp = 1 }]);

        Assert.Equal(["(1) - Weapon 4"], _catalog.Load(Faction, showUnitsInInches: false, CancellationToken.None).Weapons);
    }

    [Fact]
    public void Load_ProviderFails_IsEmpty()
    {
        _specOps.GetSpecopsSkillsByFaction(Faction, Arg.Any<CancellationToken>()).Returns(_ => throw new InvalidOperationException("Broken."));

        Assert.True(_catalog.Load(Faction, showUnitsInInches: false, CancellationToken.None).IsEmpty);
    }

    private void Filters(int factionId, params IEnumerable<KeyValuePair<string, IReadOnlyList<ArmyFilterItem>>> sections) =>
        _armyData.GetFactionSnapshot(factionId, Arg.Any<CancellationToken>()).Returns(new Faction
        {
            FactionId = factionId,
            Filters = new ArmyFilterCatalog(sections.ToDictionary(section => section.Key, section => section.Value))
        });

    private static KeyValuePair<string, IReadOnlyList<ArmyFilterItem>> Section(string name, params IEnumerable<ArmyFilterItem> items) =>
        new(name, items.ToList());
}
