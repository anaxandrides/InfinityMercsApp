using System.Globalization;
using System.Text.Json;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Records;

/// <summary>Lists and deletes saved company files.</summary>
public sealed class CompanyRecordCatalog(RecordPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
                var payload = JsonSerializer.Deserialize<SavedCompanyFile>(await File.ReadAllTextAsync(filePath), JsonOptions);
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

                records.Add(new CompanyRecord(filePath, name, payload.CompanyType, factionIcons, created));
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
        if (File.Exists(record.FilePath))
        {
            File.Delete(record.FilePath);
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
