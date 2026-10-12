using CommunityToolkit.Mvvm.Input;

namespace InfinityMercsApp.ViewModels.Base;

/// <summary>
/// A page ViewModel. Shell hands navigation query parameters to the page's BindingContext when it is
/// <see cref="IQueryAttributable"/>, so ViewModels receive them via <see cref="ApplyQueryAttributes"/>.
/// </summary>
public interface IViewModelBase : IQueryAttributable
{
    public bool IsInitialized { get; set; }

    public bool IsBusy { get; }

    public IAsyncRelayCommand InitializeAsyncCommand { get; }
}
