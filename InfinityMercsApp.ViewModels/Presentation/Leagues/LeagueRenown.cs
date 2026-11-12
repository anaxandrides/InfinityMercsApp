using System.Text.Json;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.Presentation.Leagues;

/// <summary>
/// A season's company renown as the season page shows it, for reporting to a league. Its armies are loaded first,
/// since which troopers are Servant peripherals comes from the army data.
/// </summary>
public sealed class LeagueRenown(ICompanyFileService companyFiles, IArmyDataLoader armies, DeploymentRoster roster)
{
    /// <summary>The company's renown; null when the company file is missing or unreadable.</summary>
    public async Task<int?> OfAsync(SeasonRecord season)
    {
        try
        {
            if (!companyFiles.Exists(season.CompanyFilePath) || await companyFiles.LoadAsync(season.CompanyFilePath) is not { } company)
            {
                return null;
            }

            await armies.EnsureAsync(company.SourceFactions.Select(faction => faction.FactionId)
                .Concat(company.Entries.Select(entry => entry.SourceFactionId))
                .Where(factionId => factionId > 0)
                .Distinct()
                .ToList());
            return await roster.CompanyRenownAsync(season.CompanyFilePath, season.SeasonFilePath);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"League renown of '{season.SeasonFilePath}' couldn't be worked out: {ex.Message}");
            return null;
        }
    }
}
