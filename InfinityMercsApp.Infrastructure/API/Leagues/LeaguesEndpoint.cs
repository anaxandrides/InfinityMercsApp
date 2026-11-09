using System.Globalization;
using InfinityMercsApp.ApiClient;

namespace InfinityMercsApp.Infrastructure.API.Leagues;

/// <summary>Where leagues are kept online.</summary>
/// <param name="BaseAddress">The API's root, ending in a slash, e.g. https://example.azurewebsites.net/api/.</param>
public sealed record LeaguesEndpoint(Uri BaseAddress)
{
    /// <summary>The API that infra/main.bicep deploys.</summary>
    public static LeaguesEndpoint Production { get; } = new(ApiAddress.Production);

    /// <summary>The user's leagues.</summary>
    public Uri Mine => new(BaseAddress, "me/leagues");

    /// <summary>Where new leagues are started.</summary>
    public Uri Leagues => new(BaseAddress, "leagues");

    public Uri League(string leagueId) => new(BaseAddress, $"leagues/{Escape(leagueId)}");

    public Uri Invite(string leagueId) => new(BaseAddress, $"leagues/{Escape(leagueId)}/invite");

    /// <summary>What an invite code leads to, and where joining with it is asked.</summary>
    public Uri InviteCode(string code) => new(BaseAddress, $"invites/{Escape(code)}");

    public Uri Entry(string leagueId, string seasonId) => new(BaseAddress, $"leagues/{Escape(leagueId)}/entries/{Escape(seasonId)}");

    public Uri Games(string leagueId) => new(BaseAddress, $"leagues/{Escape(leagueId)}/games");

    public Uri Game(string leagueId, string gameId) => new(BaseAddress, $"leagues/{Escape(leagueId)}/games/{Escape(gameId)}");

    public Uri Army(string leagueId) => new(BaseAddress, $"leagues/{Escape(leagueId)}/army");

    /// <summary>Starts the league season after <paramref name="fromSeason"/>.</summary>
    public Uri NextSeason(string leagueId, int fromSeason) =>
        new(BaseAddress, $"leagues/{Escape(leagueId)}/seasons?from={fromSeason.ToString(CultureInfo.InvariantCulture)}");

    private static string Escape(string text) => Uri.EscapeDataString(text);
}
