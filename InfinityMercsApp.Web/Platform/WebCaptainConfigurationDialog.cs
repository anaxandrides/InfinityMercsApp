using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.ViewModels.Captain;

namespace InfinityMercsApp.Web.Platform;

/// <inheritdoc/>
/// <remarks>Shows the shared captain view model in the layout's captain host, and waits for its answer.</remarks>
public sealed class WebCaptainConfigurationDialog : ICaptainConfigurationDialog
{
    public ICaptainPresenter? Presenter { get; set; }

    public async Task<SavedImprovedCaptainStats?> ShowAsync(CaptainUpgradePopupContext context)
    {
        if (Presenter is not { } presenter)
        {
            return null;
        }

        var viewModel = new ConfigureCaptainViewModel(context);
        presenter.Show(viewModel);
        try
        {
            return await viewModel.Completion;
        }
        finally
        {
            presenter.Show(null);
        }
    }
}
