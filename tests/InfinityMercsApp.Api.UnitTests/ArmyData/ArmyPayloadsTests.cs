using System.Text.Json;
using InfinityMercsApp.Api.ArmyData;

namespace InfinityMercsApp.Api.UnitTests.ArmyData;

public sealed class ArmyPayloadsTests
{
    [Fact]
    public void FactionIds_MetadataWithFactions_AreInOrderOnce()
    {
        var ids = ArmyPayloads.FactionIds("""{"factions":[{"id":201},{"id":101},{"id":"301"},{"id":101},{"name":"no id"}]}""");

        Assert.Equal([101, 201, 301], ids);
    }

    [Theory]
    [InlineData("""{"weapons":[]}""")]
    [InlineData("""[]""")]
    [InlineData("""{"factions":{}}""")]
    public void FactionIds_NoFactionList_Throws(string json) =>
        Assert.ThrowsAny<JsonException>(() => ArmyPayloads.FactionIds(json));

    [Fact]
    public void FactionIds_NotJson_Throws() =>
        Assert.ThrowsAny<JsonException>(() => ArmyPayloads.FactionIds("<html>Forbidden</html>"));

    [Fact]
    public void Version_ArmyWithVersion_IsIt() =>
        Assert.Equal("1.4.2", ArmyPayloads.Version("""{"version":"1.4.2","units":[]}"""));

    [Theory]
    [InlineData("""{"units":[]}""")]
    [InlineData("""{"version":""}""")]
    [InlineData("""{"version":3}""")]
    public void Version_NoVersionText_IsNull(string json) => Assert.Null(ArmyPayloads.Version(json));

    [Fact]
    public void Hash_Text_IsItsLowerCaseSha256() =>
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", ArmyPayloads.Hash("abc"));

    [Fact]
    public void FactionIds_UnusableIds_AreLeftOut() =>
        Assert.Equal([101], ArmyPayloads.FactionIds("""{"factions":[5,{"id":1.5},{"id":"x"},{"id":true},{"id":101}]}"""));
}
