using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.UnitTests.Characterization;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Stores;

public sealed class StoreProviderTests
{
    private readonly IAppPackageFiles _packageFiles = Substitute.For<IAppPackageFiles>();
    private readonly StoreProvider _provider;

    public StoreProviderTests()
    {
        _packageFiles.OpenAsync(Arg.Any<string>()).Returns(call =>
            Task.FromResult<Stream>(File.OpenRead(RepoFiles.Resolve("InfinityMercsApp/Resources/Raw/" + call.Arg<string>()))));
        _provider = new StoreProvider(_packageFiles);
    }

    [Fact]
    public async Task StoresAreReadFromThePackagedJson()
    {
        var store = await _provider.GetStoreByNameAsync("Jade Temu");

        Assert.NotNull(store);
        Assert.Equal("Jade Temu", store.Name);
        Assert.Equal(["Yu Jing"], store.AssociatedFactions);
        await _packageFiles.Received(1).OpenAsync("Stores/store-jade-temu.json");
    }

    [Fact]
    public async Task EachStoreIsReadOnce()
    {
        await _provider.GetStoreByNameAsync("Base Market");
        await _provider.GetStoreByNameAsync("base market");

        await _packageFiles.Received(1).OpenAsync("Stores/store-neutral.json");
    }

    [Fact]
    public async Task UnknownStoresAreNull()
    {
        Assert.Null(await _provider.GetStoreByNameAsync("Corner Shop"));
        await _packageFiles.DidNotReceive().OpenAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task AvailableStoresAreTheNeutralOnesAndTheFactionsOwn()
    {
        var names = (await _provider.GetAvailableStoresAsync(["Yu Jing"])).Select(store => store.Name).ToList();

        Assert.Contains("Base Market", names);
        Assert.Contains("Jade Temu", names);
        Assert.DoesNotContain("Bantai Yamaco", names);
    }
}
