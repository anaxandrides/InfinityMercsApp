using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Proves the typed reader reproduces the legacy hand-written JSON rules on every real unit and army.
/// </summary>
public sealed class ArmyJsonReaderEquivalenceTests
{
    private static readonly string[] LookupSections =
    [
        ArmyFilterSection.Chars,
        ArmyFilterSection.Skills,
        ArmyFilterSection.Equip,
        ArmyFilterSection.Weapons,
        ArmyFilterSection.Ammunition,
        ArmyFilterSection.Peripheral,
        ArmyFilterSection.Type,
        ArmyFilterSection.Extras,
        ArmyFilterSection.Category
    ];

    public static IEnumerable<object[]> CorpusSources =>
    [
        ["army-101.json"],
        ["infinitymercs.db3"]
    ];

    [Theory]
    [MemberData(nameof(CorpusSources))]
    public void ProfileGroups_ReadThroughTheTypedReader_MatchLegacyFacts(string source)
    {
        var reader = new ArmyJsonReader();
        var units = ReferenceCorpus.All.Where(unit => unit.Source == source).ToList();
        Assert.NotEmpty(units);

        var mismatches = new List<string>();
        foreach (var unit in units)
        {
            var expected = ProfileFacts.FromLegacy(unit);
            var actual = ProfileFacts.FromReader(unit, reader);
            if (expected != actual)
            {
                mismatches.Add($"{unit.Key}: {FirstDifference(expected, actual)}");
                if (mismatches.Count >= 10)
                {
                    break;
                }
            }
        }

        Assert.True(mismatches.Count == 0, string.Join(Environment.NewLine, mismatches));
    }

