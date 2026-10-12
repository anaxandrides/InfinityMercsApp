using System.Text.Json;
using InfinityMercsApp.UnitTests.Legacy;

namespace InfinityMercsApp.UnitTests.Characterization;

/// <summary>
/// Pins what the existing hand-written JSON rules produce. Real-payload cases use units from
/// <c>army-101.json</c> (values cross-checked against the raw payload); synthetic cases pin the
/// tolerance rules (string numbers, object ids, fallbacks) that real data never exercises.
/// </summary>
public sealed class LegacyJsonRulesCharacterizationTests
{
    private const int DrummersUnitId = 1806;
    private const int MaximusUnitId = 1720;

    [Fact]
    public void Army101_ContainsTheExpectedNumberOfUnits()
    {
        Assert.Equal(100, ReferenceCorpus.Army101.Count);
    }

    [Fact]
    public void Drummers_OptionWithTwoIncludes_CollectsEntriesFromBothIncludedOptionsInOrder()
    {
        var (root, group, option) = FindOption(DrummersUnitId, groupId: 1, optionId: 7);

        var weaponIds = Ids(LegacyCompanyProfileOptionService.GetOptionEntriesWithIncludes(root, option, "weapons"));
        var peripheralIds = Ids(LegacyCompanyProfileOptionService.GetOptionEntriesWithIncludes(root, option, "peripheral"));

        // Own weapons [84, 72, 69, 5], then group 2/option 1 [4, 71], then group 2/option 5 [57, 72, 71].
        Assert.Equal(new[] { 84, 72, 69, 5, 4, 71, 57, 72, 71 }, weaponIds);
        Assert.Equal(new[] { 307, 311 }, peripheralIds);
        Assert.Equal(new[] { 307, 311 }, Ids(LegacyCompanyProfileOptionService.GetDisplayPeripheralEntriesForOption(root, group, option)));
        Assert.Equal("47", LegacyCompanyProfileOptionService.ReadOptionCost(option));
        Assert.Equal("47", LegacyCompanyProfileOptionService.ReadAdjustedOptionCost(root, group, option));
        Assert.Equal("1", LegacyCompanyProfileOptionService.ReadOptionSwc(option));
        Assert.Equal(1, LegacyCompanyProfileOptionService.ReadOptionMinis(option));
    }

    [Fact]
    public void Drummers_ControllerGroupIsDetectedFromIncludedPeripherals_PeripheralGroupIsNot()
    {
        using var doc = JsonDocument.Parse(ReferenceCorpus.Army101Unit(DrummersUnitId).ProfileGroupsJson);
        var groups = doc.RootElement.EnumerateArray().ToList();

        Assert.True(LegacyCompanyProfileOptionService.IsControllerGroup(doc.RootElement, groups[0]));
        Assert.False(LegacyCompanyProfileOptionService.IsControllerGroup(doc.RootElement, groups[1]));
    }

    [Fact]
    public void Maximus_LieutenantIsRecognisedFromTheLieutenantOrder()
    {
        var unit = ReferenceCorpus.Army101Unit(MaximusUnitId);
        var skills = CorpusLookups.For(unit.ArmyFiltersJson).Skills;
        var (_, _, regular) = FindOption(MaximusUnitId, groupId: 1, optionId: 1);
        var (_, _, lieutenant) = FindOption(MaximusUnitId, groupId: 1, optionId: 2);

        Assert.False(LegacySharedUtilities.IsLieutenantOption(regular, skills));
        Assert.True(LegacySharedUtilities.IsLieutenantOption(lieutenant, skills));
        Assert.True(LegacySharedUtilities.HasLieutenantOrder(lieutenant));
        Assert.True(LegacyCompanyUnitFilterService.UnitHasLieutenantOption(unit.ProfileGroupsJson, skills));
    }

