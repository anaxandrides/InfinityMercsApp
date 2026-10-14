using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Services.Season;

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
