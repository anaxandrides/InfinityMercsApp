using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Services.Season;

/// <summary>Reads and writes season files, for injection into ViewModels.</summary>
public interface ISeasonFileService
{
    Task<SeasonFile?> LoadAsync(string? filePath);

    Task<bool> SaveAsync(string? filePath, SeasonFile? seasonFile);

    /// <summary>Applies <paramref name="updater"/> to the latest round, refreshes the status and saves.</summary>
    Task<bool> UpdateLatestRoundAsync(string? filePath, Action<SeasonRound> updater);
}

/// <summary>
/// The injectable face of <see cref="SeasonFileService"/>. Pages not yet on ViewModels still call the
/// static service; once they are converted the implementation moves in here.
/// </summary>
internal sealed class SeasonFileStore : ISeasonFileService
{
    public Task<SeasonFile?> LoadAsync(string? filePath) => SeasonFileService.LoadSeasonFileAsync(filePath);

    public Task<bool> SaveAsync(string? filePath, SeasonFile? seasonFile) => SeasonFileService.SaveSeasonFileAsync(filePath, seasonFile);

    public Task<bool> UpdateLatestRoundAsync(string? filePath, Action<SeasonRound> updater) =>
        SeasonFileService.UpdateLatestRoundAsync(filePath, updater);
}
