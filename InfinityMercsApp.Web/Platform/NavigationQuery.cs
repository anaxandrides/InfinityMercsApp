namespace InfinityMercsApp.Web.Platform;

/// <summary>
/// The parameters a shared view model navigated with, held for the page it opened: Shell hands them to the page in the
/// app; the site's page takes them from here once.
/// </summary>
public sealed class NavigationQuery
{
    private IDictionary<string, object>? _parameters;

    public void Hold(IDictionary<string, object>? parameters) => _parameters = parameters;

    /// <summary>The parameters held, now let go; null when none were.</summary>
    public IDictionary<string, object>? Take()
    {
        var parameters = _parameters;
        _parameters = null;
        return parameters;
    }
}
