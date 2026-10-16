using System.Text.Json;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Records;

/// <inheritdoc/>
internal sealed class CompanyFileStore : ICompanyFileService
{
    private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public bool Exists(string? filePath) => !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);

    public async Task<SavedCompanyFile?> LoadAsync(string filePath) =>
        JsonSerializer.Deserialize<SavedCompanyFile>(await File.ReadAllTextAsync(filePath), ReadOptions);

    public Task SaveAsync(string filePath, SavedCompanyFile company) =>
        File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(company, WriteOptions));
}
