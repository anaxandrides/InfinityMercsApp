using System.Text.Json;
using System.Text.Json.Nodes;
using InfinityMercsApp.Services.Records;

namespace InfinityMercsApp.UnitTests.Support;

/// <summary>A throwaway MercenaryRecords folder.</summary>
internal sealed class TempRecords : IDisposable
{
    public TempRecords()
    {
        Paths = new RecordPaths(Path.Combine(Path.GetTempPath(), "mercs-tests-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(Paths.SeasonsDirectory);
    }

    public RecordPaths Paths { get; }

    /// <summary>
    /// Writes a company file. Fields every saved company has but <paramref name="json"/> leaves out are filled in, so a
    /// test writes only what it is about.
    /// </summary>
    public string WriteCompany(string name, string json) => Write(Paths.CompaniesDirectory, name, Completed(json, CompanyFields()));

    /// <summary>Writes a season file, filling in the fields every saved season has, as <see cref="WriteCompany"/> does.</summary>
    public string WriteSeason(string name, string json) => Write(Paths.SeasonsDirectory, name, Completed(json, SeasonFields()));

    private static JsonObject CompanyFields() => new()
    {
        ["companyName"] = string.Empty,
        ["companyType"] = string.Empty,
        ["companyIdentifier"] = string.Empty,
        ["sourceFactions"] = new JsonArray(),
        ["entries"] = new JsonArray()
    };

    private static JsonObject SeasonFields() => new()
    {
        ["companyName"] = string.Empty,
        ["companyIdentifier"] = string.Empty,
        ["rounds"] = new JsonArray()
    };

    // The file with each of the required fields it lacks, matched ignoring case as the app reads them. A malformed
    // file is written as it is.
    private static string Completed(string json, JsonObject requiredFields)
    {
        if (ParsedObject(json) is not { } file)
        {
            return json;
        }

        foreach (var (field, value) in requiredFields)
        {
            if (!file.Any(property => string.Equals(property.Key, field, StringComparison.OrdinalIgnoreCase)))
            {
                file[field] = value?.DeepClone();
            }
        }

        return file.ToJsonString();
    }

    private static JsonObject? ParsedObject(string json)
    {
        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Write(string directory, string name, string json)
    {
        var path = Path.Combine(directory, name);
        File.WriteAllText(path, json);
        return path;
    }

    public void Dispose() => Directory.Delete(Paths.CompaniesDirectory, recursive: true);
}
