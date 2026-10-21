using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army.Fireteams;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.UnitTests.Legacy;
using Microsoft.Data.Sqlite;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Proves the typed fireteam chart reproduces both legacy fireteam parsers (company team matching and the
/// unit viewer's fireteam list/counts) on every army's chart.
/// </summary>
public sealed class FireteamChartEquivalenceTests
{
    public static TheoryData<string, string?> Charts()
    {
        var charts = new TheoryData<string, string?>();
        using (var doc = JsonDocument.Parse(File.ReadAllText(RepoFiles.Resolve(ReferenceCorpus.Army101Path))))
        {
            charts.Add("army-101", doc.RootElement.GetProperty("fireteamChart").GetRawText());
        }

        using var connection = new SqliteConnection($"Data Source={RepoFiles.Resolve(ReferenceCorpus.DatabasePath)};Mode=ReadOnly");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT FactionId, FireteamChartJson FROM armies ORDER BY FactionId";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            charts.Add($"db:{reader.GetInt32(0)}", reader.IsDBNull(1) ? null : reader.GetString(1));
        }

        charts.Add("synthetic-loose", """
            {"spec":{"core":"2","Haris":9,"DUO":"x"},
             "teams":[
               {"name":"  Alpha  ","type":["CORE"," ","haris",5],"units":[
                 {"name":" ","slug":"  slug-a ","comment":" (FTO) ","min":"*","max":"3"},
                 {"name":"B","min":"1","max":2,"required":"true"},
                 {"slug":"","min":null,"required":false}]},
               {"name":"","type":"DUO","units":[]},
               {"type":["DUO"]}]}
            """);
        charts.Add("synthetic-not-arrays", """{"spec":[1,2],"teams":{"name":"x"}}""");
        charts.Add("synthetic-invalid", "not json");
        return charts;
    }

    [Theory]
    [MemberData(nameof(Charts))]
    public void ReadFireteamChart_CompanyEntries_MatchLegacyParser(string source, string? json)
    {
        var expected = LegacyCompanyFireteamParser.ParseEntries(json)
            .Select(entry => $"{entry.Name} d{entry.Duo} h{entry.Haris} c{entry.Core} :: " +
                             string.Join(" | ", entry.UnitLimits.Select(x => $"{x.Name}/{x.Min}/{x.Max}/{x.Slug ?? "<null>"}/{x.MinAsterisk}")));

        var actual = new ArmyJsonReader().ReadFireteamChart(json).Teams
            .Where(team => !string.IsNullOrWhiteSpace(team.CompanyName))
            .Select(team =>
            {
                var (duo, haris, core) = team.TypeCounts;
                return $"{team.CompanyName} d{duo} h{haris} c{core} :: " +
                       string.Join(" | ", team.Units.Select(x => $"{x.CompanyDisplayName}/{x.Min ?? 0}/{x.Max ?? 0}/{x.CompanySlug ?? "<null>"}/{x.IsMandatory}"));
            });

        Assert.Equal(expected, actual);
        Assert.NotNull(source);
    }

    [Theory]
    [MemberData(nameof(Charts))]
    public void ReadFireteamChart_ViewerListAndCounts_MatchLegacyViewer(string source, string? json)
    {
        var chart = new ArmyJsonReader().ReadFireteamChart(json);
        var (expectedTeams, expectedCounts) = LegacyViewer(json);

        var actualTeams = chart.Teams.Select(team =>
            $"{team.ViewerName} [{(string.IsNullOrWhiteSpace(team.TypesDisplay) ? "-" : team.TypesDisplay)}] " +
            string.Join(" | ", team.Units.Select(x => $"{x.ViewerName}/{x.Slug ?? string.Empty}/{x.Min ?? 0}/{x.Max ?? 0}")));
        var actualCounts = $"{chart.SpecDisplay(FireteamType.Duo)} {chart.SpecDisplay(FireteamType.Haris)} {chart.SpecDisplay(FireteamType.Core)}";

        Assert.Equal(expectedTeams, actualTeams);
        Assert.Equal(expectedCounts, actualCounts);
        Assert.NotNull(source);
    }

    /// <summary>The legacy viewer's UpdateFireteamTeams/UpdateFireteamCounts loops (mercs filter off) over its frozen helpers.</summary>
    private static (List<string> Teams, string Counts) LegacyViewer(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return ([], "- - -");
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            var counts = doc.RootElement.TryGetProperty("spec", out var spec) && spec.ValueKind == JsonValueKind.Object
                ? $"{LegacyViewerFireteamRules.ReadFireteamCount(spec, "DUO")} {LegacyViewerFireteamRules.ReadFireteamCount(spec, "HARIS")} {LegacyViewerFireteamRules.ReadFireteamCount(spec, "CORE")}"
                : "- - -";
            if (!doc.RootElement.TryGetProperty("teams", out var teams) || teams.ValueKind != JsonValueKind.Array)
            {
                return ([], counts);
            }

            var result = new List<string>();
            foreach (var team in teams.EnumerateArray())
            {
                var types = LegacyViewerFireteamRules.ReadTeamTypes(team);
                result.Add(
                    $"{LegacyViewerFireteamRules.ReadString(team, "name", "Unnamed Team")} [{(string.IsNullOrWhiteSpace(types) ? "-" : types)}] " +
                    string.Join(" | ", LegacyViewerFireteamRules.ReadUnitLimits(team).Select(x => $"{x.Name}/{x.Slug}/{x.Min}/{x.Max}")));
            }

            return (result, counts);
        }
        catch (JsonException)
        {
            return ([], "- - -");
        }
    }
}
