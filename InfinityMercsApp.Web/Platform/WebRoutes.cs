using InfinityMercsApp.Services.Navigation;

namespace InfinityMercsApp.Web.Platform;

/// <summary>Where the site shows each page the shared view models navigate to by its MAUI route.</summary>
public static class WebRoutes
{
    public const string Home = "";
    public const string NewCompany = "companies/new";
    public const string CompanySource = "companies/new/source";
    public const string CompanySelection = "companies/new/build";
    public const string Season = "season";
    public const string Leagues = "leagues";

    public static string League(string leagueId) => $"leagues/{Uri.EscapeDataString(leagueId)}";

    /// <summary>A company entered in a league, for its members to look at.</summary>
    public static string LeagueCompany(string leagueId, string seasonId) =>
        $"leagues/{Uri.EscapeDataString(leagueId)}/companies/{Uri.EscapeDataString(seasonId)}";

    /// <summary>The page an invite link opens; <see cref="Domain.Leagues.LeagueLinks.Join"/> is its full address.</summary>
    public static string Join(string code) => $"join/{Uri.EscapeDataString(code)}";

    /// <summary>A season by its id, to manage its company.</summary>
    public static string SeasonById(string seasonIdentifier) => $"season/{Uri.EscapeDataString(seasonIdentifier)}";

    /// <summary>The season a step's address is under ("season/{id}/rank-up" is under "season/{id}"); null for any other address.</summary>
    public static string? SeasonOf(string relativeAddress)
    {
        var path = relativeAddress.Split('?', '#')[0];
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 2 && parts[0] == Season ? $"{Season}/{parts[1]}" : null;
    }

    /// <summary>The site's address for a MAUI route, e.g. "//CreateNewCompanyPage"; null for a page the site doesn't have.</summary>
    public static string? For(string route) => route.TrimStart('/') switch
    {
        "MercsSeasonPage" or "LoadCompanyPage" or "LoadSeasonPage" => Home,
        "SeasonPage" => Season,
        "CreateNewCompanyPage" => NewCompany,
        "StandardCompanySourcePopupPage" => CompanySource,
        "LeaguesPage" => Leagues,
        _ => null
    };

    /// <summary>
    /// The site's address for a league page, which names its league (and company, or invite) so it opens again after a
    /// reload; null for a route that isn't a league page or lacks what it names.
    /// </summary>
    public static string? ForLeague(string route, IDictionary<string, object>? parameters)
    {
        var query = parameters ?? new Dictionary<string, object>();
        var leagueId = NavigationParameters.GetString(query, NavigationParameters.LeagueId);
        var seasonId = NavigationParameters.GetString(query, NavigationParameters.SeasonId);
        var code = NavigationParameters.GetString(query, NavigationParameters.InviteCode);
        return route.TrimStart('/') switch
        {
            "LeaguePage" when leagueId is not null => League(leagueId),
            "LeagueCompanyPage" when leagueId is not null && seasonId is not null => LeagueCompany(leagueId, seasonId),
            "JoinLeaguePage" when code is not null => Join(code),
            _ => null
        };
    }
}
