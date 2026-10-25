using InfinityMercsApp.Web.Platform;

namespace InfinityMercsApp.Web.UnitTests.Platform;

public sealed class RemoteLogoStoreTests
{
    [Fact]
    public async Task FetchAsync_Logo_RemembersWhereItIsWithoutDownloading()
    {
        var store = new RemoteLogoStore();

        Assert.True(await store.FetchAsync("https://assets.example/units/1.svg", "/mercs/svg-cache/units/101-1.svg", CancellationToken.None));

        Assert.True(store.Has("/mercs/svg-cache/units/101-1.svg"));
        Assert.Equal("https://assets.example/units/1.svg", store.UrlFor("/mercs/svg-cache/units/101-1.svg"));
    }

    [Fact]
    public async Task CopyPackagedAsync_PackagedLogo_IsServedFromPackage()
    {
        var store = new RemoteLogoStore();

        await store.CopyPackagedAsync("SVGCache/factions/1199.svg", "/mercs/svg-cache/1199.svg", CancellationToken.None);

        Assert.Equal("package/SVGCache/factions/1199.svg", store.UrlFor("/mercs/svg-cache/1199.svg"));
    }

    [Fact]
    public void Has_UnknownLogo_IsFalse() => Assert.False(new RemoteLogoStore().Has("/mercs/svg-cache/101.svg"));
}
