using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Services.Season;

/// <summary>What a saved company starts its season with.</summary>
public static class SeasonStart
{
    // A saved profile key reads "group|option|cost|swc|lt:N".
    private const int ProfileKeySwcPart = 3;

    /// <summary>
    /// The CR left from the starting credits once the company was recruited, and the +1 SWC its captain gives
    /// when the captain's profile grants it.
    /// </summary>
    public static SeasonStartingFunds FundsFor(SavedCompanyFile company) => new()
    {
        Credits = Math.Max(0, company.StartSeasonPoints - company.CurrentPoints),
        Swc = company.Entries
            .Where(entry => entry.IsLieutenant && !entry.IsPeripheralUnit)
            .Select(entry => ProfileValues.SwcGrant(SwcOf(entry.ProfileKey)))
            .FirstOrDefault()
    };

    private static string? SwcOf(string profileKey)
    {
        var parts = profileKey.Split('|');
        return parts.Length > ProfileKeySwcPart ? parts[ProfileKeySwcPart] : null;
    }
}
