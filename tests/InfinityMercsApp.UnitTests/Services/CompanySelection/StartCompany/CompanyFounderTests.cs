using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using NSubstitute;

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
        _founder = new CompanyFounder(captains, new SavedEntryWriter(armyData), _records.Paths);
        _dialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>())
            .Returns(new SavedImprovedCaptainStats { IsEnabled = true, CaptainName = "Kestrel" });
    }

    public void Dispose() => _records.Dispose();

    private static MercsCompanyEntry Trooper(string name, bool isLieutenant = false) => new()
    {
        Name = name,
        BaseUnitName = name,
        Subtitle = "MOV 10-10 | CC 13 | BS 12 | PH 10 | WIP 12 | ARM 1 | BTS 0 | VITA 1 | S 2",
        CostValue = 10,
        IsLieutenant = isLieutenant,
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
    public async Task FoundAsync_TagCompany_ShowsTheTagSpecOpsPopup()
    {
        await _founder.FoundAsync(Request(CompanyFounder.TagCompanyType, Trooper("Squalo", isLieutenant: true)));

        await _dialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context => context.PopupTitle == "TAG Lieutenant Spec Ops"));
    }
}
