using InfinityMercsApp.Infrastructure.API.Leagues;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>What to tell the user when a league request didn't go through.</summary>
public static class LeagueMessages
{
    public const string SignInFirst = "Log in to use leagues.";

    public static string For(LeaguesStatus status, IReadOnlyList<string> problems) => status switch
    {
        LeaguesStatus.Ok => string.Empty,
        LeaguesStatus.Rejected when problems.Count > 0 => string.Join(" ", problems),
        LeaguesStatus.Rejected => "The server refused that.",
        LeaguesStatus.NotFound => "That league isn't there, or you aren't in it.",
        LeaguesStatus.Unauthorized => "Log in again to use leagues.",
        LeaguesStatus.Forbidden => "Only the league's organizer can do that.",
        LeaguesStatus.Conflict => "The league changed while that was saving. Try again.",
        _ => "The server couldn't be reached. Try again later."
    };
}