    [Fact]
    public void Army101_FifteenUnitsOfferALieutenantOption()
    {
        var count = ReferenceCorpus.Army101.Count(unit =>
            LegacyCompanyUnitFilterService.UnitHasLieutenantOption(
                unit.ProfileGroupsJson,
                CorpusLookups.For(unit.ArmyFiltersJson).Skills));

        Assert.Equal(15, count);
    }

    [Fact]
    public void Filter_WeaponTermMatchesAnOptionThroughItsIncludes()
    {
        var unit = ReferenceCorpus.Army101Unit(DrummersUnitId);
        var lookups = CorpusLookups.For(unit.ArmyFiltersJson);
        var criteria = new UnitFilterCriteria
        {
            Terms = [new UnitFilterTerm(UnitFilterField.Weapons, ["Heavy Flamethrower"])]
        };

        // Heavy Flamethrower (57) only reaches DRUMMER options via an include of group 2 / option 5.
        Assert.True(UnitHasVisibleOption(unit, lookups, criteria));
        Assert.False(UnitHasVisibleOption(unit, lookups, new UnitFilterCriteria
        {
            Terms = [new UnitFilterTerm(UnitFilterField.Weapons, ["Heavy Flamethrower", "Marksman Rifle"], UnitFilterMatchMode.All)],
            MaxPoints = 20
        }));
    }

