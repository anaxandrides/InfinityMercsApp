using InfinityMercsApp.Infrastructure.Json.Converters;
using InfinityMercsApp.Infrastructure.Json.Dtos;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

public sealed class SpecopsLinkListConverterTests
{
    [Fact]
    public void Read_MixedItems_KeepsLinksWithIds()
    {
        var links = ConverterJson.Read<List<SpecopsLinkDto>>(
            new SpecopsLinkListConverter(),
            "[74,\"75\",\"x\",{\"id\":76,\"extras\":[304,\"305\"],\"other\":true},{\"extras\":[1]},{\"id\":77}]");

        Assert.Equal([74, 75, 76, 77], links?.Select(link => link.Id));
        Assert.Equal([304, 305], links?[2].Extras);
        Assert.Empty(links?[3].Extras ?? [0]);
    }

    [Fact]
    public void Read_NotAnArray_ReadsNull() =>
        Assert.Null(ConverterJson.Read<List<SpecopsLinkDto>>(new SpecopsLinkListConverter(), "{\"id\":1}"));

    [Fact]
    public void Write_Links_WritesObjects() =>
        Assert.Equal(
            "[{\"id\":74,\"extras\":[304]}]",
            ConverterJson.Write<List<SpecopsLinkDto>?>(new SpecopsLinkListConverter(), [new SpecopsLinkDto(74, [304])]));

    [Fact]
    public void Write_Null_WritesNull() =>
        Assert.Equal("null", ConverterJson.Write<List<SpecopsLinkDto>?>(new SpecopsLinkListConverter(), null));
}
