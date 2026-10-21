using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Services.Import;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ArmyFaction = InfinityMercsApp.Domain.Models.Army.Faction;
using MetadataFaction = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.UnitTests.Services.Import;

/// <summary>
/// Checking downloads the metadata and every army and compares them with what is stored, without storing anything;
/// applying stores what changed.
/// </summary>
public sealed class ArmyDataUpdaterTests : IDisposable
{
    private const int PanOceania = 101;
    private const int YuJing = 201;
    private const string StoredFingerprint = "metadata-v1";

    private readonly IInfinityArmyAPI _api = Substitute.For<IInfinityArmyAPI>();
    private readonly IArmyMirror _mirror = Substitute.For<IArmyMirror>();
    private readonly IFactionProvider _factions = Substitute.For<IFactionProvider>();
    private readonly IMetadataProvider _metadata = Substitute.For<IMetadataProvider>();
    private readonly IArmyImportProvider _armyImport = Substitute.For<IArmyImportProvider>();
    private readonly IAppSettingsProvider _settings = Substitute.For<IAppSettingsProvider>();
    private readonly IAirborneCompanyFactionGenerator _airborne = Substitute.For<IAirborneCompanyFactionGenerator>();
    private readonly IInspiringCompanyFactionGenerator _inspiring = Substitute.For<IInspiringCompanyFactionGenerator>();
    private readonly ITagCompanyFactionGenerator _tag = Substitute.For<ITagCompanyFactionGenerator>();
    private readonly TempRecords _records = new();
    private readonly HttpClient _httpClient = new();
    private readonly ArmyDataUpdater _updater;

    public ArmyDataUpdaterTests()
    {
        _updater = new ArmyDataUpdater(
            _api,
            _mirror,
            _factions,
            _metadata,
            _armyImport,
            _settings,
            new FactionLogoCacheService(
                _httpClient,
                _records.Paths with { FactionLogoDirectory = Path.Combine(_records.Paths.CompaniesDirectory, "logos") },
                Substitute.For<IAppPackageFiles>()),
            _inspiring,
            _airborne,
            _tag);

        _metadata.HasMetadata().Returns(true);
        _settings.GetMetadataFingerprint().Returns(StoredFingerprint);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _records.Dispose();
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public void HasStoredData_MetadataAndArmies_NeedsBoth(bool hasMetadata, bool hasArmies, bool expected)
    {
        _metadata.HasMetadata().Returns(hasMetadata);
        _factions.HasAnyFactionData().Returns(hasArmies);

        Assert.Equal(expected, _updater.HasStoredData());
    }

    [Fact]
    public async Task CheckAsync_MetadataDownloadFails_Fails()
    {
        _api.GetMetaDataAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("Status=403"));

        var check = await _updater.CheckAsync();

        Assert.False(check.Succeeded);
        Assert.Contains("Status=403", check.FailureMessage);
        await _api.DidNotReceive().GetArmyDataAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAsync_MetadataWithoutFactions_Fails()
    {
        _api.GetMetaDataAsync(Arg.Any<CancellationToken>()).Returns(new MetadataDocument());

        var check = await _updater.CheckAsync();

        Assert.False(check.Succeeded);
    }

    [Fact]
    public async Task CheckAsync_NoArmyDownloads_Fails()
    {
        Download(PanOceania, YuJing);
        _api.GetArmyDataAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));

        var check = await _updater.CheckAsync();

