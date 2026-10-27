using InfinityMercsApp.Infrastructure.Json.Converters;
using InfinityMercsApp.Infrastructure.Json.Dtos;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class ProfileEntryListConverterTests
{
    private static List<ProfileEntryDto>? Read(string json) => ConverterJson.Read<List<ProfileEntryDto>>(new ProfileEntryListConverter(), json);

    [Fact]
    public void Read_BareIds_KeepsEntriesWithoutIds()
    {
        var entries = Read("[5,\"6\",\"x\"]");

        Assert.Equal([5, 6, null], entries?.Select(entry => entry.Id));
        Assert.All(entries ?? [], entry => Assert.False(entry.IsObject));
    }

    [Fact]
    public void Read_Object_ReadsItsFields()
    {
        var entry = Read("[{\"id\":5,\"q\":2,\"order\":\"1\",\"extra\":[7,\"x\"],\"other\":{\"a\":1}}]")?.Single();

        Assert.NotNull(entry);
        Assert.True(entry.IsObject);
        Assert.Equal(5, entry.Id);
        Assert.Equal(2, entry.Q.AsInt32());
        Assert.Equal(1, entry.Order.AsInt32());
        Assert.Equal([7, null], entry.Extra);
    }

    [Fact]
    public void Read_ObjectWithoutFields_LeavesThemMissing()
    {
        var entry = Read("[{\"id\":5,\"extra\":3}]")?.Single();

        Assert.NotNull(entry);
        Assert.True(entry.Q.IsMissing);
        Assert.True(entry.Order.IsMissing);
        Assert.Null(entry.Extra);
    }

    [Fact]
    public void Read_DuplicateId_LastWins() => Assert.Equal(2, Read("[{\"id\":1,\"id\":2}]")?.Single().Id);

    [Fact]
    public void Read_NotAnArray_ReadsNull() => Assert.Null(Read("{\"id\":1}"));

    [Theory]
    [InlineData("[5,null]")]
    [InlineData("[{\"id\":5,\"q\":2,\"order\":\"1\",\"extra\":[7,null]}]")]
    [InlineData("[{}]")]
    public void Write_Read_RoundTrips(string json) =>
        Assert.Equal(json, ConverterJson.Write(new ProfileEntryListConverter(), Read(json)));

    [Fact]
    public void Write_Null_WritesNull() =>
        Assert.Equal("null", ConverterJson.Write<List<ProfileEntryDto>?>(new ProfileEntryListConverter(), null));
}
