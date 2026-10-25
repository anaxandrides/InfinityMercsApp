using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class LenientObjectListConverterTests
{
    [Fact]
    public void Read_MixedItems_KeepsOnlyObjects()
    {
        var items = ConverterJson.Read<List<NamedThing>>(
            new LenientObjectListConverter<NamedThing>(), "[{\"Name\":\"a\"},1,\"b\",null,[{\"Name\":\"c\"}],{\"Name\":\"d\"}]");

        Assert.Equal(["a", "d"], items?.Select(item => item.Name));
    }

    [Theory]
    [InlineData("{\"Name\":\"a\"}")]
    [InlineData("null")]
    public void Read_NotAnArray_ReadsNull(string json) =>
        Assert.Null(ConverterJson.Read<List<NamedThing>>(new LenientObjectListConverter<NamedThing>(), json));

    [Fact]
    public void Write_Items_WritesAnArray() =>
        Assert.Equal(
            "[{\"Name\":\"a\"}]",
            ConverterJson.Write<List<NamedThing>?>(new LenientObjectListConverter<NamedThing>(), [new NamedThing { Name = "a" }]));

    [Fact]
    public void Write_Null_WritesNull() =>
        Assert.Equal("null", ConverterJson.Write<List<NamedThing>?>(new LenientObjectListConverter<NamedThing>(), null));
}
