using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

/// <summary>A throwaway MercenaryRecords folder.</summary>
internal sealed class TempRecords : IDisposable
{
    public TempRecords()
    {
        Paths = new RecordPaths(Path.Combine(Path.GetTempPath(), "mercs-tests-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Paths.SeasonsDirectory);
    }

    public RecordPaths Paths { get; }

    public string WriteCompany(string name, string json) => Write(Paths.CompaniesDirectory, name, json);

    public string WriteSeason(string name, string json) => Write(Paths.SeasonsDirectory, name, json);

    private static string Write(string directory, string name, string json)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, json);
        return path;
    }

    public void Dispose() => Directory.Delete(Paths.CompaniesDirectory, recursive: true);
}

public sealed class SeasonRecordCatalogTests : IDisposable
{
    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    [Fact]
    public async Task ListsSeasonsNewestFirstAndSkipsMalformedFiles()
    {
        _records.WriteSeason("old.json", """{"companyName":"Old","companyFilePath":"/c/old.json","createdDate":"2026-01-02"}""");
        _records.WriteSeason("new.json", """{"companyName":"New","companyFilePath":"/c/new.json","createdDate":"2026-03-04"}""");
        _records.WriteSeason("broken.json", "{not json");

        var records = await new SeasonRecordCatalog(_records.Paths).ListAsync();

        Assert.Equal(["New", "Old"], records.Select(r => r.CompanyName));
        Assert.Equal("/c/new.json", records[0].CompanyFilePath);
        Assert.Equal("Started 2026-03-04", records[0].Subtitle);
    }

    [Fact]
    public async Task FindsTheCompanyByIdentifierWhenThePathIsMissing()
    {
        _records.WriteCompany("other.json", """{"companyIdentifier":"zzz"}""");
        var company = _records.WriteCompany("mine.json", """{"companyIdentifier":"ABC"}""");
        var season = _records.WriteSeason("Unnamed-1.json", """{"companyIdentifier":"abc","createdDate":"2026-05-06"}""");

        var record = Assert.Single(await new SeasonRecordCatalog(_records.Paths).ListAsync());

        Assert.Equal(company, record.CompanyFilePath);
        Assert.Equal(season, record.SeasonFilePath);
        Assert.Equal("Unnamed-1", record.CompanyName);
    }

    [Fact]
    public async Task NoSeasonsFolderMeansNoSeasons()
    {
        var records = await new SeasonRecordCatalog(new RecordPaths(Path.Combine(_records.Paths.CompaniesDirectory, "missing"))).ListAsync();
        Assert.Empty(records);
    }
}

public sealed class LoadSeasonViewModelTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    public void Dispose() => _records.Dispose();

    private async Task<LoadSeasonViewModel> OpenAsync()
    {
        var viewModel = new LoadSeasonViewModel(_navigation, _dialogs, new SeasonRecordCatalog(_records.Paths));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public async Task ShowsTheEmptyStateWithoutSeasons()
    {
        var viewModel = await OpenAsync();

        Assert.False(viewModel.HasSeasonRecords);
        Assert.True(viewModel.ShowEmptyState);
    }

    [Fact]
    public async Task OpeningASeasonGoesToTheSeasonPage()
    {
        var company = _records.WriteCompany("company.json", "{}");
        var season = _records.WriteSeason("s.json", $$"""{"companyName":"Kestrel","companyFilePath":{{System.Text.Json.JsonSerializer.Serialize(company)}}}""");
        var viewModel = await OpenAsync();

        await viewModel.OpenSeasonCommand.ExecuteAsync(Assert.Single(viewModel.SeasonRecords));

        await _navigation.Received(1).NavigateToAsync(
            "//SeasonPage",
            Arg.Is<IDictionary<string, object>>(p =>
                (string)p[NavigationParameters.CompanyFilePath] == company &&
                (string)p[NavigationParameters.SeasonFilePath] == season));
    }

    [Fact]
    public async Task AMissingCompanyFileIsReportedInsteadOfOpened()
    {
        _records.WriteSeason("s.json", """{"companyName":"Gone","companyFilePath":"/nowhere/company.json"}""");
        var viewModel = await OpenAsync();

        await viewModel.OpenSeasonCommand.ExecuteAsync(viewModel.SeasonRecords[0]);

        await _navigation.DidNotReceiveWithAnyArgs().NavigateToAsync(default!, default);
        await _dialogs.Received(1).ShowAlertAsync("Company Not Found", Arg.Is<string>(m => m.Contains("'Gone'")), "OK");
    }
}
