using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class ScalarTextConverterTests
{
    [Theory]
    [InlineData("\"abc\"", "abc")]
    [InlineData("3", "3")]
    [InlineData("1.25", "1.25")]
    public void Read_StringOrNumber_ReadsTheText(string json, string expected) =>
        Assert.Equal(expected, ConverterJson.Read<string>(new ScalarTextConverter(), json));

    [Theory]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("[1]")]
    [InlineData("{\"a\":1}")]
    public void Read_Other_ReadsNull(string json) =>
        Assert.Null(ConverterJson.Read<string>(new ScalarTextConverter(), json));

    [Fact]
    public void Write_Text_WritesAString() =>
        Assert.Equal("\"3\"", ConverterJson.Write<string?>(new ScalarTextConverter(), "3"));
}
