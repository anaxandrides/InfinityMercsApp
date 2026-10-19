using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Season.Gear;
using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Presentation.Company.Viewer;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Company.Viewer.Items;
using InfinityMercsApp.ViewModels.Components;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Presentation.Company.Viewer;

public sealed class CompanyRosterTests
{
    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();

    private CompanyRoster Create() => new(_armyData);

    internal static SavedCompanyEntry Entry(
        int index,
        string baseName,
        bool isLieutenant = false,
        bool isPeripheral = false,
        string customName = "",
        int sourceFactionId = 101,
        IReadOnlyList<string>? weapons = null,
        IReadOnlyList<string>? skills = null,
        IReadOnlyList<string>? characteristics = null) => new()
    {
        EntryIndex = index,
        Name = baseName,
        BaseUnitName = baseName,
        CustomName = customName,
        UnitTypeCode = "LI",
        SourceFactionId = sourceFactionId,
        SourceUnitId = 10 + index,
        IsLieutenant = isLieutenant,
        CarriesCaptainUpgrades = isLieutenant,
        IsPeripheralUnit = isPeripheral,
        Cost = 20,
        ExperiencePoints = 0,
        CustomWeapons = [.. weapons ?? []],
        CustomSkills = [.. skills ?? []],
        CustomCharacteristics = [.. characteristics ?? []],
        CurrentCc = "14",
        CurrentPh = "10",
        CurrentVitaOrStr = "1"
    };

    [Fact]
    public void UnitsOf_BootyAndMetachemistryRolled_PutsResultsOnTheTrooper()
    {
        _armyData.GetWeaponNames().Returns(["DA CC Weapon"]);
        _armyData.GetSkillNames().Returns(["Regeneration"]);
        var entry = Entry(1, "Ghulam", skills: ["Booty", "Metachemistry"]);
        entry.RecruitRolls = [new RecruitRoll(RecruitRollTable.Booty, 7, "DA CC Weapon"), new RecruitRoll(RecruitRollTable.Metachemistry, 20, "+3 PH + Regeneration")];

        var unit = Create().UnitsOf(new SavedCompanyFile { Entries = [entry] })[0];

        Assert.Equal("DA CC Weapon", unit.SavedCcWeapons);
        Assert.Equal("13", unit.UnitPh);
        Assert.Contains("Regeneration", unit.SavedSkills);
        Assert.Equal(["Booty: DA CC Weapon (rolled 7)", "Metachemistry: +3 PH + Regeneration (rolled 20)"], unit.RollLines);
        Assert.Empty(unit.OwedRolls);
    }

    [Fact]
    public void UnitsOf_BootyNotRolled_OwesARoll()
    {
        var unit = Create().UnitsOf(new SavedCompanyFile { Entries = [Entry(1, "Ghulam", skills: ["Booty (ReRoll)"])] })[0];

        Assert.Equal([new OwedRoll(RecruitRollTable.Booty, CanReroll: true)], unit.OwedRolls);
    }

    // The armour's own roll goes with it to whoever wears it.
    [Fact]
    public void UnitsOf_RolledArmourWorn_GivesItsResultToTheWearer()
    {
        _armyData.GetSkillNames().Returns(["Regeneration"]);
        var season = new SeasonFile { UnitGear = new() { [1] = [new SeasonUnitGear { ItemName = "Chemtank [Regeneration]", Slot = GearSlot.Armor.Key() }] } };

        var unit = Create().UnitsOf(new SavedCompanyFile { Entries = [Entry(1, "Ghulam")] }, season)[0];

        Assert.Contains("Regeneration", unit.SavedSkills);
    }

