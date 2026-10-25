namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class RelaxedNullableInt32ConverterTests
{
    [Theory]
    [InlineData("4", 4)]
    [InlineData("\"12\"", 12)]
    [InlineData("\"\"", null)]
    [InlineData("\"x\"", null)]
    [InlineData("1.5", null)]
    [InlineData("null", null)]
    [InlineData("false", null)]
    public void Read_Values_ReadsAnIntegerOrNull(string json, int? expected) =>
        Assert.Equal(expected, ConverterJson.Read<int?>(new RelaxedNullableInt32Converter(), json));

    [Theory]
    [InlineData(3, "3")]
    [InlineData(null, "null")]
    public void Write_Value_WritesNumberOrNull(int? value, string expected) =>
        Assert.Equal(expected, ConverterJson.Write(new RelaxedNullableInt32Converter(), value));
}
