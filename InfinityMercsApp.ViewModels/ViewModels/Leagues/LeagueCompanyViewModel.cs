using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Infrastructure.API.Leagues;
using InfinityMercsApp.Presentation.Leagues;
using InfinityMercsApp.Presentation.Records;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Leagues;

/// <summary>
/// A company entered in a league, as its owner keeps it now: its troopers, and its season's contracts. Any member of
/// the league can look; nobody but the owner can change it.
/// </summary>
public partial class LeagueCompanyViewModel(
    INavigationService navigationService,
    ILeaguesApi leagues,
    ISignInService signIn) : ViewModelBase(navigationService)
{
    private string _leagueId = string.Empty;
    private string _seasonId = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    public partial CompanySheet? Company { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title), nameof(HasSeason))]
    public partial SeasonSheet? Season { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Loading…";

    public string Title => Company?.Name ?? Season?.CompanyName ?? "Company";

    public bool HasSeason => Season is not null;

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _leagueId = NavigationParameters.GetString(query, NavigationParameters.LeagueId) ?? string.Empty;
        _seasonId = NavigationParameters.GetString(query, NavigationParameters.SeasonId) ?? string.Empty;
    }

    public override async Task InitializeAsync()
    {
        if (await signIn.GetAccessTokenAsync() is not { } token)
        {
            Status = LeagueMessages.SignInFirst;
            return;
        }

        var reply = await leagues.EntryFilesAsync(_leagueId, _seasonId, token);
        if (reply.Value is not { } files)
        {
            Status = LeagueMessages.For(reply.Status, reply.Problems);
            return;
        }

        Company = files.Company is { } company ? CompanySheet.From(company.CompanyIdentifier, company) : null;
        Season = files.Season is { } season ? SeasonSheet.From(_seasonId, season) : null;
        Status = Company is null && Season is null ? "Its owner has deleted this company." : string.Empty;
    }

    [RelayCommand]
    private Task BackAsync() => NavigationService.PopAsync();
}
