using System.IO.Compression;
using InfinityMercsApp.Infrastructure.API.ArmyMirror;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.UnitTests.Support;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Logos;

public sealed class LogoBundleInstallerTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly IArmyMirror _mirror = Substitute.For<IArmyMirror>();
    private readonly string _cache;
    private readonly LogoBundleInstaller _installer;

    public LogoBundleInstallerTests()
    {
        _cache = Path.Combine(_records.Paths.CompaniesDirectory, "svg-cache");
        _installer = new LogoBundleInstaller(_mirror, _records.Paths with { FactionLogoDirectory = _cache });
        Offers("h1");
    }

    public void Dispose() => _records.Dispose();

    private void Offers(string hash) =>
        _mirror.GetManifestAsync(Arg.Any<CancellationToken>()).Returns(new ArmyMirrorManifest { Logos = new MirroredPayload(hash, DateTimeOffset.UnixEpoch) });

    // The mirror "downloads" a zip holding these entries.
    private void Serves(params IEnumerable<string> entryNames) =>
        _mirror.DownloadLogoBundleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            using var archive = ZipFile.Open(call.Arg<string>(), ZipArchiveMode.Create);
            foreach (var name in entryNames)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());
                writer.Write("<svg/>");
            }

            return true;
        });

    [Fact]
    public async Task InstallAsync_NewBundle_FillsTheCache()
    {
        Serves("101.svg", "units/101-7.svg");

        Assert.True(await _installer.InstallAsync());

        Assert.True(File.Exists(Path.Combine(_cache, "101.svg")));
        Assert.True(File.Exists(Path.Combine(_cache, "units", "101-7.svg")));
        Assert.False(File.Exists(Path.Combine(_cache, "logos.zip.part")));
    }

    [Fact]
    public async Task InstallAsync_SameBundleAgain_DoesNotDownload()
    {
        Serves("101.svg");
        await _installer.InstallAsync();

        Assert.False(await _installer.InstallAsync());

        await _mirror.Received(1).DownloadLogoBundleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InstallAsync_EntriesOutsideTheCacheOrNotLogos_AreSkipped()
    {
        Serves("../escaped.svg", "notes.txt", "101.svg");

        await _installer.InstallAsync();

        Assert.False(File.Exists(Path.Combine(_records.Paths.CompaniesDirectory, "escaped.svg")));
        Assert.False(File.Exists(Path.Combine(_cache, "notes.txt")));
        Assert.True(File.Exists(Path.Combine(_cache, "101.svg")));
    }

    [Fact]
    public async Task InstallAsync_DownloadFails_LeavesNoMarker()
    {
        _mirror.DownloadLogoBundleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        Assert.False(await _installer.InstallAsync());

        Assert.False(File.Exists(Path.Combine(_cache, LogoBundleInstaller.MarkerFileName)));
    }

    [Fact]
    public async Task InstallAsync_NoBundleYet_DoesNothing()
    {
        _mirror.GetManifestAsync(Arg.Any<CancellationToken>()).Returns(new ArmyMirrorManifest());

        Assert.False(await _installer.InstallAsync());
    }
}
