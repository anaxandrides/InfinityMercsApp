using System.Text.Json;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Inventory;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.UnitTests.Season.Armor;
using InfinityMercsApp.UnitTests.Season.Inventory;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Season.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Presentation.Season;

public sealed class DeploymentRosterTests : IDisposable
{
    private const string SeasonPath = "/seasons/s.json";

    private readonly TempRecords _records = new();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly IStoreProvider _stores = Substitute.For<IStoreProvider>();
    private readonly DeploymentRoster _roster;

    public DeploymentRosterTests() => _roster = new DeploymentRoster(Army101Data.Create(), _seasonFiles, new StoreCatalog(_stores));

    public void Dispose() => _records.Dispose();

    private const string Company = """
        {
          "improvedCaptainStats": { "isEnabled": true, "weaponChoice1": "(+2) - Pistol", "skillChoice1": "-", "equipmentChoice1": "Albedo" },
          "entries": [
            { "entryIndex": 2, "name": "Fusilier (Hacker)", "baseUnitName": "Fusilier (Hacker)", "sourceFactionId": 101, "cost": 10, "experiencePoints": 5,
              "CurrentWeaponCodes": [ { "id": 33 }, { "id": 6 } ], "CurrentCharacteristicCodes": [ { "id": 4 } ],
              "Current BS": "12", "Current VITA/STR": "1" },
            { "entryIndex": 1, "name": "Trooper", "customName": " Kestrel ", "isLieutenant": true, "carriesCaptainUpgrades": true, "sourceFactionId": 101,
              "CurrentSkillCodes": [ { "id": 86 } ], "CurrentWeaponCodes": [ { "id": 33, "extra": [ 298 ] } ], "Current VITA/STR": "2" },
            { "entryIndex": 3, "isPeripheralUnit": true, "sourceFactionId": 101, "Custom Characteristics": [ "Peripheral (Servant)" ] },
            { "entryIndex": 4, "name": "Tag", "unitTypeCode": "TAG", "sourceFactionId": 2003 }
          ]
        }
        """;

    private Task<IReadOnlyList<DeploymentUnitItem>> LoadAsync(IReadOnlySet<int>? deployed = null) =>
        _roster.LoadAsync(_records.WriteCompany("c.json", Company), SeasonPath, showUnitsInInches: true, deployed);