    [Fact]
    public void UnitsOf_SeasonWithRetiredTroopers_LeavesThemOut()
    {
        var company = new SavedCompanyFile { Entries = [Entry(1, "Fusilier", isLieutenant: true), Entry(2, "Bolt"), Entry(3, "Zhanshi")] };
        var season = new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 2 }] };

        Assert.Equal([1, 3], Create().UnitsOf(company, season).Select(unit => unit.EntryIndex));
        Assert.Equal(3, Create().UnitsOf(company).Count);
    }

    [Fact]
    public void UnitsOf_Company_ListsLieutenantFirstThenSavedOrder()
    {
        var company = new SavedCompanyFile
        {
            Entries = [Entry(2, "Bravo"), Entry(1, "Alpha"), Entry(3, "Fusilier", isLieutenant: true)]
        };

        var units = Create().UnitsOf(company);

        Assert.Equal(["Captain", "Alpha", "Bravo"], units.Select(u => u.Name));
        Assert.Equal(["Lieutenant", "", ""], units.Select(u => u.Subtitle));
        Assert.Equal(SavedUnitText.CaptainIconPath, units[0].CaptainIconPackagedPath);
        Assert.Equal(string.Empty, units[1].CaptainIconPackagedPath);
    }

    [Fact]
    public void UnitsOf_CustomAndCaptainNames_UsesThem()
    {
        var company = new SavedCompanyFile
        {
            ImprovedCaptainStats = new SavedImprovedCaptainStats { CaptainName = " Major Lunah " },
            Entries = [Entry(1, "Fusilier", isLieutenant: true), Entry(2, "Bolt", customName: " Sparky ")]
        };

        var units = Create().UnitsOf(company);

        Assert.Equal(["Major Lunah", "Sparky"], units.Select(u => u.Name));
        Assert.Equal("Bolt", units[1].BaseUnitName);
    }

    [Fact]
    public void UnitsOf_CaptainChoices_JoinTheLieutenantsLoadout()
    {
        var company = new SavedCompanyFile
        {
            ImprovedCaptainStats = new SavedImprovedCaptainStats
            {
                IsEnabled = true,
                WeaponChoice1 = "(2) - Sniper Rifle",
                SkillChoice2 = "Courage",
                EquipmentChoice3 = "(4) - Multispectral Visor L1"
            },
            Entries = [Entry(1, "Fusilier", isLieutenant: true, weapons: ["Combi Rifle"]), Entry(2, "Bolt", weapons: ["Combi Rifle"])]
        };

        var units = Create().UnitsOf(company);

        Assert.Equal(string.Join(Environment.NewLine, "Combi Rifle", "Sniper Rifle"), units[0].SavedRangedWeapons);
        Assert.Equal("Courage", units[0].SavedSkills);
        Assert.Equal("Multispectral Visor L1", units[0].SavedEquipment);
        Assert.Equal("Combi Rifle", units[1].SavedRangedWeapons);
    }

    [Fact]
    public void UnitsOf_CaptainReplacedInDowntime_OldCaptainKeepsTheUpgrades()
    {
        var founder = new SavedCompanyEntry { EntryIndex = 1, BaseUnitName = "Fusilier", CarriesCaptainUpgrades = true, CustomWeapons = ["Combi Rifle"] };
        var captain = new SavedCompanyEntry { EntryIndex = 2, BaseUnitName = "Bolt", IsLieutenant = true, CustomWeapons = ["Combi Rifle"] };
        var company = new SavedCompanyFile
        {
            ImprovedCaptainStats = new SavedImprovedCaptainStats { IsEnabled = true, WeaponChoice1 = "(2) - Sniper Rifle" },
            Entries = [founder, captain]
        };

        var units = Create().UnitsOf(company);

        Assert.Equal(["Bolt", "Fusilier"], units.Select(unit => unit.BaseUnitName));
        Assert.Equal("Combi Rifle", units[0].SavedRangedWeapons);
        Assert.Equal(string.Join(Environment.NewLine, "Combi Rifle", "Sniper Rifle"), units[1].SavedRangedWeapons);
    }

    [Fact]
    public void UnitsOf_DisabledCaptain_AddsNothing()
    {
        var company = new SavedCompanyFile
        {
            ImprovedCaptainStats = new SavedImprovedCaptainStats { IsEnabled = false, SkillChoice1 = "Courage" },
            Entries = [Entry(1, "Fusilier", isLieutenant: true)]
        };

        Assert.Equal("-", Create().UnitsOf(company)[0].SavedSkills);
    }

    [Fact]
    public void UnitsOf_NoDownloadedLogo_UsesThePackagedUnitLogo()
    {
        var unit = Create().UnitsOf(new SavedCompanyFile { Entries = [Entry(1, "Bolt")] })[0];

        Assert.Equal("SVGCache/units/101-11.svg", unit.PackagedLogoPath);
        Assert.Null(unit.CachedLogoPath);
        Assert.Null(unit.FallbackLogoPath);
    }

    [Theory]
    [InlineData("Turtlemek", 101)]
    [InlineData("Repurposed Mining Equipment", 101)]
    [InlineData("Anything", CompanyRoster.TagCompanyFactionId)]
    public void UnitsOf_TagCompanyUnit_KeepsTheTagCompanyAsSource(string baseName, int sourceFactionId)
    {
        var entry = Entry(1, baseName, sourceFactionId: sourceFactionId);
        var unit = Create().UnitsOf(new SavedCompanyFile { Entries = [entry] })[0];

        Assert.Equal(CompanyRoster.TagCompanyFactionId, SeasonTroopers.SourceFactionId(entry));
        Assert.Equal(CompanyRoster.TagCompanyFactionId, unit.SourceFactionId);
        Assert.Equal(CompanyRoster.TagCompanyFallbackIconPath, unit.FallbackLogoPath);
    }

    [Fact]
    public void UnitsOf_MissingBaseName_NamesItByPosition()
    {
        var entry = Entry(1, "");
        var unit = Create().UnitsOf(new SavedCompanyFile { Entries = [entry] })[0];

        Assert.Equal("Unit 1", unit.Name);
    }

    [Fact]
    public void ProfileOf_Lieutenant_TidiesItsSkills()
    {
        var item = new CompanyViewerUnitListItem
        {
            Name = "Captain",
            IsLieutenant = true,
            CarriesCaptainUpgrades = true,
            Cost = 18,
            SavedSkills = string.Join(Environment.NewLine, "Lieutenant (+1 Order)", "Dodge", "Courage"),
            SavedRangedWeapons = "Combi Rifle"
        };

        var profile = CompanyRoster.ProfileOf(item);

        Assert.Equal("Combi Rifle", profile.RangedWeapons);
        Assert.EndsWith("Dodge", profile.UniqueSkills);
        Assert.Contains("Courage", profile.UniqueSkills);
        Assert.Equal("18", profile.Cost);
        Assert.True(profile.IsLieutenant);
        Assert.Equal(2, CompanyRoster.LieutenantIconCount(profile));
    }

    [Fact]
    public void ShowOn_SavedTrooper_SetsItsStatsAndIcons()
    {
        var detail = new UnitDetailViewModel(_armyData);
        var item = new CompanyViewerUnitListItem
        {
            Name = "Captain",
            IsLieutenant = true,
            CarriesCaptainUpgrades = true,
            UnitCc = "14",
            UnitPh = "10",
            UnitVitality = "1",
            UnitTypeCode = "LI",
            SavedCharacteristics = "Irregular, Cube",
            SavedEquipment = "Killer Hacking Device"
        };

        CompanyRoster.ShowOn(detail, item, CompanyRoster.ProfileOf(item));

        // A lieutenant's saved stats already hold the captain's bonuses, so they aren't added again.
        Assert.Equal(("14", "10", "1"), (detail.Stats.Cc, detail.Stats.Ph, detail.Stats.Vitality));
        Assert.Single(detail.Profiles);
        Assert.Equal((false, true), (detail.Icons.Regular, detail.Icons.Irregular));
        Assert.Equal((true, false, true), (detail.Icons.Cube, detail.Icons.Cube2, detail.Icons.Hackable));
    }

    [Fact]
    public void HeaderColorsFor_UnknownFaction_ReturnsTheDefaults()
    {
        var colors = Create().HeaderColorsFor(0);

        Assert.Equal(InfinityMercsApp.Presentation.Themes.CompanyHeaderColors.DefaultPrimary, colors.Primary);
    }

    [Fact]
    public void UnitsOf_MovementInCentimetres_KeepsItUnambiguous()
    {
        var entries = new List<SavedCompanyEntry>
        {
            new() { EntryIndex = 1, BaseUnitName = "Bolt", CurrentMov = "4-4", CurrentMoveFirstCm = 10, CurrentMoveSecondCm = 5 },
            new() { EntryIndex = 2, BaseUnitName = "Zond", CurrentMov = "6-2" }
        };

        var units = Create().UnitsOf(new SavedCompanyFile { Entries = entries });

        Assert.Equal(["10cm-5cm", "6-2"], units.Select(u => u.UnitMov));
    }

    [Fact]
    public void ProfileOf_ShowUnitsInInches_WritesDistancesInThatUnit()
    {
        var item = new CompanyViewerUnitListItem
        {
            Name = "Bolt",
            SavedEquipment = "Jammer (10cm)",
            SavedSkills = "Super-Jump (+5cm)",
            SavedCharacteristics = "Range 20cm"
        };

        var inInches = CompanyRoster.ProfileOf(item, showUnitsInInches: true);
        var asSaved = CompanyRoster.ProfileOf(item);

        Assert.Equal(("Jammer (4\")", "Super-Jump (+2\")", "Range 8\""), (inInches.UniqueEquipment, inInches.UniqueSkills, inInches.Characteristics));
        Assert.Equal("Jammer (10cm)", asSaved.UniqueEquipment);
    }

    [Fact]
    public async Task LogoSourceAsync_AnyLogo_UsesDownloadThenPackageThenFallback()
    {
        var packageFiles = Substitute.For<IAppPackageFiles>();
        packageFiles.ExistsAsync("svgcache/units/101-11.svg").Returns(true);
        var packaged = new CompanyViewerUnitListItem { PackagedLogoPath = "/SVGCache/units/101-11.svg" };
        var missing = new CompanyViewerUnitListItem { PackagedLogoPath = "SVGCache/units/9-9.svg", FallbackLogoPath = "fallback.svg" };

        Assert.Equal("svgcache/units/101-11.svg", await CompanyRoster.LogoSourceAsync(packaged, packageFiles));
        Assert.Equal("fallback.svg", await CompanyRoster.LogoSourceAsync(missing, packageFiles));
    }
}
