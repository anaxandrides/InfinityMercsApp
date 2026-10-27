namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class RelaxedInt32ConverterTests
{
    [Theory]
    [InlineData("4", 4)]
    [InlineData("\"12\"", 12)]
    [InlineData("\" \"", 0)]
    [InlineData("\"x\"", 0)]
    [InlineData("1.5", 0)]
    [InlineData("null", 0)]
    [InlineData("true", 0)]
    public void Read_Values_ReadsAnIntegerOrZero(string json, int expected) =>
        Assert.Equal(expected, ConverterJson.Read<int>(new RelaxedInt32Converter(), json));

    [Fact]
    public void Write_Value_WritesTheNumber() => Assert.Equal("3", ConverterJson.Write(new RelaxedInt32Converter(), 3));
}
