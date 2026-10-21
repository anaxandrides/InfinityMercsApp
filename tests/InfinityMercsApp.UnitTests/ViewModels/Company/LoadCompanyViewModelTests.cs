using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company;

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
    public async Task SavedRecords_NoCompanies_ShowsTheEmptyState()
    {
        var viewModel = await OpenAsync();

        Assert.False(viewModel.HasSavedRecords);
        Assert.Empty(viewModel.SavedRecords);
    }

    [Theory]
    [InlineData(false, "//CompanyViewerPage")]
    [InlineData(true, "//SeasonPage")]
    public async Task OpenCompanyCommand_Mode_GoesToTheViewerOrANewSeason(bool seasonMode, string route)
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
    public async Task DeleteCompanyCommand_Confirmed_RemovesTheCompany()
    {
        var company = _records.WriteCompany("c.json", """{"companyName":"Kestrel"}""");
        _dialogs.ConfirmAsync("Delete Company", "Confirm deletion of company [Kestrel]", "Confirm", "Back").Returns(true);
        var viewModel = await OpenAsync();

        await viewModel.DeleteCompanyCommand.ExecuteAsync(viewModel.SavedRecords[0]);

        Assert.False(File.Exists(company));
        Assert.Empty(viewModel.SavedRecords);
        Assert.False(viewModel.HasSavedRecords);
    }

    [Fact]
    public async Task DeleteCompanyCommand_Declined_KeepsTheCompany()
    {
        var company = _records.WriteCompany("c.json", "{}");
        var viewModel = await OpenAsync();

        await viewModel.DeleteCompanyCommand.ExecuteAsync(viewModel.SavedRecords[0]);

        Assert.True(File.Exists(company));
        Assert.Single(viewModel.SavedRecords);
    }
}
