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
/// An invite, opened from its link, its QR code or a typed code: which league it leads to, and joining it. Someone
/// signed out is asked to log in first.
/// </summary>
public partial class JoinLeagueViewModel(
    INavigationService navigationService,
    ILeaguesApi leagues,
    ISignInService signIn,
    IDialogService dialogs) : ViewModelBase(navigationService)
{
    public const string InvalidInvite = "This invite doesn't lead to a league any more. Ask the organizer for a new code.";

    private string? _code;

    /// <summary>The league the invite leads to, once found.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLeague))]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand))]
    public partial LeagueInvitePreview? Preview { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowLogIn))]
    [NotifyCanExecuteChangedFor(nameof(LogInCommand))]
    public partial bool IsSignedIn { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(JoinCommand), nameof(LogInCommand))]
    public partial bool IsWorking { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;

    public bool HasLeague => Preview is not null;

    public bool ShowLogIn => signIn.IsAvailable && !IsSignedIn;

    /// <summary>"Run by Olive · 5 members".</summary>
    public string Details => Preview is { } preview
        ? $"Run by {preview.OrganizerName} · {preview.MemberCount} {(preview.MemberCount == 1 ? "member" : "members")}"
        : string.Empty;

    public override void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _code = InviteCodes.Parse(NavigationParameters.GetString(query, NavigationParameters.InviteCode));

    public override async Task InitializeAsync()
    {
        IsSignedIn = signIn.IsAvailable && await signIn.GetAccountAsync() is not null;
        await LookUpAsync();
    }

    private bool CanLogIn() => ShowLogIn && !IsWorking;

    [RelayCommand(CanExecute = nameof(CanLogIn))]
    private async Task LogInAsync()
    {
        try
        {
            IsWorking = true;
            var outcome = await signIn.SignInAsync();
            if (outcome.Account is not null)
            {
                IsSignedIn = true;
            }
            else if (outcome.Status is SignInStatus.Failed or SignInStatus.Unavailable)
            {
                await dialogs.ShowAlertAsync("Not Logged In", outcome.Message);
            }
        }
        finally
        {
            IsWorking = false;
        }

        await LookUpAsync();
    }

    private bool CanJoin() => Preview is not null && !IsWorking;

    /// <summary>Joins the league, or opens it for someone already in it.</summary>
    [RelayCommand(CanExecute = nameof(CanJoin))]
    private async Task JoinAsync()
    {
        if (Preview is not { } preview || _code is not { } code)
        {
            return;
        }

        if (preview.IsMember)
        {
            await OpenLeagueAsync(preview.LeagueId);
            return;
        }

        try
        {
            IsWorking = true;
            if (await signIn.GetAccessTokenAsync() is not { } token || await signIn.GetAccountAsync() is not { } account)
            {
                Status = LeagueMessages.SignInFirst;
                return;
            }

            var reply = await leagues.JoinAsync(code, new JoinLeague { DisplayName = DisplayNames.Of(account) }, token);
            if (reply.Value is { } league)
            {
                await OpenLeagueAsync(league.LeagueId);
            }
            else
            {
                Status = reply.Status == LeaguesStatus.NotFound ? InvalidInvite : LeagueMessages.For(reply.Status, reply.Problems);
            }
        }
        finally
        {
            IsWorking = false;
        }
    }

    private async Task LookUpAsync()
    {
        Preview = null;
        OnPropertyChanged(nameof(Details));
        if (_code is not { } code)
        {
            Status = InvalidInvite;
            return;
        }

        if (!IsSignedIn || await signIn.GetAccessTokenAsync() is not { } token)
        {
            Status = "Log in to see the league this invite leads to.";
            return;
        }

        var reply = await leagues.PreviewInviteAsync(code, token);
        Preview = reply.Value;
        OnPropertyChanged(nameof(Details));
        Status = reply.Value switch
        {
            { IsMember: true } => "You're already in this league.",
            not null => string.Empty,
            _ when reply.Status == LeaguesStatus.NotFound => InvalidInvite,
            _ => LeagueMessages.For(reply.Status, reply.Problems)
        };
    }

    private Task OpenLeagueAsync(string leagueId) =>
        NavigationService.PushAsync(LeaguesViewModel.LeaguePage, NavigationParameters.ForLeague(leagueId));
}
