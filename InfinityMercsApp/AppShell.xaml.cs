namespace InfinityMercsApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.Splash.SplashPage), typeof(Views.Splash.SplashPage));
        // Routes declared in AppShell.xaml ShellContent entries must not be registered here,
        // otherwise Shell can throw ambiguous-route exceptions.
        Routing.RegisterRoute(nameof(Views.Feedback.FeedbackBugsPage), typeof(Views.Feedback.FeedbackBugsPage));

        // Season flow — pushed as a stack on top of SeasonPage.
        Routing.RegisterRoute(nameof(Views.Season.PlayModePage), typeof(Views.Season.PlayModePage));
        Routing.RegisterRoute(nameof(Views.Season.Game.GameModePage), typeof(Views.Season.Game.GameModePage));
        Routing.RegisterRoute(nameof(Views.Season.InjuriesPage), typeof(Views.Season.InjuriesPage));
        Routing.RegisterRoute(nameof(Views.Season.MissionOutcomePage), typeof(Views.Season.MissionOutcomePage));
        Routing.RegisterRoute(nameof(Views.Season.ExperiencePage), typeof(Views.Season.ExperiencePage));
        Routing.RegisterRoute(nameof(Views.Season.Downtime.DowntimePage), typeof(Views.Season.Downtime.DowntimePage));
        Routing.RegisterRoute(nameof(Views.Season.RankUp.RankUpPage), typeof(Views.Season.RankUp.RankUpPage));
        Routing.RegisterRoute(nameof(Views.Season.Bye.ByeRoundPage), typeof(Views.Season.Bye.ByeRoundPage));
        Routing.RegisterRoute(nameof(Views.Season.Loot.LootBoxPage), typeof(Views.Season.Loot.LootBoxPage));

        // MERCS Season home sub-pages.
        Routing.RegisterRoute(nameof(Views.Season.LoadSeasonPage), typeof(Views.Season.LoadSeasonPage));
    }
}