    [Fact]
    public void ArmyFilters_NameLinkAndExtraLookups_MatchLegacyBuilders()
    {
        var reader = new ArmyJsonReader();
        var armyFilters = ReferenceCorpus.All
            .Select(unit => unit.ArmyFiltersJson)
            .Where(json => !string.IsNullOrWhiteSpace(json))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        Assert.NotEmpty(armyFilters);

        foreach (var json in armyFilters)
        {
            var catalog = reader.ReadArmyFilters(json);
            foreach (var section in LookupSections)
            {
                AssertSameMap(LegacySharedUtilities.BuildIdNameLookup(json, section), catalog.Names(section), $"names[{section}]");
                AssertSameMap(LegacyViewerOptionRules.BuildIdLinkLookup(json, section), catalog.Links(section), $"links[{section}]");
            }

            var legacyExtras = LegacyViewerOptionRules.BuildExtrasLookup(json)
                .ToDictionary(x => x.Key, x => $"{x.Value.Name}|{x.Value.Type}|{x.Value.Url}");
            var extras = catalog.Extras.ToDictionary(x => x.Key, x => $"{x.Value.Name}|{x.Value.Type}|{x.Value.Url}");
            AssertSameMap(legacyExtras, extras, "extras");
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("""{"not":"an array"}""")]
    [InlineData("null")]
    [InlineData("[")]
    public void ProfileGroups_MissingOrInvalidJson_ReadsAsEmpty(string? json)
    {
        var groups = new ArmyJsonReader().ReadProfileGroups(json);

        Assert.True(groups.IsEmpty);
        Assert.False(groups.HasLieutenantOption(new Dictionary<int, string> { [119] = "Lieutenant" }));
    }

    [Theory]
    [InlineData("[101, \"102\", \"x\", 103.5]", new[] { 101, 102 })]
    [InlineData("""{"sourceFactionId":1001}""", new int[0])]
    [InlineData("not json", new int[0])]
    [InlineData("[1] trailing", new int[0])]
    [InlineData(null, new int[0])]
    public void FactionIds_MatchLegacyParsing(string? json, int[] expected)
    {
        Assert.Equal(expected, LegacySharedUtilities.ParseFactionIds(json).ToArray());
        Assert.Equal(expected, new ArmyJsonReader().ReadFactionIds(json).ToArray());
    }

    /// <summary>
    /// Synthetic payloads covering the tolerance rules real data never exercises, compared fact for fact.
    /// </summary>
    [Theory]
    [InlineData("""[{"id":"1","isc":5,"profiles":[{"id":{"id":2},"name":"P","CC":"14","stats":{"bs":12},"move":[10,"5",null],"str":"true","type":"3","skills":[7,"8",{"id":"9","q":"2","order":3,"extra":[1,"2",{"id":3},null]}]}],"options":[{"id":1,"name":"Lieutenant X","swc":1.5,"points":2.5,"cost":"30","minis":"3","peripheral":[{"id":9,"q":2}]}]}]""")]
    [InlineData("""[{"id":1,"options":[{"id":1,"weapons":[{"id":10}],"includes":[{"group":1,"option":2}]},{"id":2,"weapons":[{"id":20}],"includes":[{"group":"1","option":{"id":1}}]}]}]""")]
    [InlineData("""[{"id":1,"options":[{"id":1,"includes":[{"group":2,"option":5}, 7, {"group":"x","option":1}]}]},{"id":2,"options":[{"id":4,"skills":[{"id":40}]}]},{"id":2,"options":[{"id":5,"skills":[{"id":50}]}]}]""")]
    [InlineData("""[{"id":1,"profiles":[{"peripheral":[{"id":9}]}],"options":[{"id":1,"peripheral":[{"id":9},{"id":8}]},{"id":2,"peripheral":[{"id":8}]}]}]""")]
    [InlineData("""[{"id":1,"profiles":[],"options":[{"id":1,"points":30,"minis":3,"peripheral":[{"id":9,"q":2}]},{"id":2,"points":31,"minis":3,"peripheral":[{"id":9,"q":2}]},{"id":3,"pts":"12","swc":"","minis":-2,"orders":[{"type":"lieutenant"},{"type":5}]}]}]""")]
    [InlineData("""[{"id":1,"profiles":[{"mov":["4","x"],"cc":null}],"options":[{"id":1,"swc":null,"points":"  ","weapons":[{}, null, "w", {"id":3,"q":0}]}]}]""")]
    public void SyntheticPayloads_ReadThroughTheTypedReader_MatchLegacyFacts(string json)
    {
        var unit = new CorpusUnit("synthetic", "synthetic", 0, json, null, null, ReferenceCorpus.ReadArmy101FiltersJson());

        Assert.Equal(ProfileFacts.FromLegacy(unit), ProfileFacts.FromReader(unit, new ArmyJsonReader()));
    }

    [Fact]
    public void ReadProfileGroups_CachesByContent()
    {
        var reader = new ArmyJsonReader();
        var json = ReferenceCorpus.Army101Unit(1806).ProfileGroupsJson;

        var first = reader.ReadProfileGroups(json);
        var second = reader.ReadProfileGroups(new string(json.AsSpan()));

        Assert.Same(first, second);
    }

    [Fact]
    public void ReadUnitFilters_KeepsIntegerIdsPerSection()
    {
        var filters = new ArmyJsonReader().ReadUnitFilters(
            """{"categories":[3],"types":[1,2],"chars":[5],"skills":["7",8],"equip":[],"weapons":[84,null],"ammunition":{"bad":1}}""");

        Assert.Equal(new[] { 3 }, filters.Categories.ToArray());
        Assert.Equal(new[] { 1, 2 }, filters.Types.ToArray());
        Assert.Equal(new[] { 7, 8 }, filters.Skills.ToArray());
        Assert.Equal(new[] { 84 }, filters.Weapons.ToArray());
        Assert.Empty(filters.Ammunition);
    }

    private static void AssertSameMap<TValue>(
        IReadOnlyDictionary<int, TValue> expected,
        IReadOnlyDictionary<int, TValue> actual,
        string what)
    {
        var expectedText = string.Join(";", expected.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
        var actualText = string.Join(";", actual.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
        Assert.True(expectedText == actualText, $"{what} differs.\nexpected: {expectedText}\nactual:   {actualText}");
    }

    private static string FirstDifference(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        for (var i = 0; i < Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var e = i < expectedLines.Length ? expectedLines[i].TrimEnd('\r') : "<end>";
            var a = i < actualLines.Length ? actualLines[i].TrimEnd('\r') : "<end>";
            if (e != a)
            {
                return $"line {i + 1}: legacy '{e}' / reader '{a}'";
            }
        }

        return "none";
    }
}

/// <summary>
/// Stored id arrays (spec-ops <c>ExtrasJson</c>) read through the typed reader match the legacy per-element id parsing.
/// </summary>
public sealed class ArmyJsonReaderIdListTests
{
    public static IEnumerable<object[]> StoredExtrasJson()
    {
        var dbPath = RepoFiles.Resolve(ReferenceCorpus.DatabasePath);
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DISTINCT ExtrasJson FROM specops_skills UNION SELECT DISTINCT ExtrasJson FROM specops_equipment";
        using var reader = command.ExecuteReader();
        var values = new List<object[]>();
        while (reader.Read())
        {
            values.Add([reader.IsDBNull(0) ? null! : reader.GetString(0)]);
        }

        values.Add(["[\"5\", {\"id\":6}, null, \"x\", 7.5, 8]"]);
        values.Add(["{\"id\":1}"]);
        values.Add(["not json"]);
        values.Add(["[1] trailing"]);
        return values;
    }

    [Theory]
    [MemberData(nameof(StoredExtrasJson))]
    public void ReadIdList_MatchesLegacyElementParsing(string? json)
    {
        Assert.Equal(LegacyIds(json), new ArmyJsonReader().ReadIdList(json).ToArray());
    }

    private static int[] LegacyIds(string? json)
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

            return ids.ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
