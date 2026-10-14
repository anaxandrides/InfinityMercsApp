using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class CompanyRecordCatalogTests : IDisposable
{
    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    [Fact]
    public async Task ListsCompaniesNewestFirstAndSkipsMalformedFiles()
    {
        _records.WriteCompany("old.json", """{"companyName":"Old","companyType":"TAG Company","createdUtc":"2026-01-02T03:04:05Z"}""");
        _records.WriteCompany("new.json", """{"companyName":"New","createdUtc":"2026-03-04T05:06:07Z"}""");
        _records.WriteCompany("broken.json", "{not json");

        var records = await new CompanyRecordCatalog(_records.Paths).ListAsync();

        Assert.Equal(["New", "Old"], records.Select(r => r.CompanyName));
        Assert.Equal("2026-03-04 05:06:07 UTC", records[0].Subtitle);
        Assert.Equal("SVGCache/MercsIcons/noun-battle-mech-1731140.svg", records[1].CompanyIconPath);
        Assert.Equal("SVGCache/MercsIcons/noun-hack-2277937.svg", records[0].CompanyIconPath);
    }

    [Fact]
    public async Task AnUnnamedCompanyTakesItsFileName()
    {
        _records.WriteCompany("Kestrel.json", "{}");

        var record = Assert.Single(await new CompanyRecordCatalog(_records.Paths).ListAsync());

        Assert.Equal("Kestrel", record.CompanyName);
    }

    [Fact]
    public async Task ShowsUpToTwoSourceFactionLogos()
    {
        var paths = _records.Paths with { FactionLogoDirectory = "/logos" };
        _records.WriteCompany("c.json", """{"sourceFactions":[{"factionId":101},{"factionId":101},{"factionId":0},{"factionId":102},{"factionId":103}]}""");

        var record = Assert.Single(await new CompanyRecordCatalog(paths).ListAsync());

        Assert.Equal([Path.Combine("/logos", "101.svg"), Path.Combine("/logos", "102.svg")], record.FactionIconPaths);
        Assert.True(record.HasSecondaryFactionIcon);
    }

    [Fact]
    public async Task FallsBackToTheEntriesFactionsWithoutSourceFactions()
    {
        _records.WriteCompany("c.json", """{"entries":[{"sourceFactionId":102},{"sourceFactionId":102}]}""");

        var record = Assert.Single(await new CompanyRecordCatalog(_records.Paths).ListAsync());

        Assert.Equal([_records.Paths.FactionLogoPath(102)], record.FactionIconPaths);
        Assert.False(record.HasSecondaryFactionIcon);
    }

    [Fact]
    public async Task DeleteRemovesTheFile()
    {
        var path = _records.WriteCompany("c.json", "{}");
        var catalog = new CompanyRecordCatalog(_records.Paths);

        catalog.Delete((await catalog.ListAsync())[0]);

        Assert.False(File.Exists(path));
    }
}

public sealed class LoadCompanyViewModelTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    public void Dispose() => _records.Dispose();

    private async Task<LoadCompanyViewModel> OpenAsync(bool seasonMode = false)
    {
        var viewModel = new LoadCompanyViewModel(_navigation, _dialogs, new CompanyRecordCatalog(_records.Paths));
        if (seasonMode)
        {
            viewModel.ApplyQueryAttributes(new Dictionary<string, object> { [NavigationParameters.SeasonMode] = true });
        }

        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task ShowsTheEmptyStateWithoutCompanies()
    {
        var viewModel = await OpenAsync();

        Assert.False(viewModel.HasSavedRecords);
        Assert.True(viewModel.ShowEmptyState);
    }

    [Theory]
    [InlineData(false, "//CompanyViewerPage")]
    [InlineData(true, "//SeasonPage")]
    public async Task OpeningACompanyGoesToTheViewerOrANewSeason(bool seasonMode, string route)
    {
        var company = _records.WriteCompany("c.json", "{}");
        var viewModel = await OpenAsync(seasonMode);

        await viewModel.OpenCompanyCommand.ExecuteAsync(Assert.Single(viewModel.SavedRecords));

        await _navigation.Received(1).NavigateToAsync(
            route,
            Arg.Is<IDictionary<string, object>>(p =>
                (string)p[NavigationParameters.CompanyFilePath] == company &&
                !p.ContainsKey(NavigationParameters.SeasonFilePath)));
    }

    [Fact]
    public async Task ConfirmedDeleteRemovesTheCompany()
    {
        var company = _records.WriteCompany("c.json", """{"companyName":"Kestrel"}""");
        _dialogs.ConfirmAsync("Delete Company", "Confirm deletion of company [Kestrel]", "Confirm", "Back").Returns(true);
        var viewModel = await OpenAsync();

        await viewModel.DeleteCompanyCommand.ExecuteAsync(viewModel.SavedRecords[0]);

        Assert.False(File.Exists(company));
        Assert.Empty(viewModel.SavedRecords);
        Assert.True(viewModel.ShowEmptyState);
    }

    [Fact]
    public async Task DeclinedDeleteKeepsTheCompany()
    {
        var company = _records.WriteCompany("c.json", "{}");
        var viewModel = await OpenAsync();

        await viewModel.DeleteCompanyCommand.ExecuteAsync(viewModel.SavedRecords[0]);

        Assert.True(File.Exists(company));
        Assert.Single(viewModel.SavedRecords);
    }
}
