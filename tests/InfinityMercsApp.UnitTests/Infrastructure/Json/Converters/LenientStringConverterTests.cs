using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class LenientStringConverterTests
{
    [Fact]
    public void Read_String_ReadsIt() =>
        Assert.Equal("abc", ConverterJson.Read<string>(new LenientStringConverter(), "\"abc\""));

    [Theory]
    [InlineData("3")]
    [InlineData("null")]
    [InlineData("[\"a\"]")]
    [InlineData("{\"a\":\"b\"}")]
    public void Read_NotAString_ReadsNull(string json) =>
        Assert.Null(ConverterJson.Read<string>(new LenientStringConverter(), json));

    [Fact]
    public void Write_Null_WritesNull() =>
        Assert.Equal("null", ConverterJson.Write<string?>(new LenientStringConverter(), null));
}
