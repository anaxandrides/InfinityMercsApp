using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.ViewModels.UnitBrowser;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Viewer;

public sealed class CompanyViewerViewModelTests
{
    private const string CompanyPath = "/companies/blue.json";

    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly ICompanyFileService _companyFiles = Substitute.For<ICompanyFileService>();
    private readonly IAppPackageFiles _packageFiles = Substitute.For<IAppPackageFiles>();
    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();

    private static SavedCompanyFile Company(string name = "Blue Team") => new()
    {
        CompanyName = name,
        Entries =
        [
            CompanyRosterTests.Entry(1, "Bolt", weapons: ["Combi Rifle"]),
            CompanyRosterTests.Entry(2, "Fusilier", isLieutenant: true),
            CompanyRosterTests.Entry(3, "Traktor Mul", isPeripheral: true, characteristics: ["Peripheral (Servant)"])
        ]
    };

    private async Task<CompanyViewerViewModel> OpenAsync(SavedCompanyFile? company)
    {
        _companyFiles.Exists(CompanyPath).Returns(true);
        _companyFiles.LoadAsync(CompanyPath).Returns(company);

        var viewModel = new CompanyViewerViewModel(_navigation, _companyFiles, _packageFiles, new CompanyRoster(_armyData), new UnitDetailViewModel(_armyData));
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(CompanyPath));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task OpeningACompanyListsItsTroopersAndSelectsTheFirst()
    {
        var viewModel = await OpenAsync(Company());

        Assert.Equal("Company Viewer: Blue Team", viewModel.Title);
        Assert.Equal(["Captain", "Bolt", "Traktor Mul"], viewModel.CompanyUnits.Select(u => u.Name));
        Assert.Same(viewModel.CompanyUnits[0], viewModel.SelectedUnit);
        Assert.True(viewModel.CompanyUnits[0].IsSelected);
        Assert.Equal("Captain", viewModel.SelectedName);
        Assert.Equal("Fusilier", viewModel.SelectedBaseName);
        Assert.True(viewModel.HasSelectedBaseName);
        Assert.Equal("LI", viewModel.SelectedUnitType);
        Assert.True(viewModel.ShowLieutenantIcon);
        Assert.Equal(1, viewModel.LieutenantIconCount);
        Assert.False(viewModel.HasStatusMessage);
        Assert.Equal("Captain", viewModel.Detail.UnitNameHeading);
    }

    [Fact]
    public async Task AnUnnamedCompanyIsTitledByItsFile()
    {
        var viewModel = await OpenAsync(Company(name: " "));

        Assert.Equal("Company Viewer: blue", viewModel.Title);
    }

    [Fact]
    public async Task SelectingAnotherTrooperShowsIt()
    {
        var viewModel = await OpenAsync(Company());
        var bolt = viewModel.CompanyUnits[1];

        await viewModel.SelectUnitCommand.ExecuteAsync(bolt);

        Assert.True(bolt.IsSelected);
        Assert.False(viewModel.CompanyUnits[0].IsSelected);
        Assert.Equal("Bolt", viewModel.SelectedName);
        Assert.False(viewModel.HasSelectedBaseName);
        Assert.False(viewModel.ShowLieutenantIcon);
        Assert.Equal("Combi Rifle", Assert.Single(viewModel.RangedWeaponsFormatted.Spans).Text);
    }

    [Fact]
    public async Task APeripheralShowsThePeripheralIcon()
    {
        var viewModel = await OpenAsync(Company());

        await viewModel.SelectUnitCommand.ExecuteAsync(viewModel.CompanyUnits[2]);

        Assert.True(viewModel.ShowPeripheralIcon);
    }

