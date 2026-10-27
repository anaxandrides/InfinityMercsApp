using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Json;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json;

/// <summary>The army reader turns blank or malformed blobs into empty values, and finds movement and stats however a payload spells them.</summary>
public sealed class ArmyJsonReaderLenienceTests
{
    private readonly ArmyJsonReader _reader = new();

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("{not json")]
    public void Readers_BlankOrMalformed_ReadAsEmpty(string? json)
    {
        Assert.True(_reader.ReadProfileGroups(json).IsEmpty);
        Assert.Same(Domain.Models.Army.Filters.UnitFilterIds.Empty, _reader.ReadUnitFilters(json));
        Assert.Empty(_reader.ReadArmyFilters(json).Sections);
        Assert.Empty(_reader.ReadFactionIds(json));
        Assert.Empty(_reader.ReadFireteamChart(json).Teams);
        Assert.Empty(_reader.ReadUnitOptions(json));
        Assert.Empty(_reader.ReadRelations(json));
        Assert.Null(_reader.ReadReinforcements(json));
        Assert.Same(Domain.Models.Army.SpecOps.SpecopsCatalog.Empty, _reader.ReadSpecops(json));
        Assert.Null(_reader.ReadArmyPayload(json));
    }

    [Theory]
    [InlineData("[1,2] 3")]
    [InlineData("{\"faction\":1}")]
    public void ReadFactionIds_NotOneArray_ReadsEmpty(string json) => Assert.Empty(_reader.ReadFactionIds(json));

    [Fact]
    public void ReadFactionIds_Array_ReadsTheIds() => Assert.Equal([101, 102], _reader.ReadFactionIds("[101,\"102\"]"));

    [Fact]
    public void ReadArmyPayload_Null_ReadsNull() => Assert.Null(_reader.ReadArmyPayload("null"));

    [Fact]
    public void ReadUnitFilters_Null_ReadsEmpty() =>
        Assert.Same(Domain.Models.Army.Filters.UnitFilterIds.Empty, _reader.ReadUnitFilters("null"));

    [Fact]
    public void ReadReinforcements_NumericString_ReadsTheFaction() => Assert.Equal(1001, _reader.ReadReinforcements("\"1001\""));

    [Fact]
    public void ReadProfileGroups_SameJsonTwice_ReturnsTheCachedResult()
    {
        const string json = """[{"id":1,"profiles":[{"id":1,"name":"A"}],"options":[]}]""";

        Assert.Same(_reader.ReadProfileGroups(json), _reader.ReadProfileGroups(json));
    }

    [Theory]
    [InlineData("\"move\":[10,5]", "10-5")]
    [InlineData("\"move\":[\"x\"],\"mov\":\"15-5\"", "15-5")]
    [InlineData("\"MOVE\":[\"10\",5]", "10-5")]
    [InlineData("\"mov\":[10,\"5\"]", "10-5")]
    [InlineData("\"mov\":[true]", "-")]
    [InlineData("\"mov\":\"10-10\"", "10-10")]
    [InlineData("\"mov\":4", "-")]
    [InlineData("\"stats\":{\"mov\":\"5-5\"}", "5-5")]
    [InlineData("", "-")]
    public void ReadProfileGroups_MoveSpellings_ReadTheMove(string move, string expected) =>
        Assert.Equal(expected, Profile(move).Move);

    [Theory]
    [InlineData("\"move\":\"10-5\"", "10,5")]
    [InlineData("\"Move\":\"10-x-5\"", "10,5")]
    [InlineData("\"mov\":[10,\"5\",true]", "10,5")]
    [InlineData("\"mov\":4", "")]
    public void ReadProfileGroups_MoveSpellings_ReadTheCentimetres(string move, string expected) =>
        Assert.Equal(expected, string.Join(",", Profile(move).MoveCm));

    [Theory]
    [InlineData("\"cc\":\"14\"", 14)]
    [InlineData("\"CC\":15", 15)]
    [InlineData("\"Cc\":\"16\"", 16)]
    [InlineData("\"attributes\":{\"CC\":17}", 17)]
    [InlineData("\"attrs\":[1],\"Stats\":{\"cc\":18}", 18)]
    [InlineData("\"CC\":true", null)]
    [InlineData("\"stats\":{\"bs\":1}", null)]
    public void ReadProfileGroups_StatSpellings_ReadTheStat(string stat, int? expected) =>
        Assert.Equal(expected, Profile(stat).Cc);

    [Fact]
    public void ReadProfileGroups_UndeclaredVita_ReadsIt() => Assert.Equal(1, Profile("\"VITA\":\"1\"").Vita);

    private UnitProfile Profile(string properties)
    {
        var separator = properties.Length == 0 ? string.Empty : ",";
        var groups = _reader.ReadProfileGroups($$"""[{"id":1,"profiles":[{"id":1,"name":"A"{{separator}}{{properties}}}],"options":[]}]""");
        return Assert.Single(Assert.Single(groups.Groups).Profiles);
    }
}
