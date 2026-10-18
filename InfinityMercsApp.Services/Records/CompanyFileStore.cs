using System.Text.Json;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.Services.Records;

/// <inheritdoc/>
internal sealed class CompanyFileStore : ICompanyFileService
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public bool Exists(string? filePath) => !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);

    public async Task<SavedCompanyFile?> LoadAsync(string filePath) =>
        JsonSerializer.Deserialize<SavedCompanyFile>(await File.ReadAllTextAsync(filePath), ReadOptions);

    /// <summary>Writes the company; a Lone Wolf's Aspects are copied from it again first, so they never fall behind.</summary>
    public Task SaveAsync(string filePath, SavedCompanyFile company)
    {
        LoneWolfAspects.Refresh(company);
        return File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(company, WriteOptions));
    }
}
