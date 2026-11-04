namespace InfinityMercsApp.Web.Platform;

/// <summary>The season steps' MAUI routes and site addresses.</summary>
public static class SeasonSteps
{
    /// <summary>The step a MAUI route opens, e.g. "RankUpPage"; null for a route that isn't one.</summary>
    public static SeasonStep? ForRoute(string route) => route.TrimStart('/') switch
    {
        "RankUpPage" => SeasonStep.RankUp,
        "ByeRoundPage" => SeasonStep.Bye,
        "LootBoxPage" => SeasonStep.Loot,
        "PlayModePage" => SeasonStep.Play,
        "GameModePage" => SeasonStep.Game,
        "InjuriesPage" => SeasonStep.Injuries,
        "MissionOutcomePage" => SeasonStep.Outcome,
        "ExperiencePage" => SeasonStep.Experience,
        "DowntimePage" => SeasonStep.Downtime,
        _ => null
    };

    /// <summary>The step's part of the address, after the season's.</summary>
    public static string Slug(this SeasonStep step) => step switch
    {
        SeasonStep.RankUp => "rank-up",
        SeasonStep.Bye => "bye",
        SeasonStep.Loot => "loot",
        SeasonStep.Play => "play",
        SeasonStep.Game => "game",
        SeasonStep.Injuries => "injuries",
        SeasonStep.Outcome => "outcome",
        SeasonStep.Experience => "experience",
        _ => "downtime"
    };

    /// <summary>
    /// Whether the step can be opened from its address alone, as on a reload. The round's later steps carry what
    /// happened in the game in memory, so without it they go back to the season.
    /// </summary>
    public static bool OpensFromAddress(this SeasonStep step) =>
        step is SeasonStep.RankUp or SeasonStep.Bye or SeasonStep.Loot or SeasonStep.Play;

    /// <summary>The step's address for a season, by the season's id.</summary>
    public static string Address(this SeasonStep step, string seasonIdentifier) =>
        $"{WebRoutes.SeasonById(seasonIdentifier)}/{step.Slug()}";
}
