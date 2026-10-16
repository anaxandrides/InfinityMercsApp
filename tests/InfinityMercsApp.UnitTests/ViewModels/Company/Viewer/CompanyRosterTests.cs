using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.ViewModels.Company.Viewer.Items;
using InfinityMercsApp.ViewModels.Components;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Viewer;

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
    public void ASeasonLeavesOutItsRetiredTroopers()
    {
        var company = new SavedCompanyFile { Entries = [Entry(1, "Fusilier", isLieutenant: true), Entry(2, "Bolt"), Entry(3, "Zhanshi")] };
        var season = new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 2 }] };

        Assert.Equal([1, 3], Create().UnitsOf(company, season).Select(unit => unit.EntryIndex));
        Assert.Equal(3, Create().UnitsOf(company).Count);
    }

    [Fact]
    public void TheLieutenantComesFirstThenSavedOrder()
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
    public void CustomNamesAndTheCaptainsNameAreUsed()
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
    public void TheCaptainsChoicesJoinTheLieutenantsLoadout()
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
    public void ADisabledCaptainAddsNothing()
    {
        var company = new SavedCompanyFile
        {
            ImprovedCaptainStats = new SavedImprovedCaptainStats { IsEnabled = false, SkillChoice1 = "Courage" },
            Entries = [Entry(1, "Fusilier", isLieutenant: true)]
        };

        Assert.Equal("-", Create().UnitsOf(company)[0].SavedSkills);
    }

    [Fact]
    public void LogosDefaultToThePackagedUnitLogo()
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
    public void TagCompanyUnitsKeepTheTagCompanyAsSource(string baseName, int sourceFactionId)
    {
        var entry = Entry(1, baseName, sourceFactionId: sourceFactionId);
        var unit = Create().UnitsOf(new SavedCompanyFile { Entries = [entry] })[0];

        Assert.Equal(CompanyRoster.TagCompanyFactionId, CompanyRoster.EffectiveSourceFactionId(entry));
        Assert.Equal(CompanyRoster.TagCompanyFactionId, unit.SourceFactionId);
        Assert.Equal(CompanyRoster.TagCompanyFallbackIconPath, unit.FallbackLogoPath);
    }

    [Fact]
    public void AMissingBaseNameFallsBackToItsPosition()
    {
        var entry = Entry(1, "");
        var unit = Create().UnitsOf(new SavedCompanyFile { Entries = [entry] })[0];

        Assert.Equal("Unit 1", unit.Name);
    }

    [Fact]
    public void TheProfileTidiesTheLieutenantsSkills()
    {
        var item = new CompanyViewerUnitListItem
        {
            Name = "Captain",
            IsLieutenant = true,
            Cost = 18,
            SavedSkills = string.Join(Environment.NewLine, "Lieutenant (+1 Order)", "Dodge", "Courage"),
            SavedRangedWeapons = "Combi Rifle",
            SavedPeripheralNameHeading = "Peripheral: Traktor Mul"
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
    public void ShowOnSetsStatsBonusesAndIcons()
    {
        var detail = new UnitDetailViewModel(_armyData);
        var item = new CompanyViewerUnitListItem
        {
            Name = "Captain",
            IsLieutenant = true,
            UnitCc = "14",
            UnitPh = "10",
            UnitVitality = "1",
            UnitTypeCode = "LI",
            SavedCharacteristics = "Irregular, Cube",
            SavedEquipment = "Killer Hacking Device"
        };
        var captain = new SavedImprovedCaptainStats { IsEnabled = true, CcBonus = 5, VitalityBonus = 1 };

        CompanyRoster.ShowOn(detail, item, CompanyRoster.ProfileOf(item), captain);

        Assert.Equal(("19", "10", "2"), (detail.Stats.Cc, detail.Stats.Ph, detail.Stats.Vitality));
        Assert.Single(detail.Profiles);
        Assert.Equal((false, true), (detail.Icons.Regular, detail.Icons.Irregular));
        Assert.Equal((true, false, true), (detail.Icons.Cube, detail.Icons.Cube2, detail.Icons.Hackable));
    }

    [Fact]
    public void HeaderColoursFallBackToTheDefaultsForAnUnknownFaction()
    {
        var colors = Create().HeaderColorsFor(0);

        Assert.Equal(InfinityMercsApp.ViewModels.Themes.CompanyHeaderColors.DefaultPrimary, colors.Primary);
    }

    [Fact]
    public void MovementInCentimetresIsKeptUnambiguous()
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
    public void TheProfileCanWriteDistancesInTheReadersUnit()
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
    public async Task ALogoComesFromTheDownloadThePackageOrTheFallback()
    {
        var packageFiles = Substitute.For<IAppPackageFiles>();
        packageFiles.ExistsAsync("svgcache/units/101-11.svg").Returns(true);
        var packaged = new CompanyViewerUnitListItem { PackagedLogoPath = "/SVGCache/units/101-11.svg" };
        var missing = new CompanyViewerUnitListItem { PackagedLogoPath = "SVGCache/units/9-9.svg", FallbackLogoPath = "fallback.svg" };

        Assert.Equal("svgcache/units/101-11.svg", await CompanyRoster.LogoSourceAsync(packaged, packageFiles));
        Assert.Equal("fallback.svg", await CompanyRoster.LogoSourceAsync(missing, packageFiles));
    }
}
