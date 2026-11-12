using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Sync;

namespace InfinityMercsApp.Web.Platform;

/// <summary>Finds a season of the account by its id, for a page opened from its address rather than by a view model.</summary>
public sealed class SeasonLookup(ISyncCoordinator sync, SeasonRecordCatalog seasons)
{
    /// <summary>
    /// The navigation parameters that open the season (its company and season files), after a sync brings in the
    /// account's files and any change since; null when the account has no such season.
    /// </summary>
    public async Task<Dictionary<string, object>?> ParametersForAsync(string seasonIdentifier)
    {
        await sync.SyncNowAsync();
        var record = (await seasons.ListAsync()).FirstOrDefault(season => season.SeasonIdentifier == seasonIdentifier);
        return record is null || record.CompanyFilePath.Length == 0
            ? null
            : NavigationParameters.ForCompany(record.CompanyFilePath, record.SeasonFilePath);
    }
}
