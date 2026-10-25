using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class JsonScalarListConverterTests
{
    [Fact]
    public void Read_Array_ReadsEachScalar()
    {
        var items = ConverterJson.Read<List<JsonScalar>>(new JsonScalarListConverter(), "[10,\"5\",null]");

        Assert.Equal([JsonScalarKind.Number, JsonScalarKind.String, JsonScalarKind.Null], items?.Select(item => item.Kind));
    }

    [Fact]
    public void Read_NotAnArray_ReadsNull() =>
        Assert.Null(ConverterJson.Read<List<JsonScalar>>(new JsonScalarListConverter(), "\"10-5\""));

    [Fact]
    public void Write_Read_RoundTrips()
    {
        var items = ConverterJson.Read<List<JsonScalar>>(new JsonScalarListConverter(), "[10,\"5\"]");

        Assert.Equal("[10,\"5\"]", ConverterJson.Write(new JsonScalarListConverter(), items));
    }

    [Fact]
    public void Write_Null_WritesNull() =>
        Assert.Equal("null", ConverterJson.Write<List<JsonScalar>?>(new JsonScalarListConverter(), null));
}
