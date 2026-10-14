using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Diagnostics;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels;

/// <summary>Diagnostics and temporary debug tools.</summary>
public partial class DebugViewModel(
    INavigationService navigationService,
    IDialogService dialogs,
    TestCompanyReformer reformer) : ViewModelBase(navigationService)
{
    private const string ReformTitle = "Reform Test Companies";

    /// <summary>Rebuilds the test companies; the button stays disabled while it runs.</summary>
    [RelayCommand]
    private async Task ReformTestCompaniesAsync()
    {
        var lines = await reformer.ReformAllAsync();
        await dialogs.ShowAlertAsync(ReformTitle, string.Join(Environment.NewLine, lines), "OK");
    }
}
