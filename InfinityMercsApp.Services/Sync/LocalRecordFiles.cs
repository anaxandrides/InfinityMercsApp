using System.Text.Json;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Records;

namespace InfinityMercsApp.Services.Sync;

/// <summary>Company and season files as sync reads and writes them, without noting the writes in the outbox.</summary>
internal sealed class LocalRecordFiles(RecordPaths paths)
{
    /// <summary>The file's content, or null when it is missing or can't be read.</summary>
    public static TContent? Read<TContent>(string filePath)
        where TContent : class
    {
        try
        {
            return File.Exists(filePath) ? JsonSerializer.Deserialize<TContent>(File.ReadAllText(filePath), RecordJson.ReadOptions) : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"'{filePath}' couldn't be read to sync: {ex.Message}");
            return null;
        }
    }

    public static void Write<TContent>(string filePath, TContent content)
        where TContent : class
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? ".");
        File.WriteAllText(filePath, JsonSerializer.Serialize(content, OptionsFor(content)));
    }

    public static void Delete(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <summary>Whether two versions of a file hold the same thing.</summary>
    public static bool Same<TContent>(TContent here, TContent other)
        where TContent : class =>
        JsonSerializer.Serialize(here, RecordJson.CompanyWriteOptions) == JsonSerializer.Serialize(other, RecordJson.CompanyWriteOptions);

    /// <summary>The company's name, which names a company or a season to the user.</summary>
    public static string NameOf(object? content) => content switch
    {
        SavedCompanyFile company => company.CompanyName,
        SeasonFile season => season.CompanyName,
        _ => string.Empty
    };

    /// <summary>Where a file fetched from another device is written.</summary>
    public string NewPathFor(UserDocumentKind kind, string documentId, object content) => kind == UserDocumentKind.Company
        ? Path.Combine(paths.CompaniesDirectory, CompanyFounder.NextCompanyFileName(paths.CompaniesDirectory, NameOf(content)))
        : Path.Combine(paths.SeasonsDirectory, $"season-{documentId}.json");

    /// <summary>
    /// Every company and season file with its id. A season saved before seasons had ids is given one here, so it can
    /// be sent.
    /// </summary>
    public IReadOnlyList<LocalRecordFile> All()
    {
        var files = new List<LocalRecordFile>();
        foreach (var filePath in JsonFilesIn(paths.CompaniesDirectory))
        {
            if (Read<SavedCompanyFile>(filePath) is { CompanyIdentifier.Length: > 0 } company)
            {
                files.Add(new LocalRecordFile(UserDocumentKind.Company, company.CompanyIdentifier, filePath));
            }
        }

        foreach (var filePath in JsonFilesIn(paths.SeasonsDirectory))
        {
            if (Read<SeasonFile>(filePath) is not { } season)
            {
                continue;
            }

            if (string.IsNullOrEmpty(season.SeasonIdentifier))
            {
                season.SeasonIdentifier = Guid.NewGuid().ToString("N");
                Write(filePath, season);
            }

            files.Add(new LocalRecordFile(UserDocumentKind.Season, season.SeasonIdentifier, filePath));
        }

        return files;
    }

    private static IEnumerable<string> JsonFilesIn(string directory) =>
        Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*.json") : [];

    private static JsonSerializerOptions OptionsFor(object content) =>
        content is SeasonFile ? RecordJson.SeasonWriteOptions : RecordJson.CompanyWriteOptions;
}
