using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Presentation.Leagues;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Leagues;

/// <summary>
/// The leagues the user belongs to, starting a new one (which they then organize), and joining one with an invite
/// code. Leagues are online, so this needs a sign-in.
/// </summary>
public partial class LeaguesViewModel(
    INavigationService navigationService,
    ILeaguesApi leagues,
    ISignInService signIn,
    IDialogService dialogs) : ViewModelBase(navigationService)
{
    public const string LeaguePage = "LeaguePage";
    public const string JoinLeaguePage = "JoinLeaguePage";

    public ObservableCollection<LeagueSummary> Leagues { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLogIn))]
    [NotifyCanExecuteChangedFor(nameof(CreateLeagueCommand), nameof(JoinCommand), nameof(LogInCommand))]
    public partial bool IsSignedIn { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LogInCommand))]
    public partial bool IsSigningIn { get; set; }

    /// <summary>What happened last, or why the list is empty.</summary>
    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateLeagueCommand))]
    public partial string NewLeagueName { get; set; } = string.Empty;

    /// <summary>An invite code as typed: any case, with spaces or dashes.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
    public partial string InviteCode { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateLeagueCommand))]
    public partial bool IsCreating { get; set; }

    public bool ShowLogIn => signIn.IsAvailable && !IsSignedIn;

    public bool HasNoLeagues => IsSignedIn && Leagues.Count == 0;

    public override async Task InitializeAsync()
    {
        IsSignedIn = signIn.IsAvailable && await signIn.GetAccountAsync() is not null;
        await RefreshAsync();
    }

    /// <summary>Fetches the user's leagues again.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!IsSignedIn || await signIn.GetAccessTokenAsync() is not { } token)
        {
            ShowLeagues([]);
            Status = LeagueMessages.SignInFirst;
            return;
        }

        var reply = await leagues.ListAsync(token);
        ShowLeagues(reply.Value ?? []);
        Status = reply.Status == LeaguesStatus.Ok
            ? Leagues.Count == 0 ? "You aren't in a league yet. Start one, or join one with its code." : string.Empty
            : LeagueMessages.For(reply.Status, reply.Problems);
    }

    private bool CanLogIn() => ShowLogIn && !IsSigningIn;

    [RelayCommand(CanExecute = nameof(CanLogIn))]
    private async Task LogInAsync()
    {
        try
        {
            IsSigningIn = true;
            var outcome = await signIn.SignInAsync();
            if (outcome.Account is not null)
            {
                IsSignedIn = true;
                await RefreshAsync();
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

    private bool CanCreateLeague() => IsSignedIn && !IsCreating && LeagueText.IsName(NewLeagueName, LeagueLimits.NameLength);

    /// <summary>Starts a league the user organizes, and opens it.</summary>
    [RelayCommand(CanExecute = nameof(CanCreateLeague))]
    private async Task CreateLeagueAsync()
    {
        try
        {
            IsCreating = true;
            if (await signIn.GetAccessTokenAsync() is not { } token || await signIn.GetAccountAsync() is not { } account)
            {
                Status = LeagueMessages.SignInFirst;
                return;
            }

            var reply = await leagues.CreateAsync(new NewLeague { Name = NewLeagueName.Trim(), DisplayName = DisplayNames.Of(account) }, token);
            if (reply.Value is not { } league)
            {
                Status = LeagueMessages.For(reply.Status, reply.Problems);
                return;
            }

            NewLeagueName = string.Empty;
            await NavigationService.PushAsync(LeaguePage, NavigationParameters.ForLeague(league.LeagueId));
        }
        finally
        {
            IsCreating = false;
        }
    }

    private bool CanJoin() => IsSignedIn && Domain.Leagues.InviteCodes.Parse(InviteCode) is not null;

    /// <summary>Opens the invite the typed code leads to.</summary>
    [RelayCommand(CanExecute = nameof(CanJoin))]
    private async Task JoinAsync()
    {
        if (Domain.Leagues.InviteCodes.Parse(InviteCode) is { } code)
        {
            InviteCode = string.Empty;
            await NavigationService.PushAsync(JoinLeaguePage, new Dictionary<string, object> { [NavigationParameters.InviteCode] = code });
        }
    }

    [RelayCommand]
    private Task OpenAsync(LeagueSummary league) =>
        NavigationService.PushAsync(LeaguePage, NavigationParameters.ForLeague(league.LeagueId));

    private void ShowLeagues(IEnumerable<LeagueSummary> list)
    {
        Leagues.Clear();
        foreach (var league in list)
        {
            Leagues.Add(league);
        }

        OnPropertyChanged(nameof(HasNoLeagues));
    }
}
