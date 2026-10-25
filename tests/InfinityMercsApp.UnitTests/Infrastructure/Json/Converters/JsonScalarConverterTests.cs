using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class JsonScalarConverterTests
{
    private static JsonScalar Read(string json) => ConverterJson.Read<JsonScalar>(new JsonScalarConverter(), json);

    [Fact]
    public void Read_Integer_KeepsTheNumberAndItsText()
    {
        var scalar = Read("12");

        Assert.Equal(JsonScalarKind.Number, scalar.Kind);
        Assert.Equal("12", scalar.Text);
        Assert.Equal(12, scalar.AsInt32());
        Assert.True(scalar.TryGetNumberDecimal(out var value));
        Assert.Equal(12m, value);
    }

    [Fact]
    public void Read_Fraction_IsADecimalButNotAnInteger()
    {
        var scalar = Read("1.50");

        Assert.Equal("1.50", scalar.Text);
        Assert.False(scalar.TryGetNumberInt32(out _));
        Assert.Null(scalar.AsInt32());
        Assert.True(scalar.TryGetNumberDecimal(out var value));
        Assert.Equal(1.5m, value);
    }

    [Fact]
    public void Read_HugeNumber_KeepsOnlyTheText()
    {
        var scalar = Read("1e400");

        Assert.Equal(JsonScalarKind.Number, scalar.Kind);
        Assert.Equal("1e400", scalar.Text);
        Assert.False(scalar.TryGetNumberDecimal(out _));
    }

    [Fact]
    public void Read_NumericString_ParsesAsInteger()
    {
        var scalar = Read("\"8\"");

        Assert.Equal(JsonScalarKind.String, scalar.Kind);
        Assert.Equal("8", scalar.AsString());
        Assert.Equal(8, scalar.AsInt32());
        Assert.Equal(8, scalar.AsInvariantInt32());
    }

    [Theory]
    [InlineData("true", "True", true)]
    [InlineData("false", "False", false)]
    [InlineData("\"True\"", "String", true)]
    [InlineData("\"no\"", "String", null)]
    [InlineData("null", "Null", null)]
    public void Read_BoolShapes_ReadAsBool(string json, string kind, bool? expected)
    {
        var scalar = Read(json);

        Assert.Equal(kind, scalar.Kind.ToString());
        Assert.Equal(expected, scalar.AsBool());
    }

    [Theory]
    [InlineData("[1]")]
    [InlineData("{\"a\":1}")]
    public void Read_ObjectOrArray_IsOther(string json)
    {
        var scalar = Read(json);

        Assert.Equal(JsonScalarKind.Other, scalar.Kind);
        Assert.Null(scalar.AsString());
        Assert.Null(scalar.AsInvariantInt32());
    }

    [Theory]
    [InlineData("12.50")]
    [InlineData("\"x\"")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("null")]
    public void Write_ReadValue_WritesItAsSent(string json) =>
        Assert.Equal(json, ConverterJson.Write(new JsonScalarConverter(), Read(json)));

    [Fact]
    public void Write_Other_WritesNull() =>
        Assert.Equal("null", ConverterJson.Write(new JsonScalarConverter(), JsonScalar.Other));

    [Fact]
    public void Write_Missing_WritesNull() =>
        Assert.Equal("null", ConverterJson.Write(new JsonScalarConverter(), default(JsonScalar)));
}
