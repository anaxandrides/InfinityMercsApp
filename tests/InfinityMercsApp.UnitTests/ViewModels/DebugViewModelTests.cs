using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.Diagnostics;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class DebugViewModelTests : IDisposable
{
    private readonly TempRecords _records = new();

    public void Dispose() => _records.Dispose();

    [Fact]
    public async Task ReformingShowsOneSummaryLinePerCompany()
    {
        var packageFiles = Substitute.For<IAppPackageFiles>();
        packageFiles.ReadTextAsync(Arg.Any<string>()).ThrowsAsync(new FileNotFoundException());
        var dialogs = Substitute.For<IDialogService>();
        var viewModel = new DebugViewModel(
            Substitute.For<INavigationService>(),
            dialogs,
            new TestCompanyReformer(Army101Data.Create(), Substitute.For<ISpecOpsProvider>(), packageFiles, _records.Paths));

        await viewModel.ReformTestCompaniesCommand.ExecuteAsync(null);

        await dialogs.Received(1).ShowAlertAsync(
            "Reform Test Companies",
            string.Join(Environment.NewLine, TestCompanyReformer.AdditionalCompanyFileNames.Select(f => $"{f}: skipped (missing company data)")),
            "OK");
    }
}
