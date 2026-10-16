using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.Records;

namespace InfinityMercsApp.Services.Season;

/// <summary>Season files as indented JSON under the app's MercenaryRecords/Seasons folder.</summary>
internal sealed class SeasonFileStore(RecordPaths paths) : ISeasonFileService
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<string> CreateAsync(string companyName, string companyIdentifier, string companyFilePath)
    {
        var now = DateTimeOffset.UtcNow;
        var seasonsDir = paths.SeasonsDirectory;
        Directory.CreateDirectory(seasonsDir);

        var fileName = $"{companyName.Replace(' ', '_')}-{now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}.json";
        var filePath = Path.Combine(seasonsDir, fileName);

        var seasonFile = new SeasonFile
        {
            CompanyName = companyName,
            CompanyIdentifier = companyIdentifier,
            CompanyFilePath = companyFilePath,
            CreatedDate = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            InitialPurchases = new SeasonMarketplace(),
            Rounds = []
        };

        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(seasonFile, WriteOptions));
        return filePath;
    }

    public async Task<SeasonFile?> LoadAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<SeasonFile>(json, ReadOptions);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to load season file '{filePath}': {ex.Message}");
            return null;
        }
    }

    public async Task<bool> SaveAsync(string? filePath, SeasonFile? seasonFile)
    {
        if (string.IsNullOrWhiteSpace(filePath) || seasonFile is null)
        {
            return false;
        }

        try
        {
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(seasonFile, WriteOptions));
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to save season file '{filePath}': {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UpdateAsync(string? filePath, Action<SeasonFile> updater)
    {
        if (await LoadAsync(filePath) is not { } seasonFile)
        {
            return false;
        }

        updater(seasonFile);
        SeasonStatusUpdate.Apply(seasonFile);
        return await SaveAsync(filePath, seasonFile);
    }

    public async Task<bool> UpdateLatestRoundAsync(string? filePath, Action<SeasonRound> updater)
    {
        var seasonFile = await LoadAsync(filePath);
        if (SeasonRounds.Latest(seasonFile) is not { } latest || seasonFile is null)
        {
            return false;
        }

        updater(latest);
        SeasonStatusUpdate.Apply(seasonFile);
        return await SaveAsync(filePath, seasonFile);
    }
}
