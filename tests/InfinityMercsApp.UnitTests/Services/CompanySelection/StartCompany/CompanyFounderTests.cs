using System.Text.Json;
using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Fireteams;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using NSubstitute;
using InfinityMercsApp.Domain.Validation.Company;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.StartCompany;

public sealed class CompanyFounderTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly ICaptainConfigurationDialog _dialog = Substitute.For<ICaptainConfigurationDialog>();
    private readonly CompanyFounder _founder;

    private static readonly ArmyFactionSelectionItem PanOceania = new()
    {
        Id = Army101Data.PanOceania,
        ParentId = Army101Data.PanOceania,
        Name = "PanOceania"
    };

    public CompanyFounderTests()
    {
        var armyData = Army101Data.Create();
        var captains = new CaptainSetup(_dialog, new CaptainOptionCatalog(armyData, Substitute.For<ISpecOpsProvider>()), armyData);
        _founder = new CompanyFounder(captains, new SavedEntryWriter(armyData), _records.Paths, new CompanyFileStore(Substitute.For<ISyncOutbox>()));
        _dialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>())
            .Returns(new SavedImprovedCaptainStats { IsEnabled = true, CaptainName = "Kestrel" });
    }

    public void Dispose() => _records.Dispose();

    private static MercsCompanyEntry Trooper(string name, bool isLieutenant = false, string unitTypeCode = "LI") => new()
    {
        Name = name,
        BaseUnitName = name,
        Subtitle = "MOV 10-10 | CC 13 | BS 12 | PH 10 | WIP 12 | ARM 1 | BTS 0 | VITA 1 | S 2",
        CostValue = 10,
        IsLieutenant = isLieutenant,
        UnitTypeCode = unitTypeCode,
        SourceFactionId = Army101Data.PanOceania,
        SourceUnitId = Army101Data.UnitNamed("FUSILIERS").Id
    };

    private static CompanyFoundingRequest<ArmyFactionSelectionItem, MercsCompanyEntry> Request(
        string type, params IEnumerable<MercsCompanyEntry> troopers) => new()
    {
        CompanyName = "Blue Lions",
        CompanyType = type,
        MercsCompanyEntries = troopers.ToList(),
        ShowRightSelectionBox = false,
        LeftSlotFaction = PanOceania,
        RightSlotFaction = null,
        Factions = [PanOceania],
        ShowUnitsInInches = false,
        SelectedStartSeasonPoints = "75",
        SeasonPointsCapText = "20"
    };

    [Fact]
    public void NextCompanyFileName_Name_SlugsItAndTakesTheNextIndex()
    {
        var dir = _records.Paths.CompaniesDirectory;

        Assert.Equal("iron-wolves-0001.json", CompanyFounder.NextCompanyFileName(dir, "  Iron Wolves! "));
        Assert.Equal("company-0001.json", CompanyFounder.NextCompanyFileName(dir, "???"));
    }

    [Fact]
    public async Task FoundAsync_CaptainChosen_WritesTheCompaniesFolderFile()
    {
        var result = await _founder.FoundAsync(Request("Standard Company", Trooper("Alpha", isLieutenant: true), Trooper("Bravo")));

        Assert.Equal(FoundingOutcome.Founded, result.Outcome);
        Assert.Equal(Path.Combine(_records.Paths.CompaniesDirectory, "blue-lions-0001.json"), result.FilePath);
        var json = await File.ReadAllTextAsync(result.FilePath);
        Assert.Contains("\"Blue Lions\"", json);
        Assert.Contains("\"Kestrel\"", json);
    }

    [Fact]
    public async Task FoundAsync_Cohesive_SavesTheCoreFireteamAndItsPlaces()
    {
        var request = Request("Cohesive Company", Trooper("Alpha", isLieutenant: true), Trooper("Bravo"));

        var result = await _founder.FoundAsync(new CompanyFoundingRequest<ArmyFactionSelectionItem, MercsCompanyEntry>
        {
            CompanyName = request.CompanyName,
            CompanyType = request.CompanyType,
            MercsCompanyEntries = request.MercsCompanyEntries,
            ShowRightSelectionBox = request.ShowRightSelectionBox,
            LeftSlotFaction = request.LeftSlotFaction,
            RightSlotFaction = request.RightSlotFaction,
            Factions = request.Factions,
            ShowUnitsInInches = request.ShowUnitsInInches,
            SelectedStartSeasonPoints = request.SelectedStartSeasonPoints,
            SeasonPointsCapText = request.SeasonPointsCapText,
            CoreFireteam = "Fusilier Core",
            CoreFireteamSlots = [new FireteamSlot("Fusilier", 7, 101, "*")]
        });

        var company = JsonSerializer.Deserialize<SavedCompanyFile>(
            await File.ReadAllTextAsync(result.FilePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(company);
        Assert.Equal("Fusilier Core", company.CoreFireteam);
        Assert.Equal([new FireteamSlot("Fusilier", 7, 101, "*")], company.CoreFireteamSlots);
    }

    // Armour keeps the captain's ARM bonus on top, and the Team tab lists its buys, from the saved upgrades.
    [Fact]
    public async Task FoundAsync_CaptainUpgraded_SavesTheUpgradesAndTheirStats()
    {
        _dialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>())
            .Returns(new SavedImprovedCaptainStats { IsEnabled = true, CaptainName = "Kestrel", ArmBonus = 1 });

        var result = await _founder.FoundAsync(Request("Standard Company", Trooper("Alpha", isLieutenant: true), Trooper("Bravo")));

        var company = await new CompanyFileStore(Substitute.For<ISyncOutbox>()).LoadAsync(result.FilePath);
        Assert.NotNull(company);
        Assert.Equal((true, 1), (company.ImprovedCaptainStats?.IsEnabled, company.ImprovedCaptainStats?.ArmBonus));
        Assert.Equal(["2", "1"], company.Entries.Select(entry => entry.CurrentArm));
        Assert.Equal([true, false], company.Entries.Select(entry => entry.CarriesCaptainUpgrades));
    }

    // A new company must pass the rules the API checks, or it could never be kept online.
    [Fact]
    public async Task FoundAsync_AnyCompany_PassesTheCompanyRules()
    {
        var result = await _founder.FoundAsync(Request(CompanyTypes.StandardVanilla, Trooper("Alpha", isLieutenant: true), Trooper("Bravo")));

        var company = await new CompanyFileStore(Substitute.For<ISyncOutbox>()).LoadAsync(result.FilePath);
        Assert.NotNull(company);
        Assert.Empty(SavedCompanyFileValidator.Instance.Validate(company).Errors);
    }

    // The company is kept online under its identifier, so two devices must never pick the same one.
    [Fact]
    public async Task FoundAsync_TwoCompaniesOfOneName_GetDifferentIdentifiers()
    {
        var store = new CompanyFileStore(Substitute.For<ISyncOutbox>());
        var first = await _founder.FoundAsync(Request("Standard Company", Trooper("Alpha", isLieutenant: true)));
        var firstIdentifier = (await store.LoadAsync(first.FilePath))?.CompanyIdentifier;
        File.Delete(first.FilePath);

        var second = await _founder.FoundAsync(Request("Standard Company", Trooper("Alpha", isLieutenant: true)));

        Assert.Equal(first.FilePath, second.FilePath);
        var secondIdentifier = (await store.LoadAsync(second.FilePath))?.CompanyIdentifier;
        Assert.Equal(32, secondIdentifier?.Length);
        Assert.NotEqual(firstIdentifier, secondIdentifier);
    }

    [Fact]
    public async Task FoundAsync_NoTroopers_ReturnsNoTroopers()
    {
        var result = await _founder.FoundAsync(Request("Standard Company"));

        Assert.Equal(FoundingOutcome.NoTroopers, result.Outcome);
        await _dialog.DidNotReceive().ShowAsync(Arg.Any<CaptainUpgradePopupContext>());
    }

    [Fact]
    public async Task FoundAsync_CaptainPopupCancelled_SavesNothing()
    {
        _dialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>()).Returns((SavedImprovedCaptainStats?)null);

        var result = await _founder.FoundAsync(Request("Standard Company", Trooper("Alpha", isLieutenant: true)));

        Assert.Equal(FoundingOutcome.CaptainCancelled, result.Outcome);
        Assert.False(Directory.Exists(_records.Paths.CompaniesDirectory) && Directory.EnumerateFiles(_records.Paths.CompaniesDirectory).Any());
    }

    [Fact]
    public async Task FoundAsync_TagCompanyWithTagCaptain_ShowsTheTagSpecOpsPopup()
    {
        await _founder.FoundAsync(Request(CompanyFounder.TagCompanyType, Trooper("Squalo", isLieutenant: true, unitTypeCode: "TAG")));

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.PopupTitle == "TAG Lieutenant Spec Ops" &&
            context.BaseExperienceOverride == 20 &&
            context.LockedStats.SequenceEqual(TagRestrictions.LockedStats)));
    }

    [Fact]
    public async Task FoundAsync_TagCompanyWithOtherCaptain_ShowsTheCaptainPopup()
    {
        await _founder.FoundAsync(Request(CompanyFounder.TagCompanyType, Trooper("Alpha", isLieutenant: true)));

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context =>
            context.PopupTitle == "Captain Configuration" && context.LockedStats.Count == 0));
    }
}
