using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class FlexibleIdConverterTests
{
    [Theory]
    [InlineData("5", 5)]
    [InlineData("\"7\"", 7)]
    [InlineData("{\"id\":9}", 9)]
    [InlineData("{\"name\":\"x\",\"id\":{\"id\":\"11\"}}", 11)]
    [InlineData("{\"id\":1,\"id\":2}", 2)]
    public void Read_IdShapes_ReadsTheId(string json, int expected) =>
        Assert.Equal(expected, ConverterJson.Read<int?>(new FlexibleIdConverter(), json));

    [Theory]
    [InlineData("null")]
    [InlineData("1.5")]
    [InlineData("99999999999")]
    [InlineData("\"abc\"")]
    [InlineData("{\"name\":\"x\"}")]
    [InlineData("[1,2]")]
    [InlineData("true")]
    public void Read_NoUsableId_ReadsNull(string json) =>
        Assert.Null(ConverterJson.Read<int?>(new FlexibleIdConverter(), json));

    [Fact]
    public void Write_Value_WritesTheNumber() =>
        Assert.Equal("4", ConverterJson.Write<int?>(new FlexibleIdConverter(), 4));

    [Fact]
    public void Write_Null_WritesNull() =>
        Assert.Equal("null", ConverterJson.Write<int?>(new FlexibleIdConverter(), null));
}
