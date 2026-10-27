using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Json.Converters;
using InfinityMercsApp.Infrastructure.Json.Dtos;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class ScalarOrArrayConverterTests
{
    private static ScalarOrArrayDto Read(string json) => ConverterJson.Read<ScalarOrArrayDto>(new ScalarOrArrayConverter(), json);

    [Fact]
    public void Read_Array_KeepsTheItems()
    {
        var value = Read("[\"CORE\",\"HARIS\"]");

        Assert.Equal(JsonScalarKind.Other, value.Kind);
        Assert.Equal(["CORE", "HARIS"], value.Items?.Select(item => item.AsString()));
        Assert.Null(value.Text);
    }

    [Fact]
    public void Read_String_KeepsTheText()
    {
        var value = Read("\"10-5\"");

        Assert.Equal(JsonScalarKind.String, value.Kind);
        Assert.Equal("10-5", value.Text);
        Assert.Null(value.Items);
    }

    [Fact]
    public void Read_Number_HasNoText()
    {
        var value = Read("4");

        Assert.Equal(JsonScalarKind.Number, value.Kind);
        Assert.Null(value.Text);
    }

    [Theory]
    [InlineData("[10,5]")]
    [InlineData("\"CORE\"")]
    [InlineData("null")]
    public void Write_Read_RoundTrips(string json) =>
        Assert.Equal(json, ConverterJson.Write(new ScalarOrArrayConverter(), Read(json)));

    [Fact]
    public void Missing_Default_IsMissing() => Assert.True(default(ScalarOrArrayDto).IsMissing);
}
