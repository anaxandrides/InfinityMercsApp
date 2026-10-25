using InfinityMercsApp.ApiClient.Telemetry;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.ArmyData;

public sealed class WebArmyDataTests
{
    [Fact]
    public async Task StartAsync_ApiAnswers_IsReadyAndStartsSync()
    {
        var kit = new WebArmyDataKit();

        await kit.Data.StartAsync();

        Assert.True(kit.Data.IsReady);
        Assert.Equal(1, kit.Data.TotalArmies);
        kit.Sync.Received(1).Start();
        kit.Telemetry.Received().Record(Arg.Is<TelemetryEvent>(e => e.Kind == TelemetryKind.Startup && e.Name == "WebReady" && e.Value == 250));
    }

    [Fact]
    public async Task StartAsync_ManifestMissing_SaysWhyAndIsNotReady()
    {
        var kit = new WebArmyDataKit(manifestServed: false);

        await kit.Data.StartAsync();

        Assert.False(kit.Data.IsReady);
        Assert.Contains("Reload the page", kit.Data.FailureMessage, StringComparison.Ordinal);
        kit.Sync.DidNotReceive().Start();
    }

    [Fact]
    public async Task StartAsync_ApiWorkedOutListings_OffersThem()
    {
        var kit = new WebArmyDataKit();

        await kit.Data.StartAsync();

        Assert.Equal(WebArmyDataKit.Listings.LieutenantFactionIds, kit.Data.Listings?.LieutenantFactionIds);
        Assert.Equal(["Haris"], kit.Data.Listings?.CohesiveAt(75)?.Factions.Single().ValidCoreFireteams);
    }

    [Fact]
    public async Task StartAsync_ApiWithoutListings_OffersNone()
    {
        var kit = new WebArmyDataKit(apiBuiltGeneratedFactions: false);

        await kit.Data.StartAsync();

        Assert.True(kit.Data.IsReady);
        Assert.Null(kit.Data.Listings);
    }

    [Fact]
    public async Task EnsureAsync_Army_StoresIt()
    {
        var kit = new WebArmyDataKit();

        await kit.Data.EnsureAsync([WebArmyDataKit.PanOceania]);

        Assert.NotEmpty(kit.Factions.GetUnitsByFaction(WebArmyDataKit.PanOceania));
    }

    [Fact]
    public async Task EnsureAsync_GeneratedFactionTheApiBuilt_StoresTheApisBuild()
    {
        var kit = new WebArmyDataKit();

        await kit.Data.EnsureAsync([TagCompanyFactionGenerator.TagCompanyFactionId]);

        Assert.Single(kit.Factions.GetUnitsByFaction(TagCompanyFactionGenerator.TagCompanyFactionId));
        Assert.Contains($"army/generated/{TagCompanyFactionGenerator.TagCompanyFactionId}", kit.Api.Requested);
    }

    [Fact]
    public async Task EnsureAsync_GeneratedFactionTheApiLacks_BuildsItFromEveryArmy()
    {
        var kit = new WebArmyDataKit(apiBuiltGeneratedFactions: false);

        await kit.Data.EnsureAsync([AirborneCompanyFactionGenerator.AirborneCompanyFactionId]);

        Assert.NotEmpty(kit.Factions.GetUnitsByFaction(AirborneCompanyFactionGenerator.AirborneCompanyFactionId));
        Assert.DoesNotContain(kit.Api.Requested, path => path.StartsWith("army/generated/", StringComparison.Ordinal));
    }
}