    [Fact]
    public async Task LoadAsync_RetiredTroopers_LeavesThemOut()
    {
        _seasonFiles.LoadAsync(SeasonPath).Returns(new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 2 }] });

        Assert.Equal([1, 4], (await LoadAsync()).Select(u => u.EntryIndex));
    }

    [Fact]
    public async Task LoadAsync_Company_ListsLieutenantFirstWithoutServants()
    {
        var units = await LoadAsync();

        Assert.Equal([1, 2, 4], units.Select(u => u.EntryIndex));
        Assert.Equal("Kestrel", units[0].Name);
        Assert.Equal("Lieutenant", units[0].Subtitle);
        Assert.True(units[0].IsChecked);
        Assert.False(units[1].IsChecked);
    }

    [Fact]
    public async Task LoadAsync_DeployedEntriesGiven_ListsOnlyThose()
    {
        var units = await LoadAsync(new HashSet<int> { 2 });

        Assert.Equal([2], units.Select(u => u.EntryIndex));
    }

    [Fact]
    public async Task LoadAsync_SavedCodes_ResolvesThemFromArmyData()
    {
        var fusilier = (await LoadAsync())[1];

        Assert.Equal("Fusilier", fusilier.BaseUnitDisplayName);
        Assert.Equal("Combi Rifle", fusilier.RangedWeapons);
        Assert.Equal("AP CC Weapon", fusilier.MeleeWeapons);
        Assert.True(fusilier.IsIrregular);
        Assert.False(fusilier.IsImpetuous);
        Assert.Equal(15, fusilier.Renown);
        Assert.Equal("12", fusilier.UnitBs);
        Assert.Equal("SVGCache/units/101-0.svg", fusilier.PackagedLogoPath);
    }

    [Fact]
    public async Task LoadAsync_Lieutenant_GetsCaptainUpgradesAndCodeExtras()
    {
        var lieutenant = (await LoadAsync())[0];

        Assert.Equal($"Combi Rifle (+1){Environment.NewLine}Pistol", lieutenant.RangedWeapons);
        Assert.Contains("Albedo", lieutenant.Equipment);
        Assert.True(lieutenant.HasNwi);
        Assert.Equal(2, lieutenant.StartingVitality);
        Assert.Equal("SVGCache/NonCBIcons/noun-captain-8115950.svg", lieutenant.CaptainIconPackagedPath);
    }

    [Fact]
    public async Task LoadAsync_TagCompanyTag_TakesTwoSlots()
    {
        var tag = (await LoadAsync())[2];

        Assert.True(tag.IsTagFromTagCompany);
        Assert.Equal(2, tag.SlotCost);
        Assert.Equal(Stat.Str, tag.VitalityStat);
    }

    [Fact]
    public async Task LoadAsync_Trooper_ListsEveryStatInOrder()
    {
        var tag = (await LoadAsync())[2];

        Assert.Equal<Stat>(
            [Stat.Mov, Stat.Cc, Stat.Bs, Stat.Ph, Stat.Wip, Stat.Arm, Stat.Bts, Stat.Str, Stat.S],
            tag.Statline.Select(v => v.Stat));
        Assert.Equal("STR", tag.VitalityHeader);
    }

    [Fact]
    public async Task LoadAsync_SeasonGear_AppliesIt()
    {
        _seasonFiles.LoadAsync(SeasonPath).Returns(new SeasonFile
        {
            UnitGear = new()
            {
                [2] =
                [
                    new SeasonUnitGear { Slot = "Augments", ItemName = "BS = 14" },
                    new SeasonUnitGear { Slot = "Augments", ItemName = "Neurocinetics" },
                    new SeasonUnitGear { Slot = "Roles", ItemName = "Forward Observer" },
                    new SeasonUnitGear { Slot = "Sidearm", ItemName = "Knife" },
                    new SeasonUnitGear { Slot = "Primary", ItemName = "Spitfire" }
                ]
            }
        });

        var fusilier = (await LoadAsync())[1];

        Assert.Equal("14", fusilier.UnitBs);
        Assert.Equal($"Neurocinetics{Environment.NewLine}Forward Observer", fusilier.Skills);
        Assert.Equal($"Combi Rifle{Environment.NewLine}Spitfire", fusilier.RangedWeapons);
        Assert.Equal($"AP CC Weapon{Environment.NewLine}Knife", fusilier.MeleeWeapons);
    }

    [Fact]
    public void CarriedGearOf_SeasonGear_KeepsRolesAugmentsBootyAndNeurocinetics()
    {
        List<SeasonUnitGear> gear =
        [
            new() { Slot = "Augments", ItemName = "BS = 14" },
            new() { Slot = "Augments", ItemName = "Mimetism (-3)" },
            new() { Slot = "Roles", ItemName = "Forward Observer" },
            new() { Slot = "Primary", ItemName = "Spitfire" },
            new() { Slot = "Armor", ItemName = "Light Armour (LI)" }
        ];

        var carried = DeploymentRoster.CarriedGearOf(
            gear, InventoryCatalog.Empty, ["Courage", "Neurocinetics (B2 in Active)"], ["Medikit"]);

        Assert.Equal(["Mimetism (-3)", "Forward Observer", "Neurocinetics (B2 in Active)"], carried.Skills);
        Assert.Equal(["Medikit"], carried.Equipment);
        Assert.Equal([new StatOverride(Stat.Bs, 14)], carried.Overrides);
    }

    [Fact]
    public async Task LoadAsync_MissingCompany_ReturnsNoUnits()
    {
        Assert.Empty(await _roster.LoadAsync("/nowhere/c.json", null, true));
    }

    [Theory]
    [InlineData("(+2) - Pistol", "Pistol")]
    [InlineData("-", null)]
    [InlineData("  ", null)]
    public void CaptainUpgradesChoices_Choice_DropsItsCost(string choice, string? expected) =>
        Assert.Equal(expected is null ? [] : [expected], DeploymentRoster.CaptainUpgrades.Choices(choice));

    [Fact]
    public void SourceFactionId_RepurposedMiningTag_ReturnsTheTagCompany()
    {
        var entry = JsonSerializer.Deserialize<InfinityMercsApp.Services.CompanySelection.Models.Saved.SavedCompanyEntry>(
            """{"sourceFactionId":101,"baseUnitName":"Turtlemek"}""",
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(entry);

        Assert.Equal(DeploymentRoster.TagCompanyFactionId, SeasonTroopers.SourceFactionId(entry));
    }

    [Fact]
    public async Task LoadAsync_SeasonProgress_ChangesTheTrooper()
    {
        var season = new SeasonFile
        {
            Rounds = [new SeasonRound { MissionResults = new SeasonMissionResult { UnitResults = [new() { EntryIndex = 2, UnitName = "Fusilier (Hacker)", XpGained = 12 }] } }]
        };
        season.TrooperFor(2).Injuries.Add(Injury.Arms);
        season.TrooperFor(2).CharacteristicsLost.Add("Irregular");
        _seasonFiles.LoadAsync(SeasonPath).Returns(season);

        var fusilier = (await LoadAsync())[1];

        Assert.Equal("11", fusilier.UnitBs);
        Assert.False(fusilier.IsIrregular);
        Assert.Contains("Courage", fusilier.Skills);
        Assert.Equal(10 + 17, fusilier.Renown);
        Assert.EndsWith("noun-2-stars.svg", fusilier.ExperienceIconPackagedPath);
    }

    [Fact]
    public async Task LoadAsync_SeasonGear_CountsItsBaseCostInRenown()
    {
        _stores.GetAllStoreNames().Returns(["Number One"]);
        _stores.GetStoreByNameAsync("Number One", Arg.Any<CancellationToken>()).Returns(InventoryTestStores.NumberOne);
        var season = new SeasonFile();
        season.UnitGear[2] =
        [
            new SeasonUnitGear { ItemName = "Spitfire", Slot = GearSlot.Primary.Key() },
            new SeasonUnitGear { ItemName = "Light Armour (LI)", Slot = GearSlot.Armor.Key() },
            new SeasonUnitGear { ItemName = "Unknown Gadget", Slot = GearSlot.Accessories.Key() }
        ];
        _seasonFiles.LoadAsync(SeasonPath).Returns(season);

        var fusilier = (await LoadAsync())[1];

        // Cost 10 and 5 XP, then the Spitfire's 20 CR and the armour's 10; gear no market sells adds nothing.
        Assert.Equal(15 + 20 + 10, fusilier.Renown);
    }

    [Fact]
    public async Task LoadAsync_ArmourWorn_DeploysInIt()
    {
        var superheavy = PackagedArmor.Named("Superheavy Armor");
        _stores.GetAllStoreNames().Returns(["Number One"]);
        _stores.GetStoreByNameAsync("Number One", Arg.Any<CancellationToken>())
            .Returns(new Store { Name = "Number One", AssociatedFactions = ["PanOceania"], TroopTypes = [superheavy] });
        var season = new SeasonFile();
        season.UnitGear[2] = [new SeasonUnitGear { ItemName = "Superheavy Armor (HI)", Slot = GearSlot.Armor.Key() }];
        _seasonFiles.LoadAsync(SeasonPath).Returns(season);

        var fusilier = (await LoadAsync())[1];

        Assert.Equal(("5", "6", "2", "5"), (fusilier.UnitArm, fusilier.UnitBts, fusilier.UnitVitality, fusilier.UnitS));
        Assert.Equal(2, fusilier.StartingVitality);
        Assert.True(fusilier.HasNwi);
        Assert.Contains("Immunity (Shock)", fusilier.Skills);
    }

    // The Wetsuit's own Booty roll goes with it, and comes after the armour and everything else.
    [Fact]
    public async Task LoadAsync_RolledArmourWorn_PutsItsResultOnTheWearer()
    {
        var data = Army101Data.Create();
        data.GetWeaponNames().Returns(["HMG"]);
        var roster = new DeploymentRoster(data, _seasonFiles, new StoreCatalog(_stores));
        var season = new SeasonFile();
        season.UnitGear[2] = [new SeasonUnitGear { ItemName = "Irmandihno Wetsuit (LI) [HMG]", Slot = GearSlot.Armor.Key() }];
        _seasonFiles.LoadAsync(SeasonPath).Returns(season);

        var fusilier = (await roster.LoadAsync(_records.WriteCompany("c.json", Company), SeasonPath, showUnitsInInches: true))[1];

        Assert.Contains("HMG", fusilier.RangedWeapons);
    }

    // A drummer and its Synchronized drumbot, and a Controller augment peripheral no one controls.
    private const string Drummers = """
        {
          "entries": [
            { "entryIndex": 1, "name": "Drummer", "isLieutenant": true, "sourceFactionId": 101, "Custom Characteristics": [ "Regular" ] },
            { "entryIndex": 2, "name": "Drumbot", "isPeripheralUnit": true, "parentEntryIndex": 1, "sourceFactionId": 101,
              "Custom Skills": [ "Peripheral (Synchronized)" ], "Custom Characteristics": [ "Regular" ] },
            { "entryIndex": 3, "name": "Chaksa", "isPeripheralUnit": true, "fromControllerAugment": true, "sourceFactionId": 101 }
          ]
        }
        """;

    [Fact]
    public async Task LoadAsync_NonServantPeripheral_DeploysAsItsOwnTrooper()
    {
        var units = await _roster.LoadAsync(_records.WriteCompany("d.json", Drummers), SeasonPath, showUnitsInInches: true);

        Assert.Equal([1, 2], units.Select(unit => unit.EntryIndex));
        Assert.True(units[1].IsIrregular);
        Assert.False(units[0].IsIrregular);
        Assert.Contains(PeripheralKinds.ControllerSkill, units[0].Skills);
    }

    // A Cohesive company's core fireteam has room for one Kamau: the lieutenant fits nowhere, the second Kamau is left out.
    private const string Cohesive = """
        {
          "companyType": "Cohesive Company", "coreFireteam": "Kamau Core",
          "coreFireteamSlots": [ { "name": "Kamau", "max": "1" } ],
          "entries": [
            { "entryIndex": 1, "name": "Drummer", "isLieutenant": true, "sourceFactionId": 101, "Custom Characteristics": [ "Regular" ] },
            { "entryIndex": 2, "name": "Kamau", "sourceFactionId": 101, "Custom Characteristics": [ "Irregular" ] },
            { "entryIndex": 3, "name": "Kamau", "sourceFactionId": 101, "Custom Characteristics": [ "Regular" ] }
          ]
        }
        """;

    [Fact]
    public async Task LoadAsync_CohesiveCompany_CoreFireteamDecidesTheOrderType()
    {
        var units = await _roster.LoadAsync(_records.WriteCompany("cohesive.json", Cohesive), SeasonPath, showUnitsInInches: true);

        Assert.Equal([true, false, true], units.Select(unit => unit.IsIrregular));
        Assert.Equal([false, true, false], units.Select(unit => unit.IsInCoreFireteam));
    }

    [Fact]
    public async Task LoadAsync_CohesiveTrooperRetired_FreesItsFireteamPlace()
    {
        _seasonFiles.LoadAsync(SeasonPath).Returns(new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 2 }] });

        var units = await _roster.LoadAsync(_records.WriteCompany("cohesive.json", Cohesive), SeasonPath, showUnitsInInches: true);

        Assert.Equal([true, false], units.Select(unit => unit.IsIrregular));
        Assert.Equal(3, units[1].EntryIndex);
    }

    [Fact]
    public async Task LoadAsync_ItemInExtraSlot_WorksAsItsOwnKind()
    {
        _stores.GetAllStoreNames().Returns(["Number One"]);
        _stores.GetStoreByNameAsync("Number One", Arg.Any<CancellationToken>()).Returns(InventoryTestStores.NumberOne);
        var season = new SeasonFile();
        season.UnitGear[2] = [new SeasonUnitGear { ItemName = "Spitfire", Slot = GearSlot.Extra.Key() }];
        _seasonFiles.LoadAsync(SeasonPath).Returns(season);

        var fusilier = (await LoadAsync())[1];

        Assert.Contains("Spitfire", fusilier.RangedWeapons);
    }
}
