using System.Text.Json;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Stored id arrays (spec-ops <c>ExtrasJson</c>) read through the typed reader match the legacy per-element id parsing.
/// </summary>
public sealed class ArmyJsonReaderIdListTests
{
    public static TheoryData<string?> StoredExtrasJson()
    {
        var dbPath = RepoFiles.Resolve(ReferenceCorpus.DatabasePath);
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT ExtrasJson FROM specops_skills UNION SELECT DISTINCT ExtrasJson FROM specops_equipment";
        using var reader = command.ExecuteReader();
        var values = new TheoryData<string?>();
        while (reader.Read())
        {
            values.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
        }

        values.Add("[\"5\", {\"id\":6}, null, \"x\", 7.5, 8]");
        values.Add("{\"id\":1}");
        values.Add("not json");
        values.Add("[1] trailing");
        return values;
    }

    [Theory]
    [MemberData(nameof(StoredExtrasJson))]
    public void ReadIdList_MatchesLegacyElementParsing(string? json)
    {
        Assert.Equal(LegacyIds(json), new ArmyJsonReader().ReadIdList(json));
    }

    private static List<int> LegacyIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var ids = new List<int>();
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (LegacySharedUtilities.TryParseId(element, out var id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
