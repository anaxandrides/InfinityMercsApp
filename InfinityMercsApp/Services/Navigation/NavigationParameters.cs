using System.Collections;

namespace InfinityMercsApp.Services.Navigation;

/// <summary>
/// Names and helpers for Shell navigation parameters. Parameters are passed as objects through
/// <see cref="INavigationService"/> (or <c>Shell.GoToAsync(route, parameters)</c>), never
/// URL-encoded into the route, so file paths arrive exactly as sent.
/// </summary>
public static class NavigationParameters
{
    /// <summary>Path of the saved company file (string).</summary>
    public const string CompanyFilePath = "companyFilePath";

    /// <summary>Path of the season file (string).</summary>
    public const string SeasonFilePath = "seasonFilePath";

    /// <summary>Indices of the company entries deployed for a game (<see cref="IReadOnlyCollection{T}"/> of int).</summary>
    public const string DeployedIndices = "deployedIndices";

    /// <summary>Whether the game uses elite deployment (bool).</summary>
    public const string EliteDeployment = "eliteDeployment";

    /// <summary>The entry of the deployed loadout a Proxy Pack's Lone Wolf starts the game in (int).</summary>
    public const string LoneWolfStart = "loneWolfStart";

    /// <summary>
    /// The renown both sides deployed and what the company bought with inducements
    /// (<see cref="Domain.Models.Season.ContractInducements"/>).
    /// </summary>
    public const string Inducements = "inducements";

    /// <summary>Present when LoadCompanyPage is opened to start a season (bool).</summary>
    public const string SeasonMode = "seasonMode";

    /// <summary>Parameters for a page that works on a company, optionally within a season.</summary>
    public static Dictionary<string, object> ForCompany(string companyFilePath, string? seasonFilePath = null)
    {
        var parameters = new Dictionary<string, object> { [CompanyFilePath] = companyFilePath };
        if (seasonFilePath is not null)
        {
            parameters[SeasonFilePath] = seasonFilePath;
        }

        return parameters;
    }

    /// <summary>The parameter as text, or null when it is absent.</summary>
    public static string? GetString(IDictionary<string, object> query, string key)
    {
        return query.TryGetValue(key, out var value) ? value as string ?? value?.ToString() : null;
    }

    /// <summary>True when the parameter is <c>true</c>, <c>"true"</c> or <c>"1"</c>.</summary>
    public static bool GetFlag(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value))
        {
            return false;
        }

        return value switch
        {
            bool flag => flag,
            string text => text == "1" || bool.TryParse(text, out var parsed) && parsed,
            _ => false
        };
    }

    /// <summary>The parameter as an int, or null when it is absent or not a number.</summary>
    public static int? GetInt(IDictionary<string, object> query, string key) =>
        query.TryGetValue(key, out var value)
            ? value switch
            {
                int number => number,
                string text when int.TryParse(text, out var parsed) => parsed,
                _ => null
            }
            : null;

    /// <summary>The parameter as a set of ints, or null when it is absent.</summary>
    public static HashSet<int>? GetIntSet(IDictionary<string, object> query, string key)
    {
        if (!query.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            IEnumerable<int> numbers => numbers.ToHashSet(),
            string text => text
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => int.TryParse(part.Trim(), out var n) ? n : (int?)null)
                .OfType<int>()
                .ToHashSet(),
            IEnumerable items => items.OfType<int>().ToHashSet(),
            _ => null
        };
    }
}
