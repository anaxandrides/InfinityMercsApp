using System.Globalization;
using System.Text.Json;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Services.Sync;

namespace InfinityMercsApp.Services.Records;

/// <summary>Lists and deletes saved company files; deletions are noted in the sync outbox.</summary>
public sealed class CompanyRecordCatalog(RecordPaths paths, ISyncOutbox outbox)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = RecordJsonContext.Default };

    /// <summary>Every readable company file, newest first; malformed files are skipped.</summary>
    public async Task<IReadOnlyList<CompanyRecord>> ListAsync()
    {
        if (!Directory.Exists(paths.CompaniesDirectory))
        {
            return [];
        }

        var records = new List<CompanyRecord>();
        foreach (var filePath in Directory.EnumerateFiles(paths.CompaniesDirectory, "*.json"))
        {
            try
            {
                var payload = JsonSerializer.Deserialize(await File.ReadAllTextAsync(filePath), JsonContracts.For<SavedCompanyFile>(JsonOptions));
                if (payload is null)
                {
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(payload.CompanyName)
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : payload.CompanyName;
                var created = DateTimeOffset.TryParse(payload.CreatedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
                    ? parsed
                    : new DateTimeOffset(File.GetLastWriteTimeUtc(filePath), TimeSpan.Zero);
                var factionIcons = SourceFactionIds(payload).Take(2).Select(paths.FactionLogoPath).ToList();

                records.Add(new CompanyRecord(filePath, name, payload.CompanyType, factionIcons, created) { CompanyIdentifier = payload.CompanyIdentifier });
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // Skip malformed company files.
            }
        }

        return records.OrderByDescending(r => r.CreatedUtc).ToList();
    }

    public void Delete(CompanyRecord record)
    {
        if (!File.Exists(record.FilePath))
        {
            return;
        }

        var identifier = IdentifierOf(record.FilePath);
        File.Delete(record.FilePath);
        if (!string.IsNullOrEmpty(identifier))
        {
            outbox.RecordDelete(UserDocumentKind.Company, identifier);
        }
    }

    // The company's identifier, or null when the file can't be read.
    private static string? IdentifierOf(string filePath)
    {
        try
        {
            return JsonSerializer.Deserialize(File.ReadAllText(filePath), JsonContracts.For<SavedCompanyFile>(JsonOptions))?.CompanyIdentifier;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The company's source factions, or failing that the factions its entries come from.</summary>
    private static IEnumerable<int> SourceFactionIds(SavedCompanyFile payload)
    {
        var fromFactions = payload.SourceFactions.Select(f => f.FactionId).Where(id => id > 0).Distinct().ToList();
        return fromFactions.Count > 0
            ? fromFactions
            : payload.Entries.Select(e => e.SourceFactionId).Where(id => id > 0).Distinct();
    }
}
