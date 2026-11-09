using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace InfinityMercsApp.Web.Platform;

/// <summary>
/// Navigates the site for the shared view models, mapping their MAUI routes to the site's addresses. A season's steps
/// (rank-up, a round's pages) sit under the season's address, so going back to the root returns to the season.
/// </summary>
public sealed class WebNavigationService(
    NavigationManager navigation,
    IJSRuntime js,
    NavigationQuery query,
    WebDialogService dialogs,
    ISeasonFileService seasonFiles) : INavigationService
{
    public const string OnlyInTheApp = "This part of the season is only in the Infinity Mercs app for now.";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null)
    {
        if (SeasonSteps.ForRoute(route) is { } step)
        {
            await OpenStepAsync(step, routeParameters);
            return;
        }

        if (WebRoutes.ForLeague(route, routeParameters) is { } leagueAddress)
        {
            navigation.NavigateTo(leagueAddress);
            return;
        }

        if (WebRoutes.For(route) is not { } address)
        {
            await dialogs.ShowAlertAsync("Only in the App", OnlyInTheApp);
            return;
        }

        query.Hold(routeParameters);
        navigation.NavigateTo(address);
    }

    public Task PushAsync(string route, IDictionary<string, object>? routeParameters = null) => NavigateToAsync(route, routeParameters);

    public async Task PopAsync() => await js.InvokeVoidAsync("history.back");

    /// <summary>
    /// From a season's step back to the season, as the app pops to its season page; home from anywhere else. The step's
    /// address names its season, so this holds after a reload too.
    /// </summary>
    public Task PopToRootAsync()
    {
        navigation.NavigateTo(WebRoutes.SeasonOf(navigation.ToBaseRelativePath(navigation.Uri)) ?? WebRoutes.Home);
        return Task.CompletedTask;
    }

    // A step's address names its season by id, read from the season file the view model passes.
    private async Task OpenStepAsync(SeasonStep step, IDictionary<string, object>? routeParameters)
    {
        var seasonFilePath = routeParameters is null ? null : NavigationParameters.GetString(routeParameters, NavigationParameters.SeasonFilePath);
        if (await seasonFiles.LoadAsync(seasonFilePath) is not { SeasonIdentifier.Length: > 0 } season)
        {
            await dialogs.ShowAlertAsync("Season Not Found", "The season's file couldn't be read.");
            return;
        }

        query.Hold(routeParameters);
        navigation.NavigateTo(step.Address(season.SeasonIdentifier));
    }
}
