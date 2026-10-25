using CommunityToolkit.Mvvm.Input;

namespace InfinityMercsApp.ViewModels.Base;

/// <summary>
/// A page ViewModel. Navigation hands it the page's parameters through <see cref="ApplyQueryAttributes"/>: in the MAUI
/// app the page passes on what Shell gives it, and on the web the page passes on its route's parameters.
/// </summary>
public interface IViewModelBase
{
    void ApplyQueryAttributes(IDictionary<string, object> query);

    public bool IsInitialized { get; set; }

    public bool IsBusy { get; }

    public IAsyncRelayCommand InitializeAsyncCommand { get; }
}
