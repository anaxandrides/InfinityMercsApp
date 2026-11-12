using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>One of the user's own seasons, offered for entering in a league.</summary>
public sealed record SeasonChoice(SeasonRecord Season, string Text)
{
    public static SeasonChoice For(SeasonRecord season) => new(season, $"{season.CompanyName} · started {season.StartedUtc:yyyy-MM-dd}");

    public override string ToString() => Text;
}
