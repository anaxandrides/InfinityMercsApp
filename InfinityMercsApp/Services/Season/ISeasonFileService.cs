using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Services.Season;

/// <summary>Reads and writes season files, for injection into ViewModels.</summary>
public interface ISeasonFileService
{
    /// <summary>Starts an empty season for a company and returns the new file's path.</summary>
    Task<string> CreateAsync(string companyName, string companyIdentifier, string companyFilePath);

    Task<SeasonFile?> LoadAsync(string? filePath);

    Task<bool> SaveAsync(string? filePath, SeasonFile? seasonFile);

    /// <summary>Applies <paramref name="updater"/> to the latest round, refreshes the status and saves.</summary>
    Task<bool> UpdateLatestRoundAsync(string? filePath, Action<SeasonRound> updater);
}