    [Theory]
    [InlineData("""{"swc":"1.5"}""", "1.5")]
    [InlineData("""{"swc":1.5}""", "1.5")]
    [InlineData("""{"swc":"1.50"}""", "1.50")]
    [InlineData("""{"swc":""}""", "")]
    [InlineData("""{"swc":null}""", "-")]
    [InlineData("""{}""", "-")]
    public void ReadOptionSwc_NormalisesNumbersAndStrings(string json, string expected)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expected, LegacyCompanyProfileOptionService.ReadOptionSwc(doc.RootElement));
    }

    [Theory]
    [InlineData("""{"points":25}""", "25")]
    [InlineData("""{"points":"25"}""", "25")]
    [InlineData("""{"points":"  "}""", "-")]
    [InlineData("""{"points":2.5,"cost":30}""", "30")]
    [InlineData("""{"points":null,"pts":"12"}""", "12")]
    [InlineData("""{"cost":"x7"}""", "x7")]
    [InlineData("""{}""", "-")]
    public void ReadOptionCost_FallsBackFromPointsToCostToPts(string json, string expected)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expected, LegacyCompanyProfileOptionService.ReadOptionCost(doc.RootElement));
    }

    [Theory]
    [InlineData("""{"minis":3}""", 3)]
    [InlineData("""{"minis":"2"}""", 2)]
    [InlineData("""{"minis":-4}""", 0)]
    [InlineData("""{"minis":true}""", 0)]
    [InlineData("""{}""", 0)]
    public void ReadOptionMinis_ClampsAndParsesStrings(string json, int expected)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expected, LegacyCompanyProfileOptionService.ReadOptionMinis(doc.RootElement));
    }

    [Theory]
    [InlineData("""5""", 1)]
    [InlineData("""{"id":5}""", 1)]
    [InlineData("""{"id":5,"q":3}""", 3)]
    [InlineData("""{"id":5,"q":"2"}""", 2)]
    [InlineData("""{"id":5,"q":0}""", 1)]
    [InlineData("""{"id":5,"q":"many"}""", 1)]
    public void ReadEntryQuantity_DefaultsToOne(string json, int expected)
    {
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(expected, LegacyCompanyProfileOptionService.ReadEntryQuantity(doc.RootElement));
    }

    [Theory]
    [InlineData("""7""", true, 7)]
    [InlineData(""" "7" """, true, 7)]
    [InlineData("""{"id":7}""", true, 7)]
    [InlineData("""{"id":"7"}""", true, 7)]
    [InlineData("""{"id":{"id":7}}""", true, 7)]
    [InlineData("""7.5""", false, 0)]
    [InlineData("""{}""", false, 0)]
    [InlineData("""null""", false, 0)]
    public void TryParseId_AcceptsNumbersStringsAndIdObjects(string json, bool expectedOk, int expectedId)
    {
        using var doc = JsonDocument.Parse(json);
        var ok = LegacySharedUtilities.TryParseId(doc.RootElement, out var id);
        Assert.Equal(expectedOk, ok);
        Assert.Equal(expectedId, id);
    }

    [Fact]
    public void AdjustedCost_RemovesPerModelCostForCollapsedDuplicatePeripherals()
    {
        // 3 minis for 30 points, 2 peripherals displayed: one duplicate collapsed → 30 - (2 - 1) * 10.
        const string json = """
            [{"id":1,"profiles":[],"options":[{"id":1,"points":30,"minis":3,"peripheral":[{"id":9,"q":2}]}]}]
            """;
        using var doc = JsonDocument.Parse(json);
        var group = doc.RootElement[0];
        var option = group.GetProperty("options")[0];

        Assert.Equal("20", LegacyCompanyProfileOptionService.ReadAdjustedOptionCost(doc.RootElement, group, option));
    }

    [Theory]
    [InlineData(31, "31")] // not divisible by minis → unchanged
    [InlineData(0, "0")]   // non-positive base → unchanged
    public void AdjustedCost_LeavesIndivisibleOrZeroCostsAlone(int points, string expected)
    {
        var json = $$"""
            [{"id":1,"profiles":[],"options":[{"id":1,"points":{{points}},"minis":3,"peripheral":[{"id":9,"q":2}]}]}]
            """;
        using var doc = JsonDocument.Parse(json);
        var group = doc.RootElement[0];
        var option = group.GetProperty("options")[0];

        Assert.Equal(expected, LegacyCompanyProfileOptionService.ReadAdjustedOptionCost(doc.RootElement, group, option));
    }

    [Fact]
    public void Peripherals_AreFilteredToTheControllerProfileIds_WhenTheProfileDeclaresThem()
    {
        const string json = """
            [{"id":1,"profiles":[{"peripheral":[{"id":9}]}],
              "options":[{"id":1,"peripheral":[{"id":9},{"id":8}]},{"id":2,"peripheral":[{"id":8}]}]}]
            """;
        using var doc = JsonDocument.Parse(json);
        var group = doc.RootElement[0];
        var options = group.GetProperty("options");

        Assert.Equal(new[] { 9 }, Ids(LegacyCompanyProfileOptionService.GetDisplayPeripheralEntriesForOption(doc.RootElement, group, options[0])));
        // Nothing survives the filter → falls back to the controller profile's own peripheral entries.
        Assert.Equal(new[] { 9 }, Ids(LegacyCompanyProfileOptionService.GetDisplayPeripheralEntriesForOption(doc.RootElement, group, options[1])));
        Assert.True(LegacyCompanyProfileOptionService.IsControllerGroup(doc.RootElement, group));
    }

    [Fact]
    public void Includes_StopAtCycles_ButTheRootOptionIsCollectedAgainWhenReachedByReference()
    {
        // Option 1 includes option 2, which includes option 1 again. The root is tracked by content,
        // not by group:option, so the first re-entry into option 1 is collected once more.
        const string json = """
            [{"id":1,"options":[
                {"id":1,"weapons":[{"id":10}],"includes":[{"group":1,"option":2}]},
                {"id":2,"weapons":[{"id":20}],"includes":[{"group":"1","option":{"id":1}}]}]}]
            """;
        using var doc = JsonDocument.Parse(json);
        var option = doc.RootElement[0].GetProperty("options")[0];

        Assert.Equal(new[] { 10, 20, 10 }, Ids(LegacyCompanyProfileOptionService.GetOptionEntriesWithIncludes(doc.RootElement, option, "weapons")));
    }

    [Fact]
    public void Includes_ResolveToTheFirstGroupWithAMatchingOption()
    {
        const string json = """
            [{"id":1,"options":[{"id":1,"includes":[{"group":2,"option":5}]}]},
             {"id":2,"options":[{"id":4,"skills":[{"id":40}]}]},
             {"id":2,"options":[{"id":5,"skills":[{"id":50}]}]}]
            """;
        using var doc = JsonDocument.Parse(json);
        var option = doc.RootElement[0].GetProperty("options")[0];

        Assert.Equal(new[] { 50 }, Ids(LegacyCompanyProfileOptionService.GetOptionEntriesWithIncludes(doc.RootElement, option, "skills")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{"not":"an array"}""")]
    public void UnitLevelQueries_ReturnFalseForMissingOrInvalidJson(string? json)
    {
        var skills = new Dictionary<int, string> { [119] = "Lieutenant" };
        Assert.False(LegacyCompanyUnitFilterService.UnitHasLieutenantOption(json, skills));
        Assert.False(LegacyCompanyUnitFilterService.UnitHasVisibleOption(json, skills, false, false));
    }

    [Theory]
    [InlineData("[101, \"102\", \"x\", 103.5]", new[] { 101, 102 })]
    [InlineData("""{"sourceFactionId":1001}""", new int[0])]
    [InlineData("not json", new int[0])]
    public void ParseFactionIds_KeepsIntegerAndNumericStringEntries(string json, int[] expected)
    {
        Assert.Equal(expected, LegacySharedUtilities.ParseFactionIds(json).ToArray());
    }

    [Fact]
    public void ReadMoveFromProfile_JoinsTheMoveArray()
    {
        using var doc = JsonDocument.Parse("""{"move":[10,"5",null]}""");
        Assert.Equal("10-5", LegacySharedUtilities.ReadMoveFromProfile(doc.RootElement));
    }

    [Fact]
    public void ReadIntAsString_LooksInsideNestedStatContainersAndCaseVariants()
    {
        using var doc = JsonDocument.Parse("""{"CC":"14","stats":{"bs":12}}""");
        Assert.Equal("14", LegacySharedUtilities.ReadIntAsString(doc.RootElement, "cc"));
        Assert.Equal("12", LegacySharedUtilities.ReadIntAsString(doc.RootElement, "bs"));
        Assert.Equal("-", LegacySharedUtilities.ReadIntAsString(doc.RootElement, "ph"));
    }

    private static bool UnitHasVisibleOption(CorpusUnit unit, CorpusLookups lookups, UnitFilterCriteria criteria) =>
        LegacyCompanyUnitFilterService.UnitHasVisibleOptionWithFilter(
            unit.ProfileGroupsJson,
            lookups.Skills,
            lookups.Chars,
            lookups.Equip,
            lookups.Weapons,
            lookups.Ammo,
            criteria,
            requireLieutenant: false,
            requireZeroSwc: false);

    private static (JsonElement Root, JsonElement Group, JsonElement Option) FindOption(int unitId, int groupId, int optionId)
    {
        // Parse into a document that lives for the test run; JsonElement.Clone keeps it alive.
        var root = JsonDocument.Parse(ReferenceCorpus.Army101Unit(unitId).ProfileGroupsJson).RootElement.Clone();
        foreach (var group in root.EnumerateArray())
        {
            if (group.GetProperty("id").GetInt32() != groupId)
            {
                continue;
            }

            foreach (var option in group.GetProperty("options").EnumerateArray())
            {
                if (option.GetProperty("id").GetInt32() == optionId)
                {
                    return (root, group, option);
                }
            }
        }

        throw new InvalidOperationException($"Unit {unitId} has no group {groupId} option {optionId}.");
    }

    private static int[] Ids(IEnumerable<JsonElement> entries) =>
        entries.Select(entry => LegacySharedUtilities.TryParseId(entry, out var id) ? id : -1).ToArray();
}
