using InfinityMercsApp.Web.ArmyData;

namespace InfinityMercsApp.Web.UnitTests.Site;

public sealed class SiteContextTests : SiteContext
{
    [Fact]
    public async Task Services_AsTheSiteHasThem_LoadTheArmyData()
    {
        var armyData = Get<WebArmyData>();

        await armyData.StartAsync();

        Assert.True(armyData.IsReady);
    }
}
