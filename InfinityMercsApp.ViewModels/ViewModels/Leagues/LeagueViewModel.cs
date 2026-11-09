using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Domain.Leagues;
using InfinityMercsApp.Domain.Models.Leagues;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Presentation.Leagues;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Services.Sync;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Leagues;

/// <summary>
/// One league: its season's standings by renown, the games played, its members, entering one of the user's company
/// seasons, and recording a game the user's company played. The organizer also sees the invite (code, link and QR
/// code), and starts the next league season. Opening the league reports the current renown of the user's companies.
/// </summary>
public partial class LeagueViewModel(
    INavigationService navigationService,
    ILeaguesApi leagues,
    ISignInService signIn,
    IDialogService dialogs,
    SeasonRecordCatalog seasons,
    LeagueRenown renown,
    ISyncCoordinator sync) : ViewModelBase(navigationService)
{
    public const string LeagueCompanyPage = "LeagueCompanyPage";

    private string _leagueId = string.Empty;
    private IReadOnlyList<SeasonRecord> _ownSeasons = [];

    public ObservableCollection<LeagueStanding> Standings { get; } = [];

    public ObservableCollection<LeagueGameRow> Games { get; } = [];

    public ObservableCollection<LeagueMemberRow> Members { get; } = [];

    /// <summary>The user's seasons not yet entered in the league.</summary>
    public ObservableCollection<SeasonChoice> SeasonsToEnter { get; } = [];

    /// <summary>The user's companies in the current league season, to record a game for.</summary>
    public ObservableCollection<LeagueEntryChoice> OwnEntries { get; } = [];

    /// <summary>Everyone else's companies in the current league season.</summary>
    public ObservableCollection<LeagueEntryChoice> Opponents { get; } = [];

    public ObservableCollection<LeagueSeasonChoice> LeagueSeasons { get; } = [];

    public IReadOnlyList<ResultChoice> Results => ResultChoice.All;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name), nameof(IsOrganizer), nameof(InviteCode), nameof(InviteLink), nameof(HasInvite), nameof(CurrentSeasonText))]
    [NotifyCanExecuteChangedFor(nameof(NewInviteCommand), nameof(CloseInviteCommand), nameof(StartNextSeasonCommand))]
    public partial LeagueView? League { get; set; }

    /// <summary>The league season whose standings and games are shown.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsViewingCurrentSeason))]
    public partial LeagueSeasonChoice? ViewedSeason { get; set; }

    [ObservableProperty]
    public partial InviteQr? InviteQr { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EnterSeasonCommand), nameof(RecordGameCommand), nameof(NewInviteCommand), nameof(CloseInviteCommand), nameof(StartNextSeasonCommand))]
    public partial bool IsWorking { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EnterSeasonCommand))]
    public partial SeasonChoice? SeasonToEnter { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RecordGameCommand))]
    public partial LeagueEntryChoice? OwnEntry { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RecordGameCommand))]
    public partial LeagueEntryChoice? Opponent { get; set; }

    [ObservableProperty]
    public partial ResultChoice Result { get; set; } = ResultChoice.All[0];

    [ObservableProperty]
    public partial int OwnObjectivePoints { get; set; }

    [ObservableProperty]
    public partial int OpponentObjectivePoints { get; set; }

    [ObservableProperty]
    public partial string Mission { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTime PlayedOn { get; set; } = DateTime.Today;

    public string Name => League?.Name ?? "League";

    public bool IsOrganizer => League?.IsOrganizer == true;

    public string CurrentSeasonText => League is { } league ? $"Season {league.CurrentSeason}" : string.Empty;

    public string InviteCode => League?.InviteCode ?? string.Empty;

    public bool HasInvite => IsOrganizer && InviteCode.Length > 0;

    /// <summary>The page that joins the league, which the QR code also opens.</summary>
    public string InviteLink => HasInvite ? LeagueLinks.Join(InviteCode) : string.Empty;

    public bool IsViewingCurrentSeason => League is null || ViewedSeason?.Number == League.CurrentSeason;

    /// <summary>Whether the user has a company in the current season to record a game for, and someone to have played.</summary>
    public bool CanRecordGames => OwnEntries.Count > 0 && Opponents.Count > 0;

    public bool HasSeasonsToEnter => SeasonsToEnter.Count > 0;

    public override void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _leagueId = NavigationParameters.GetString(query, NavigationParameters.LeagueId) ?? string.Empty;

    public override async Task InitializeAsync()
    {
        // The user's seasons on this device, with any synced from their others (the site has only those).
        await sync.SyncNowAsync();
        _ownSeasons = await seasons.ListAsync();
        await RefreshAsync();
        await ReportRenownAsync();
    }

    /// <summary>Fetches the league again.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (await TokenAsync() is not { } token)
        {
            return;
        }

        Show(await leagues.GetAsync(_leagueId, token));
    }

    partial void OnViewedSeasonChanged(LeagueSeasonChoice? value) => ShowSeason();

    private bool CanEnterSeason() => SeasonToEnter is not null && !IsWorking;

    /// <summary>Enters the chosen season in the league's current season, with its company's renown.</summary>
    [RelayCommand(CanExecute = nameof(CanEnterSeason))]
    private async Task EnterSeasonAsync()
    {
        if (SeasonToEnter is not { } choice)
        {
            return;
        }

        try
        {
            IsWorking = true;
            if (await renown.OfAsync(choice.Season) is not { } companyRenown)
            {
                Status = "That season's company couldn't be read.";
                return;
            }

            // The league reads the season from the account, so it must be online first.
            await sync.SyncNowAsync();
            if (await TokenAsync() is { } token)
            {
                Show(await leagues.EnterAsync(_leagueId, choice.Season.SeasonIdentifier, new LeagueEntryUpdate { Renown = companyRenown }, token));
            }
        }
        finally
        {
            IsWorking = false;
        }
    }

    /// <summary>Takes one of the user's companies out of the league; the organizer can take out anyone's.</summary>
    [RelayCommand]
    private async Task WithdrawAsync(LeagueStanding standing)
    {
        if (!await dialogs.ConfirmAsync("Withdraw Company", $"Take {standing.CompanyName} out of the league?", "Withdraw", "Keep"))
        {
            return;
        }

        if (await TokenAsync() is { } token)
        {
            Show(await leagues.WithdrawAsync(_leagueId, standing.SeasonId, token));
        }
    }

    private bool CanRecordGame() => OwnEntry is not null && Opponent is not null && !IsWorking;

    /// <summary>Records the game for both companies; the user's company's renown is reported again with it.</summary>
    [RelayCommand(CanExecute = nameof(CanRecordGame))]
    private async Task RecordGameAsync()
    {
        if (OwnEntry is not { } own || Opponent is not { } opponent)
        {
            return;
        }

        try
        {
            IsWorking = true;
            if (await TokenAsync() is not { } token)
            {
                return;
            }

            var game = new NewLeagueGame
            {
                PlayedOn = DateOnly.FromDateTime(PlayedOn),
                Mission = Mission.Trim(),
                Sides =
                [
                    new LeagueGameSide { SeasonId = own.SeasonId, Result = Result.Result, ObjectivePoints = OwnObjectivePoints },
                    new LeagueGameSide { SeasonId = opponent.SeasonId, Result = Result.Opposite, ObjectivePoints = OpponentObjectivePoints }
                ]
            };
            var reply = await leagues.RecordGameAsync(_leagueId, game, token);
            Show(reply);
            if (reply.Status == LeaguesStatus.Ok)
            {
                Mission = string.Empty;
                OwnObjectivePoints = 0;
                OpponentObjectivePoints = 0;
                Status = "Game recorded.";
                await ReportRenownAsync();
            }
        }
        finally
        {
            IsWorking = false;
        }
    }

    /// <summary>Takes a game off the record, after asking; the recorder or the organizer may.</summary>
    [RelayCommand]
    private async Task RemoveGameAsync(LeagueGameRow game)
    {
        if (!await dialogs.ConfirmAsync("Remove Game", $"Remove the game of {game.PlayedOn} ({game.Score})?", "Remove", "Keep"))
        {
            return;
        }

        if (await TokenAsync() is { } token)
        {
            Show(await leagues.RemoveGameAsync(_leagueId, game.GameId, token));
        }
    }

    /// <summary>Opens a company entered in the league, as its owner keeps it.</summary>
    [RelayCommand]
    private Task ViewCompanyAsync(LeagueStanding standing) =>
        NavigationService.PushAsync(LeagueCompanyPage, NavigationParameters.ForLeague(_leagueId, standing.SeasonId));

    private bool CanOrganize() => IsOrganizer && !IsWorking;

    /// <summary>Issues a new invite code (opening joining if it was closed); the old code stops working.</summary>
    [RelayCommand(CanExecute = nameof(CanOrganize))]
    private async Task NewInviteAsync()
    {
        if (HasInvite && !await dialogs.ConfirmAsync("New Invite Code", "The current code and its link will stop working.", "New Code", "Keep"))
        {
            return;
        }

        await OrganizeAsync(LeagueAction.NewInvite);
    }

    private bool CanCloseInvite() => CanOrganize() && HasInvite;

    /// <summary>Stops anyone else joining, until a new code is issued.</summary>
    [RelayCommand(CanExecute = nameof(CanCloseInvite))]
    private Task CloseInviteAsync() => OrganizeAsync(LeagueAction.CloseInvite);

    /// <summary>Starts the league's next season, after asking: companies are entered afresh, and the standings start again.</summary>
    [RelayCommand(CanExecute = nameof(CanOrganize))]
    private async Task StartNextSeasonAsync()
    {
        if (League is not { } league ||
            !await dialogs.ConfirmAsync(
                "Start Next Season",
                $"End season {league.CurrentSeason} and start season {league.CurrentSeason + 1}? Its standings are kept; players enter new company seasons.",
                "Start",
                "Not Yet"))
        {
            return;
        }

        await OrganizeAsync(LeagueAction.NextSeason);
    }

    private async Task OrganizeAsync(LeagueAction action)
    {
        try
        {
            IsWorking = true;
            if (await TokenAsync() is not { } token || League is not { } league)
            {
                return;
            }

            Show(action switch
            {
                LeagueAction.NewInvite => await leagues.NewInviteAsync(_leagueId, token),
                LeagueAction.CloseInvite => await leagues.CloseInviteAsync(_leagueId, token),
                _ => await leagues.StartSeasonAsync(_leagueId, league.CurrentSeason, token)
            });
        }
        finally
        {
            IsWorking = false;
        }
    }

    /// <summary>Reports the current renown of the user's companies in the current league season, where it changed.</summary>
    private async Task ReportRenownAsync()
    {
        if (League is not { } league)
        {
            return;
        }

        foreach (var entry in league.Entries.Where(entry => entry.IsYours && entry.LeagueSeason == league.CurrentSeason).ToList())
        {
            if (_ownSeasons.FirstOrDefault(season => season.SeasonIdentifier == entry.SeasonId) is not { } season ||
                await renown.OfAsync(season) is not { } current ||
                current == entry.Renown ||
                await TokenAsync() is not { } token)
            {
                continue;
            }

            Show(await leagues.EnterAsync(_leagueId, entry.SeasonId, new LeagueEntryUpdate { Renown = current }, token));
        }
    }

    private async Task<string?> TokenAsync()
    {
        if (await signIn.GetAccessTokenAsync() is { } token)
        {
            return token;
        }

        Status = LeagueMessages.SignInFirst;
        return null;
    }

    private void Show(LeaguesReply<LeagueView> reply)
    {
        if (reply.Value is not { } league)
        {
            Status = LeagueMessages.For(reply.Status, reply.Problems);
            return;
        }

        var viewed = ViewedSeason?.Number;
        var wasCurrent = League is null || viewed == League.CurrentSeason;
        League = league;
        Status = string.Empty;
        InviteQr = HasInvite ? InviteQr.For(InviteLink) : null;

        Replace(Members, league.Members.Select(LeagueMemberRow.For));
        Replace(LeagueSeasons, LeagueSeasonChoice.UpTo(league.CurrentSeason));
        var current = league.Entries.Where(entry => entry.LeagueSeason == league.CurrentSeason).ToList();
        Replace(OwnEntries, current.Where(entry => entry.IsYours).Select(entry => LeagueEntryChoice.For(entry, league)));
        Replace(Opponents, current.Where(entry => !entry.IsYours).Select(entry => LeagueEntryChoice.For(entry, league)));
        OwnEntry = OwnEntries.FirstOrDefault(choice => choice.SeasonId == OwnEntry?.SeasonId) ?? OwnEntries.FirstOrDefault();
        Opponent = Opponents.FirstOrDefault(choice => choice.SeasonId == Opponent?.SeasonId);
        var entered = league.Entries.Select(entry => entry.SeasonId).ToHashSet();
        Replace(SeasonsToEnter, _ownSeasons.Where(season => season.SeasonIdentifier.Length > 0 && !entered.Contains(season.SeasonIdentifier)).Select(SeasonChoice.For));
        SeasonToEnter = SeasonsToEnter.FirstOrDefault(choice => choice.Season == SeasonToEnter?.Season) ?? SeasonsToEnter.FirstOrDefault();
        OnPropertyChanged(nameof(CanRecordGames));
        OnPropertyChanged(nameof(HasSeasonsToEnter));

        // A new season shows itself; a past one being looked at stays.
        var keep = wasCurrent ? league.CurrentSeason : viewed ?? league.CurrentSeason;
        var choice = LeagueSeasons.FirstOrDefault(season => season.Number == keep) ?? LeagueSeasons.FirstOrDefault();
        if (choice == ViewedSeason)
        {
            ShowSeason();
        }
        else
        {
            ViewedSeason = choice;
        }
    }

    private void ShowSeason()
    {
        if (League is not { } league || ViewedSeason is not { } season)
        {
            return;
        }

        Replace(Standings, LeagueStandings.For(league, season.Number));
        Replace(Games, LeagueGameRow.ListFor(league, season.Number));
        OnPropertyChanged(nameof(IsViewingCurrentSeason));
    }

    private static void Replace<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
