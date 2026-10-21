namespace InfinityMercsApp.Services.Navigation;

/// <summary>
/// A service to handle navigation.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Initializes the service.
    /// </summary>
    /// <returns></returns>
    Task InitializeAsync();

    /// <summary>
    /// Navigates to a page.
    /// </summary>
    /// <param name="route"></param>
    /// <param name="routeParameters"></param>
    /// <returns></returns>
    public Task NavigateToAsync(string route, IDictionary<string, object>? routeParameters = null);

    /// <summary>
    /// Pushes a registered (non-ShellContent) route onto the current navigation stack, passing
    /// <paramref name="routeParameters"/> to the page and its ViewModel as Shell query attributes.
    /// </summary>
    /// <param name="route">A route registered with <c>Routing.RegisterRoute</c>, e.g. <c>"PlayModePage"</c>.</param>
    /// <param name="routeParameters">Navigation parameters; values are passed as objects, not URL-encoded.</param>
    Task PushAsync(string route, IDictionary<string, object>? routeParameters = null);

    /// <summary>
    /// Navigates to the previous page.
    /// </summary>
    /// <returns></returns>
    Task PopAsync();

    /// <summary>
    /// Pops every pushed page, back to the root of the current navigation stack.
    /// </summary>
    Task PopToRootAsync();

    /// <summary>
    /// Pushes a modal.
    /// </summary>
    /// <param name="page"></param>
    /// <returns></returns>
    Task PushModalAsync(Page page);

    /// <summary>
    /// Pops a modal;
    /// </summary>
    /// <returns></returns>
    Task PopModalAsync();
}
