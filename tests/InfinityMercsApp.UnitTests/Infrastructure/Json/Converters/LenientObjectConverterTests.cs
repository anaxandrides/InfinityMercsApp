using System.Text.Json;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class LenientObjectConverterTests
{
    private static NamedThing? Read(string thing) =>
        JsonSerializer.Deserialize<NamedThingHolder>($"{{\"Thing\":{thing}}}")?.Thing;

    [Fact]
    public void Read_Object_ReadsItsContract() => Assert.Equal("x", Read("{\"Name\":\"x\"}")?.Name);

    [Theory]
    [InlineData("[]")]
    [InlineData("\"x\"")]
    [InlineData("3")]
    [InlineData("null")]
    public void Read_NotAnObject_ReadsNull(string json) => Assert.Null(Read(json));

    [Fact]
    public void Write_Object_WritesItsContract() =>
        Assert.Equal("{\"Thing\":{\"Name\":\"x\"}}", JsonSerializer.Serialize(new NamedThingHolder { Thing = new NamedThing { Name = "x" } }));

    [Fact]
    public void Write_Null_WritesNull() =>
        Assert.Equal("{\"Thing\":null}", JsonSerializer.Serialize(new NamedThingHolder()));
}
