using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.Views.Base;

public abstract class ContentPageBase : ContentPage
{
    public ContentPageBase()
    {
        NavigationPage.SetBackButtonTitle(this, string.Empty);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        PageTelemetry.Report(this);

        if (BindingContext is not IViewModelBase ivmb)
        {
            return;
        }

        await ivmb.InitializeAsyncCommand.ExecuteAsync(null);
    }
}
