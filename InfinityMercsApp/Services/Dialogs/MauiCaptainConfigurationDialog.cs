using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Views.Captain;

namespace InfinityMercsApp.Services.Dialogs;

/// <inheritdoc/>
internal sealed class MauiCaptainConfigurationDialog : ICaptainConfigurationDialog
{
    public Task<SavedImprovedCaptainStats?> ShowAsync(CaptainUpgradePopupContext context)
    {
        var navigation = Shell.Current?.Navigation
            ?? throw new InvalidOperationException("No active Shell is available to show the captain configuration.");
        return ConfigureCaptainPopupPage.ShowAsync(navigation, context);
    }
}
