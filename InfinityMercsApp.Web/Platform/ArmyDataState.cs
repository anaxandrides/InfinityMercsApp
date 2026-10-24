using CommunityToolkit.Mvvm.Messaging;
using InfinityMercsApp.Messages;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.ViewModels.Splash;

namespace InfinityMercsApp.Web.Platform;

/// <summary>
/// The army data in the browser's memory: loaded once per visit, the first time a page needs it, by the shared splash
/// view model. Once it is in, companies and seasons start syncing with the account, as they do in the app.
/// </summary>
public sealed class ArmyDataState : IRecipient<SplashCompletedMessage>
{
    private readonly ISyncCoordinator _sync;

    public ArmyDataState(ISyncCoordinator sync)
    {
        _sync = sync;
        WeakReferenceMessenger.Default.Register(this);
    }

    /// <summary>The splash view model loading the data, whose progress the loading page shows; null before it starts.</summary>
    public SplashPageViewModel? Loader { get; private set; }

    public bool IsLoaded { get; private set; }

    public bool IsLoading { get; private set; }

    // The splash view model goes to the app's home once it has loaded; the site stays on the page that wanted the data.
    private bool _holdsHome;

    /// <summary>True, once, for the splash view model's way home after loading, which the site ignores.</summary>
    public bool TakeSplashNavigation(string route)
    {
        if (!_holdsHome || route.TrimStart('/') != "MercsSeasonPage")
        {
            return false;
        }

        _holdsHome = false;
        return true;
    }

    /// <summary>The page waiting for the army data, told once it is in.</summary>
    public IArmyDataWatcher? Watcher { get; set; }

    /// <summary>Starts loading with <paramref name="loader"/>, unless it has started already; the view model doing it.</summary>
    public SplashPageViewModel Load(SplashPageViewModel loader)
    {
        if (Loader is { } loading)
        {
            return loading;
        }

        Loader = loader;
        IsLoading = true;
        _ = loader.InitializeAsyncCommand.ExecuteAsync(null);
        return loader;
    }

    public void Receive(SplashCompletedMessage message)
    {
        IsLoaded = true;
        IsLoading = false;
        _holdsHome = true;
        _sync.Start();
        Watcher?.ArmyDataLoaded();
    }
}
