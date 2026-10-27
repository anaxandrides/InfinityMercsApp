using InfinityMercsApp.Infrastructure.Data;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Repositories.InMemory;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Infrastructure.Providers;

/// <summary>The cache of which factions can field a Core fireteam under a unit filter.</summary>
public sealed class SpecOpsFireteamValidityTests
{
    private readonly SpecOpsProvider _provider = new(new InMemoryRepository(), Substitute.For<IArmyContentStore>());

    [Fact]
    public void Upsert_Teams_ReadBackInOrder()
    {
        _provider.UpsertCCFactionFireteamValidity(101, "key", true, ["Haris", "Core"]);

        var record = Assert.Single(_provider.GetCCFactionFireteamValidity("key", [101]));
        Assert.Equal("101:key", record.CacheKey);
        Assert.True(record.HasValidCoreFireteams);
        Assert.Equal(["Haris", "Core"], record.ValidCoreFireteams);
    }

    [Fact]
    public void Upsert_Again_ReplacesTheTeams()
    {
        _provider.UpsertCCFactionFireteamValidity(101, "key", true, ["Haris", "Core"]);
        _provider.UpsertCCFactionFireteamValidity(101, "key", false, []);

        var record = Assert.Single(_provider.GetCCFactionFireteamValidity("key", [101]));
        Assert.False(record.HasValidCoreFireteams);
        Assert.Empty(record.ValidCoreFireteams);
    }

    [Fact]
    public void Upsert_PaddedKey_IsStoredTrimmed()
    {
        _provider.UpsertCCFactionFireteamValidity(101, " key ", true, ["Haris"]);

        Assert.Single(_provider.GetCCFactionFireteamValidity("key", [101]));
    }

    [Theory]
    [InlineData(0, "key")]
    [InlineData(101, " ")]
    public void Upsert_NoFactionOrKey_StoresNothing(int factionId, string filterKey)
    {
        _provider.UpsertCCFactionFireteamValidity(factionId, filterKey, true, ["Haris"]);

        Assert.Empty(_provider.GetCCFactionFireteamValidity("key", [0, 101]));
    }

    [Fact]
    public void Get_OnlySomeFactionsStored_ReturnsThoseStored()
    {
        _provider.UpsertCCFactionFireteamValidity(101, "key", true, ["Haris"]);
        _provider.UpsertCCFactionFireteamValidity(102, "other", true, ["Haris"]);

        Assert.Equal([101], _provider.GetCCFactionFireteamValidity("key", [101, 102, 103]).Select(record => record.FactionId));
    }

    [Fact]
    public void Get_BlankKeyOrNoFactions_IsEmpty()
    {
        _provider.UpsertCCFactionFireteamValidity(101, "key", true, ["Haris"]);

        Assert.Empty(_provider.GetCCFactionFireteamValidity(" ", [101]));
        Assert.Empty(_provider.GetCCFactionFireteamValidity("key", []));
    }

    [Fact]
    public void Get_Cancelled_Throws() =>
        Assert.Throws<OperationCanceledException>(() => _provider.GetCCFactionFireteamValidity("key", [101], new CancellationToken(true)));
}
