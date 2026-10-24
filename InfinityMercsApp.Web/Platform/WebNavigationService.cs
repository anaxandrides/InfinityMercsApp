using InfinityMercsApp.Services.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace InfinityMercsApp.Web.Platform;

/// <summary>Navigates the site for the shared view models, mapping their MAUI routes to the site's addresses.</summary>
public sealed class WebNavigationService(
    NavigationManager navigation,
    IJSRuntime js,
    ArmyDataState armyData,
    NavigationQuery query,
    WebDialogService dialogs) : INavigationService
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null)
    {
        // The splash view model goes home once the army data is in; the site stays on the page that was asked for.
        if (!armyData.IsLoaded || armyData.TakeSplashNavigation(route))
        {
            return Task.CompletedTask;
        }

        if (WebRoutes.For(route) is not { } address)
        {
            return dialogs.ShowAlertAsync("Only in the App", "This part of the season is only in the Infinity Mercs app for now.");
        }

        query.Hold(routeParameters);
        navigation.NavigateTo(address);
        return Task.CompletedTask;
    }

    public Task PushAsync(string route, IDictionary<string, object>? routeParameters = null) => NavigateToAsync(route, routeParameters);

    public async Task PopAsync() => await js.InvokeVoidAsync("history.back");

    public Task PopToRootAsync()
    {
        navigation.NavigateTo(WebRoutes.Home);
        return Task.CompletedTask;
    }
}
