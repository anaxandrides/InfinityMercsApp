using System.Text.Json;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Sync;

namespace InfinityMercsApp.Services.Records;

/// <inheritdoc/>
/// <remarks>Every save is noted in the sync outbox, to be sent to the user's account online.</remarks>
internal sealed class CompanyFileStore(ISyncOutbox outbox) : ICompanyFileService
{

    public bool Exists(string? filePath) => !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath);

    public async Task<SavedCompanyFile?> LoadAsync(string filePath) =>
        JsonSerializer.Deserialize<SavedCompanyFile>(await File.ReadAllTextAsync(filePath), RecordJson.ReadOptions);

    /// <summary>Writes the company; a Lone Wolf's Aspects are copied from it again first, so they never fall behind.</summary>
    public async Task SaveAsync(string filePath, SavedCompanyFile company)
    {
        LoneWolfAspects.Refresh(company);
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(company, RecordJson.CompanyWriteOptions));
        if (!string.IsNullOrEmpty(company.CompanyIdentifier))
        {
            outbox.RecordSave(UserDocumentKind.Company, company.CompanyIdentifier, filePath);
        }
    }
}
