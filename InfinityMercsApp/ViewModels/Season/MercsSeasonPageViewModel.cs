using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// The MERCS Season home: load a season, or start one from a saved or new company; and log in to keep companies and
/// seasons online, since a phone can't reach Settings until it is inside a flow. Signed in, it says who as.
/// </summary>
public partial class MercsSeasonPageViewModel(
    INavigationService navigationService,
    ISignInService signIn,
    ISyncCoordinator sync,
    IDialogService dialogs) : AdaptiveViewModelBase<MercsSeasonLayout>(navigationService, MercsSeasonLayout.For)
{
    [ObservableProperty]
    public partial bool IsChoosingNewSeason { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLogIn), nameof(ShowAccount))]
    [NotifyCanExecuteChangedFor(nameof(LogInCommand))]
    public partial bool IsSignedIn { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LogInCommand))]
    public partial bool IsSigningIn { get; set; }

    /// <summary>"Logged in as …" with the account's display name; empty when logged out.</summary>
    [ObservableProperty]
    public partial string AccountText { get; set; } = string.Empty;

    /// <summary>The log-in button, where this device can sign in and nobody has.</summary>
    public bool ShowLogIn => signIn.IsAvailable && !IsSignedIn;

    public bool ShowAccount => IsSignedIn;

    /// <summary>Always return to the main two-button state when the page reappears, and show who is logged in now.</summary>
    public override async Task InitializeAsync()
    {
        ShowMainActions();
        ShowAccountOf(signIn.IsAvailable ? await signIn.GetAccountAsync() : null);
    }

    [RelayCommand]
    private Task LoadSeasonAsync() => NavigationService.PushAsync("LoadSeasonPage");

    [RelayCommand]
    private void StartNewSeason() => IsChoosingNewSeason = true;

    [RelayCommand]
    private void CancelNewSeason() => ShowMainActions();

    [RelayCommand]
    private Task LoadCompanyAsync() => NavigationService.NavigateToAsync("//LoadCompanyPage");

    [RelayCommand]
    private Task CreateNewTeamAsync() => NavigationService.NavigateToAsync("//CreateNewCompanyPage");

    private bool CanLogIn() => ShowLogIn && !IsSigningIn;

    /// <summary>Opens the sign-in page, then syncs; a cancelled sign-in changes nothing, and a failed one says why.</summary>
    [RelayCommand(CanExecute = nameof(CanLogIn))]
    private async Task LogInAsync()
    {
        try
        {
            IsSigningIn = true;
            var outcome = await signIn.SignInAsync();
            if (outcome.Account is { } account)
            {
                ShowAccountOf(account);
                await sync.SyncNowAsync();
            }
            else if (outcome.Status is SignInStatus.Failed or SignInStatus.Unavailable)
            {
                await dialogs.ShowAlertAsync("Not Logged In", outcome.Message);
            }
        }
        finally
        {
            IsSigningIn = false;
        }
    }

    private void ShowAccountOf(SignedInAccount? account)
    {
        IsSignedIn = account is not null;
        AccountText = account is null ? string.Empty : $"Logged in as {account.DisplayName}";
    }

    private void ShowMainActions() => IsChoosingNewSeason = false;
}
