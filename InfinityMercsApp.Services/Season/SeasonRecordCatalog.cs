using System.Globalization;
using System.Text.Json;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Services.Records;

namespace InfinityMercsApp.Services.Season;

/// <summary>Lists the saved seasons and finds the company file each belongs to.</summary>
public sealed class SeasonRecordCatalog(RecordPaths paths)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = RecordJsonContext.Default };

    /// <summary>
    /// Every readable season file, newest first. A season whose company path is missing or names no file here, as
    /// for a season made on another device, is matched to a saved company by identifier; malformed files are skipped.
    /// </summary>
    public async Task<IReadOnlyList<SeasonRecord>> ListAsync()
    {
        if (!Directory.Exists(paths.SeasonsDirectory))
        {
            return [];
        }

        var records = new List<SeasonRecord>();
        foreach (var filePath in Directory.EnumerateFiles(paths.SeasonsDirectory, "*.json"))
        {
            try
            {
                var header = JsonSerializer.Deserialize(await File.ReadAllTextAsync(filePath), JsonContracts.For<SeasonFileHeader>(JsonOptions));
                if (header is null)
                {
                    continue;
                }

                var companyName = string.IsNullOrWhiteSpace(header.CompanyName)
                    ? Path.GetFileNameWithoutExtension(filePath)
                    : header.CompanyName;

                var companyFilePath = header.CompanyFilePath;
                if ((string.IsNullOrWhiteSpace(companyFilePath) || !File.Exists(companyFilePath)) && !string.IsNullOrWhiteSpace(header.CompanyIdentifier))
                {
                    companyFilePath = await FindCompanyFileAsync(header.CompanyIdentifier);
                }

                var started = DateTimeOffset.TryParse(header.CreatedDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var created)
                    ? created
                    : new DateTimeOffset(File.GetLastWriteTimeUtc(filePath), TimeSpan.Zero);

                records.Add(new SeasonRecord(filePath, companyFilePath ?? string.Empty, companyName, started) { SeasonIdentifier = header.SeasonIdentifier });
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // Skip malformed season files.
            }
        }

        return records.OrderByDescending(r => r.StartedUtc).ToList();
    }

    private async Task<string?> FindCompanyFileAsync(string companyIdentifier)
    {
        if (!Directory.Exists(paths.CompaniesDirectory))
        {
            return null;
        }

        foreach (var filePath in Directory.EnumerateFiles(paths.CompaniesDirectory, "*.json"))
        {
            try
            {
                var header = JsonSerializer.Deserialize(await File.ReadAllTextAsync(filePath), JsonContracts.For<CompanyFileHeader>(JsonOptions));
                if (string.Equals(header?.CompanyIdentifier, companyIdentifier, StringComparison.OrdinalIgnoreCase))
                {
                    return filePath;
                }
            }
            catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
            {
                // Skip unreadable files.
            }
        }

        return null;
    }
}