    [Fact]
    public async Task AMissingFileSaysSo()
    {
        _companyFiles.Exists(CompanyPath).Returns(false);
        var viewModel = new CompanyViewerViewModel(_navigation, _companyFiles, _packageFiles, new CompanyRoster(_armyData), new UnitDetailViewModel(_armyData));
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(CompanyPath));

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        Assert.Equal("Saved company file was not found.", viewModel.StatusMessage);
        Assert.Equal(CompanyViewerViewModel.DefaultTitle, viewModel.Title);
        Assert.Empty(viewModel.CompanyUnits);
    }

    [Fact]
    public async Task AnEmptyFileSaysSo() =>
        Assert.Equal("Unable to read saved company data.", (await OpenAsync(null)).StatusMessage);

    [Fact]
    public async Task ACompanyWithoutTroopersSaysSo() =>
        Assert.Equal("No units found in this company.", (await OpenAsync(new SavedCompanyFile { CompanyName = "Empty" })).StatusMessage);

    [Fact]
    public async Task AReadFailureIsShown()
    {
        _companyFiles.Exists(CompanyPath).Returns(true);
        _companyFiles.LoadAsync(CompanyPath).ThrowsAsync(new IOException("disk gone"));
        var viewModel = new CompanyViewerViewModel(_navigation, _companyFiles, _packageFiles, new CompanyRoster(_armyData), new UnitDetailViewModel(_armyData));
        viewModel.ApplyQueryAttributes(NavigationParameters.ForCompany(CompanyPath));

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        Assert.Equal("Failed to load company: disk gone", viewModel.StatusMessage);
    }

    [Fact]
    public async Task TheCompanyIsOnlyReloadedForANewFile()
    {
        var viewModel = await OpenAsync(Company());

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        await _companyFiles.Received(1).LoadAsync(CompanyPath);
    }

    [Fact]
    public async Task ThePackagedLogoIsUsedWhenItExists()
    {
        _packageFiles.ExistsAsync("SVGCache/units/101-12.svg").Returns(true);

        var viewModel = await OpenAsync(Company());

        Assert.Equal("SVGCache/units/101-12.svg", viewModel.Detail.LogoSource);
    }

    [Fact]
    public async Task ATagUnitWithoutALogoFallsBackToTheTagIcon()
    {
        var company = new SavedCompanyFile { Entries = [CompanyRosterTests.Entry(1, "Turtlemek")] };

        var viewModel = await OpenAsync(company);

        Assert.Equal(CompanyRoster.TagCompanyFallbackIconPath, viewModel.Detail.LogoSource);
    }

    [Fact]
    public async Task RenamingCanBeRejected()
    {
        var viewModel = await OpenAsync(Company());

        viewModel.EditNameCommand.Execute(null);
        Assert.True(viewModel.IsEditingName);
        Assert.True(viewModel.IsEditingName);
        viewModel.NameDraft = "Major Lunah";
        viewModel.RejectNameCommand.Execute(null);

        Assert.False(viewModel.IsEditingName);
        Assert.Equal("Captain", viewModel.NameDraft);
        await _companyFiles.DidNotReceive().SaveAsync(Arg.Any<string>(), Arg.Any<SavedCompanyFile>());
    }

    [Fact]
    public async Task SavingANameRenamesTheTileAndTheSavedEntry()
    {
        var company = Company();
        var viewModel = await OpenAsync(company);

        viewModel.EditNameCommand.Execute(null);
        viewModel.NameDraft = "  Major Lunah ";
        await viewModel.SaveNameCommand.ExecuteAsync(null);

        Assert.Equal("Major Lunah", viewModel.SelectedName);
        Assert.Equal("Major Lunah", viewModel.CompanyUnits[0].Name);
        Assert.False(viewModel.IsEditingName);
        await _companyFiles.Received(1).SaveAsync(CompanyPath, Arg.Is<SavedCompanyFile>(saved =>
            saved.Entries.Single(entry => entry.EntryIndex == 2).CustomName == "Major Lunah"));
    }

    [Fact]
    public async Task ABlankNameKeepsTheCurrentOne()
    {
        var viewModel = await OpenAsync(Company());

        viewModel.EditNameCommand.Execute(null);
        viewModel.NameDraft = "   ";
        await viewModel.SaveNameCommand.ExecuteAsync(null);

        Assert.Equal("Captain", viewModel.SelectedName);
    }

    [Fact]
    public async Task NothingCanBeRenamedWithoutASelection()
    {
        var viewModel = await OpenAsync(new SavedCompanyFile());

        Assert.False(viewModel.EditNameCommand.CanExecute(null));
        Assert.False(viewModel.SaveNameCommand.CanExecute(null));
    }
}
