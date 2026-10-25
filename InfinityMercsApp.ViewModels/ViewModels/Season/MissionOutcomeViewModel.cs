using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels.Base;

namespace InfinityMercsApp.ViewModels.Season;

/// <summary>
/// Records the mission result (victory or defeat, objective points) on the latest season round and
/// shows the credits it earns.
/// </summary>
public partial class MissionOutcomeViewModel(
    INavigationService navigationService,
    ISeasonFileService seasonFiles) : ViewModelBase(navigationService)
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;

    public IReadOnlyList<int> PointOptions { get; } = Enumerable.Range(0, 11).ToList();

    public string SwcText { get; } = SeasonEconomy.SwcText((decimal)SeasonEconomy.SwcPerMission);

    public int VictoryCredits => SeasonEconomy.VictoryCredits;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MissionCredits), nameof(CreditsTotal))]
    public partial int RoundNumber { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVictory), nameof(IsDefeat), nameof(VictoryBonus), nameof(CreditsTotal))]
    [NotifyCanExecuteChangedFor(nameof(GoToExperienceCommand))]
    public partial bool? Victory { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ObjectiveCredits), nameof(CreditsTotal))]
    [NotifyCanExecuteChangedFor(nameof(GoToExperienceCommand))]
    public partial int? PointsScored { get; set; }

    public bool IsVictory => Victory == true;

    public bool IsDefeat => Victory == false;

    public int MissionCredits => SeasonEconomy.MissionCredits(RoundNumber);

    public int ObjectiveCredits => SeasonEconomy.ObjectiveCredits(PointsScored ?? 0);

    public int VictoryBonus => SeasonEconomy.VictoryBonus(IsVictory);

    public int CreditsTotal => MissionCredits + ObjectiveCredits + VictoryBonus;

    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _companyFilePath = NavigationParameters.GetString(query, NavigationParameters.CompanyFilePath) ?? _companyFilePath;
        _seasonFilePath = NavigationParameters.GetString(query, NavigationParameters.SeasonFilePath) ?? _seasonFilePath;
    }

    /// <summary>The mission being scored is the latest round, which the game screen has just added.</summary>
    public override async Task InitializeAsync()
    {
        var seasonFile = await seasonFiles.LoadAsync(_seasonFilePath);
        RoundNumber = Math.Max(1, SeasonRounds.Current(seasonFile));
    }

    [RelayCommand]
    private void SelectVictory() => Victory = true;

    [RelayCommand]
    private void SelectDefeat() => Victory = false;

    private bool CanGoToExperience() => Victory.HasValue && PointsScored.HasValue;

    [RelayCommand(CanExecute = nameof(CanGoToExperience))]
    private async Task GoToExperienceAsync()
    {
        var won = IsVictory;
        var points = PointsScored ?? 0;
        await seasonFiles.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.MissionResults.Won = won;
            round.MissionResults.OpScored = points;
        });

        await NavigationService.PushAsync(
            "ExperiencePage",
            NavigationParameters.ForCompany(_companyFilePath, _seasonFilePath));
    }
}
