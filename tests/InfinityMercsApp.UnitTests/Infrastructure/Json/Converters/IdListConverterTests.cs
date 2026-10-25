using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class IdListConverterTests
{
    [Fact]
    public void Read_MixedItems_KeepsTheIdsInOrder() =>
        Assert.Equal([3, 4, 5], ConverterJson.Read<List<int>>(new IdListConverter(), "[3,\"4\",{\"id\":5},\"x\",null,[6]]"));

    [Theory]
    [InlineData("null")]
    [InlineData("{\"id\":1}")]
    [InlineData("7")]
    public void Read_NotAnArray_ReadsNull(string json) =>
        Assert.Null(ConverterJson.Read<List<int>>(new IdListConverter(), json));

    [Fact]
    public void Write_Ids_WritesAnArray() =>
        Assert.Equal("[1,2]", ConverterJson.Write<List<int>?>(new IdListConverter(), [1, 2]));

    [Fact]
    public void Write_Null_WritesNull() =>
        Assert.Equal("null", ConverterJson.Write<List<int>?>(new IdListConverter(), null));
}
