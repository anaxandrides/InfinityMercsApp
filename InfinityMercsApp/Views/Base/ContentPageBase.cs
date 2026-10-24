using InfinityMercsApp.Services.Telemetry;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.Views.Base;

/// <summary>A page whose view model is set up when it appears, and given the parameters Shell navigated to it with.</summary>
public abstract class ContentPageBase : ContentPage, IQueryAttributable
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

    /// <summary>Shell gives the page its query; the view model, which knows nothing of Shell, takes it from here.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query) => (BindingContext as IViewModelBase)?.ApplyQueryAttributes(query);
}
