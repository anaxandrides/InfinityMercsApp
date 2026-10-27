using InfinityMercsApp.Infrastructure.Json.Converters;
using InfinityMercsApp.Infrastructure.Json.Dtos;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class FireteamSpecConverterTests
{
    [Fact]
    public void Read_Object_KeepsEntriesInOrder()
    {
        var spec = ConverterJson.Read<FireteamSpecDto>(new FireteamSpecConverter(), "{\"CORE\":1,\"HARIS\":\"2\",\"DUO\":null}");

        Assert.Equal(["CORE", "HARIS", "DUO"], spec?.Entries.Select(entry => entry.Key));
        Assert.Equal([1, 2, null], spec?.Entries.Select(entry => entry.Value.AsInt32()));
    }

    [Theory]
    [InlineData("[1]")]
    [InlineData("\"CORE\"")]
    public void Read_NotAnObject_ReadsEmpty(string json) =>
        Assert.Empty(ConverterJson.Read<FireteamSpecDto>(new FireteamSpecConverter(), json)?.Entries ?? [new("x", default)]);

    [Fact]
    public void Write_Read_RoundTrips()
    {
        const string json = "{\"CORE\":1,\"HARIS\":\"2\"}";
        var spec = ConverterJson.Read<FireteamSpecDto>(new FireteamSpecConverter(), json);

        Assert.Equal(json, ConverterJson.Write(new FireteamSpecConverter(), spec));
    }
}
