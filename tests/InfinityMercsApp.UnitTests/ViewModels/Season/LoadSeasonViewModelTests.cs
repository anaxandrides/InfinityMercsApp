using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Season;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season;

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
    public async Task SeasonRecords_NoSeasons_ShowsTheEmptyState()
    {
        var viewModel = await OpenAsync();

        Assert.False(viewModel.HasSeasonRecords);
        Assert.Empty(viewModel.SeasonRecords);
    }

    [Fact]
    public async Task OpenSeasonCommand_Season_GoesToTheSeasonPage()
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
    public async Task OpenSeasonCommand_MissingCompanyFile_ReportsIt()
    {
        _records.WriteSeason("s.json", """{"companyName":"Gone","companyFilePath":"/nowhere/company.json"}""");
        var viewModel = await OpenAsync();

        await viewModel.OpenSeasonCommand.ExecuteAsync(viewModel.SeasonRecords[0]);

        await _navigation.DidNotReceiveWithAnyArgs().NavigateToAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>?>());
        await _dialogs.Received(1).ShowAlertAsync("Company Not Found", Arg.Is<string>(m => m.Contains("'Gone'")), "OK");
    }
}