        Assert.False(check.Succeeded);
    }

    [Fact]
    public async Task CheckAsync_SomeArmiesFail_NamesThem()
    {
        Download(PanOceania, YuJing);
        Serve(PanOceania, "7.1");
        _api.GetArmyDataAsync(YuJing, Arg.Any<CancellationToken>()).ThrowsAsync(new HttpRequestException("offline"));

        var check = await _updater.CheckAsync();

        Assert.True(check.Succeeded);
        Assert.Equal(["Yu Jing"], check.FailedFactions);
        Assert.Equal([PanOceania], check.Armies.Select(army => army.FactionId));
    }

    [Fact]
    public async Task CheckAsync_ArmyWithoutVersion_CountsAsNotDownloaded()
    {
        Download(PanOceania);
        Serve(PanOceania, string.Empty);

        var check = await _updater.CheckAsync();

        Assert.False(check.Succeeded);
    }

    [Fact]
    public async Task CheckAsync_SuppressedFaction_SkipsIt()
    {
        Download(PanOceania, 901);
        Serve(PanOceania, "7.1");

        await _updater.CheckAsync();

        await _api.DidNotReceive().GetArmyDataAsync(901, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("7.2", "7.1", true, true)]
    [InlineData("7.10", "7.9", true, true)]
    [InlineData("7.1", "7.1", true, false)]
    [InlineData("7.1", "7.2", true, false)]
    [InlineData("7.1", "7.1", false, true)]
    public async Task CheckAsync_StoredArmy_ComparesWithTheStoredVersion(string downloaded, string stored, bool hasUnits, bool isUpdate)
    {
        Download(PanOceania);
        Serve(PanOceania, downloaded);
        StoreArmy(PanOceania, stored, hasUnits);

        var check = await _updater.CheckAsync();

        var army = Assert.Single(check.Armies);
        Assert.Equal(stored, army.StoredVersion);
        Assert.Equal(isUpdate, army.IsUpdate);
    }

    [Fact]
    public async Task CheckAsync_CopyHoldsTheStoredVersion_SkipsThatArmy()
    {
        Download(PanOceania, YuJing);
        Mirror(new MirroredVersion(PanOceania, "7.1"), new MirroredVersion(YuJing, "3.0"));
        StoreArmy(PanOceania, "7.1", hasUnits: true);
        Serve(YuJing, "3.0");

        var check = await _updater.CheckAsync();

        Assert.True(check.Succeeded);
        await _api.DidNotReceive().GetArmyDataAsync(PanOceania, Arg.Any<CancellationToken>());
        Assert.Equal([YuJing], check.Armies.Select(army => army.FactionId));
    }

    [Theory]
    [InlineData("7.2", true)]
    [InlineData("7.1", false)]
    public async Task CheckAsync_CopyHoldsAnotherVersionOrNothingUsableIsStored_DownloadsTheArmy(string mirrored, bool hasUnits)
    {
        Download(PanOceania);
        Mirror(new MirroredVersion(PanOceania, mirrored));
        StoreArmy(PanOceania, "7.1", hasUnits);
        Serve(PanOceania, mirrored);

        await _updater.CheckAsync();

        await _api.Received(1).GetArmyDataAsync(PanOceania, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CheckAsync_EveryArmyCurrent_SucceedsWithoutUpdates()
    {
        Download(PanOceania);
        Mirror(new MirroredVersion(PanOceania, "7.1"));
        StoreArmy(PanOceania, "7.1", hasUnits: true);

        var check = await _updater.CheckAsync();

        Assert.True(check.Succeeded);
        Assert.False(check.HasUpdates);
        Assert.Empty(check.FailedFactions);
    }

    [Fact]
    public async Task CheckAsync_ArmyNotStored_TreatsItAsAnUpdate()
    {
        Download(PanOceania);
        Serve(PanOceania, "7.1");

        var check = await _updater.CheckAsync();

        var army = Assert.Single(check.Updates);
        Assert.Null(army.StoredVersion);
    }

    [Theory]
    [InlineData(StoredFingerprint, true, false)]
    [InlineData("metadata-v2", true, true)]
    [InlineData(StoredFingerprint, false, true)]
    public async Task CheckAsync_Metadata_ComparesWithTheStoredFingerprint(string downloaded, bool hasMetadata, bool changed)
    {
        Download(downloaded, PanOceania);
        Serve(PanOceania, "7.1");
        StoreArmy(PanOceania, "7.1", hasUnits: true);
        _metadata.HasMetadata().Returns(hasMetadata);

        var check = await _updater.CheckAsync();

        Assert.Equal(changed, check.MetadataChanged);
        Assert.Equal(changed, check.HasUpdates);
    }

    [Fact]
    public async Task CheckAsync_Updates_StoresNothing()
    {
        Download("metadata-v2", PanOceania);
        Serve(PanOceania, "7.2");

        await _updater.CheckAsync();

        _metadata.DidNotReceive().Import(Arg.Any<MetadataDocument>());
        _settings.DidNotReceive().SetMetadataFingerprint(Arg.Any<string>());
        _armyImport.DidNotReceive().Import(Arg.Any<int>(), Arg.Any<ArmyDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_ChangedMetadata_StoresItAndItsFingerprint()
    {
        var metadata = Download("metadata-v2", PanOceania);
        Serve(PanOceania, "7.1");
        StoreArmy(PanOceania, "7.1", hasUnits: true);

        var notStored = await _updater.ApplyAsync(await _updater.CheckAsync());

        Assert.Empty(notStored);
        _metadata.Received(1).Import(metadata);
        _settings.Received(1).SetMetadataFingerprint("metadata-v2");
    }

    [Fact]
    public async Task ApplyAsync_UnchangedMetadata_LeavesItAlone()
    {
        Download(PanOceania);
        Serve(PanOceania, "7.2");
        StoreArmy(PanOceania, "7.1", hasUnits: true);

        await _updater.ApplyAsync(await _updater.CheckAsync());

        _metadata.DidNotReceive().Import(Arg.Any<MetadataDocument>());
    }

    [Fact]
    public async Task ApplyAsync_SomeArmiesChanged_StoresOnlyThose()
    {
        Download(PanOceania, YuJing);
        var panOceania = Serve(PanOceania, "7.2");
        Serve(YuJing, "7.1");
        StoreArmy(PanOceania, "7.1", hasUnits: true);
        StoreArmy(YuJing, "7.1", hasUnits: true);

        await _updater.ApplyAsync(await _updater.CheckAsync());

        _armyImport.Received(1).Import(PanOceania, panOceania, Arg.Any<CancellationToken>());
        _armyImport.DidNotReceive().Import(YuJing, Arg.Any<ArmyDefinition>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_NamesTheArmiesThatCouldNotBeStored_AndStoresTheRest()
    {
        Download(PanOceania, YuJing);
        Serve(PanOceania, "7.1");
        var yuJing = Serve(YuJing, "7.1");
        _armyImport.When(import => import.Import(PanOceania, Arg.Any<ArmyDefinition>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new IOException("disk full"));

        var notStored = await _updater.ApplyAsync(await _updater.CheckAsync());

        Assert.Equal(["PanOceania"], notStored);
        _armyImport.Received(1).Import(YuJing, yuJing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_Updates_RebuildsTheGeneratedCompanies()
    {
        Download(PanOceania);
        Serve(PanOceania, "7.1");
        _airborne.When(generator => generator.Generate(Arg.Any<CancellationToken>())).Do(_ => throw new InvalidOperationException("no army"));

        await _updater.ApplyAsync(await _updater.CheckAsync());

        _airborne.Received(1).Generate(Arg.Any<CancellationToken>());
        _inspiring.Received(1).Generate(Arg.Any<CancellationToken>());
        _tag.Received(1).Generate(Arg.Any<CancellationToken>());
        _settings.Received(1).SetGeneratedCompaniesVersion(ArmyDataUpdater.GeneratedCompaniesVersion);
    }

    [Fact]
    public void RebuildOutdatedGeneratedCompanies_OlderRules_RebuildsThem()
    {
        _factions.HasAnyFactionData().Returns(true);
        _settings.GetGeneratedCompaniesVersion().Returns("1");

        Assert.True(_updater.RebuildOutdatedGeneratedCompanies());

        _airborne.Received(1).Generate(Arg.Any<CancellationToken>());
        _inspiring.Received(1).Generate(Arg.Any<CancellationToken>());
        _tag.Received(1).Generate(Arg.Any<CancellationToken>());
        _settings.Received(1).SetGeneratedCompaniesVersion(ArmyDataUpdater.GeneratedCompaniesVersion);
    }

    [Fact]
    public void RebuildOutdatedGeneratedCompanies_CurrentRules_LeavesThem()
    {
        _factions.HasAnyFactionData().Returns(true);
        _settings.GetGeneratedCompaniesVersion().Returns(ArmyDataUpdater.GeneratedCompaniesVersion);

        Assert.False(_updater.RebuildOutdatedGeneratedCompanies());

        _airborne.DidNotReceive().Generate(Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RebuildOutdatedGeneratedCompanies_NothingStored_BuildsNothing()
    {
        _factions.HasAnyFactionData().Returns(false);

        Assert.False(_updater.RebuildOutdatedGeneratedCompanies());

        _airborne.DidNotReceive().Generate(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApplyAsync_FailedCheck_DoesNothing()
    {
        var notStored = await _updater.ApplyAsync(ArmyDataCheck.Failed("offline"));

        Assert.Empty(notStored);
        _metadata.DidNotReceive().Import(Arg.Any<MetadataDocument>());
        _tag.DidNotReceive().Generate(Arg.Any<CancellationToken>());
    }

    private void Download(params IEnumerable<int> factionIds) => Download(StoredFingerprint, factionIds);

    private MetadataDocument Download(string fingerprint, params IEnumerable<int> factionIds)
    {
        var metadata = new MetadataDocument
        {
            Fingerprint = fingerprint,
            Factions = factionIds.Select(id => new MetadataFaction { Id = id, ParentId = id, Name = NameOf(id) }).ToList()
        };
        _api.GetMetaDataAsync(Arg.Any<CancellationToken>()).Returns(metadata);
        return metadata;
    }

    private ArmyDefinition Serve(int factionId, string version)
    {
        var army = new ArmyDefinition { Version = version };
        _api.GetArmyDataAsync(factionId, Arg.Any<CancellationToken>()).Returns(army);
        return army;
    }

    private void Mirror(params IEnumerable<MirroredVersion> armies) =>
        _mirror.GetManifestAsync(Arg.Any<CancellationToken>()).Returns(new ArmyMirrorManifest
        {
            Armies = armies.Select(army => new MirroredArmy(army.FactionId, army.Version, "hash", DateTimeOffset.UnixEpoch)).ToList()
        });

    private void StoreArmy(int factionId, string version, bool hasUnits)
    {
        _factions.GetFactionSnapshot(factionId).Returns(new ArmyFaction { FactionId = factionId, Version = version });
        _factions.GetResumeByFaction(factionId).Returns(hasUnits ? [new Resume { FactionId = factionId, UnitId = 1, Name = "Unit" }] : []);
    }

    private static string NameOf(int factionId) => factionId switch
    {
        PanOceania => "PanOceania",
        YuJing => "Yu Jing",
        _ => $"Faction {factionId}"
    };
}
