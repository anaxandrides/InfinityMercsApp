using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Records;

/// <summary>Reads and writes saved company files, for injection into ViewModels.</summary>
public interface ICompanyFileService
{
    bool Exists(string? filePath);

    /// <summary>The saved company; null when the file holds no company. Read and parse errors are thrown.</summary>
    Task<SavedCompanyFile?> LoadAsync(string filePath);

    Task SaveAsync(string filePath, SavedCompanyFile company);
}
