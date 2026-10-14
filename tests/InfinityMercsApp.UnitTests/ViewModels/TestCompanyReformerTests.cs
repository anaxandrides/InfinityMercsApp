using System.Text.Json;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Diagnostics;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class TestCompanyReformerTests : IDisposable
{
    private readonly TempRecords _records = new();
    private readonly IAppPackageFiles _packageFiles = Substitute.For<IAppPackageFiles>();
    private readonly TestCompanyReformer _reformer;

    public TestCompanyReformerTests()
    {
        _packageFiles.ReadTextAsync(Arg.Any<string>()).ThrowsAsync(new FileNotFoundException());
        _reformer = new TestCompanyReformer(Army101Data.Create(), Substitute.For<ISpecOpsProvider>(), _packageFiles, _records.Paths);
    }

    public void Dispose() => _records.Dispose();

    private static int FusilierId => Army101Data.UnitNamed("FUSILIERS").Id;

    private static string Company(string name, int unitId) => $$"""
        {
          "companyName": "{{name}}", "companyType": "Standard Company", "startSeasonPoints": 75, "currentPoints": 80,
          "sourceFactions": [ { "factionId": 101, "factionName": "PanOceania" } ],
          "entries": [
            { "entryIndex": 1, "name": "Fusilier", "isLieutenant": true, "sourceFactionId": 101, "sourceUnitId": {{unitId}}, "experiencePoints": 7 }
          ]
        }
        """;

    private void GiveManifest(params IEnumerable<string> companies) =>
        _packageFiles.ReadTextAsync(TestCompanyReformer.ManifestFileName)
            .Returns($$"""{ "companies": [ {{string.Join(",", companies)}} ] }""");

    private static List<string> SkippedDeviceFiles =>
        TestCompanyReformer.AdditionalCompanyFileNames.Select(f => $"{f}: skipped (missing company data)").ToList();

    [Fact]
    public async Task RebuildsAManifestCompanyFromTheArmyData()
    {
        GiveManifest($$"""{ "fileName": "test-alpha-0003.json", "companyData": {{Company("Alpha", FusilierId)}} }""");

        var lines = await _reformer.ReformAllAsync();

        Assert.Equal(["test-alpha-0003.json: reformed", .. SkippedDeviceFiles], lines);
        using var saved = JsonDocument.Parse(File.ReadAllText(Path.Combine(_records.Paths.CompaniesDirectory, "test-alpha-0003.json")));
        var root = saved.RootElement;
        Assert.Equal("Alpha", root.GetProperty("CompanyName").GetString());
        Assert.Equal(3, root.GetProperty("CompanyIndex").GetInt32());
        Assert.Equal(75, root.GetProperty("StartSeasonPoints").GetInt32());
        Assert.Equal(80, root.GetProperty("CurrentPoints").GetInt32());
        var entry = root.GetProperty("Entries")[0];
        Assert.Equal("Fusilier", entry.GetProperty("Name").GetString());
        Assert.Equal(7, entry.GetProperty("ExperiencePoints").GetInt32());
        Assert.Contains(entry.GetProperty("CurrentWeaponCodes").EnumerateArray(), code => code.GetProperty("id").GetInt32() == 33);
    }

    [Fact]
    public async Task ReformsDeviceOnlyCompaniesFromTheirSavedCopy()
    {
        var deviceFile = TestCompanyReformer.AdditionalCompanyFileNames[0];
        _records.WriteCompany(deviceFile, Company("Device", FusilierId));

        var lines = await _reformer.ReformAllAsync();

        Assert.Equal($"{deviceFile}: reformed", lines[0]);
        Assert.Contains("\"CompanyName\": \"Device\"", File.ReadAllText(Path.Combine(_records.Paths.CompaniesDirectory, deviceFile)));
    }

    [Fact]
    public async Task ReadsTheManifestFromResourcesRawWhenNotAtTheRoot()
    {
        _packageFiles.ReadTextAsync($"Resources/Raw/{TestCompanyReformer.ManifestFileName}")
            .Returns($$"""{ "companies": [ { "fileName": "test-beta-0001.json", "companyData": {{Company("Beta", FusilierId)}} } ] }""");

        var lines = await _reformer.ReformAllAsync();

        Assert.Equal("test-beta-0001.json: reformed", lines[0]);
    }

    [Fact]
    public async Task SkipsEntriesItCannotRebuild()
    {
        GiveManifest(
            """{ "companyData": {} }""",
            $$"""{ "fileName": "test-ghost-0001.json", "companyData": {{Company("Ghost", 99999)}} }""");

        var lines = await _reformer.ReformAllAsync();

        Assert.Equal(
            ["manifest entry: skipped (missing file name)", "test-ghost-0001.json: skipped (no primary entries)", .. SkippedDeviceFiles],
            lines);
        Assert.False(File.Exists(Path.Combine(_records.Paths.CompaniesDirectory, "test-ghost-0001.json")));
    }

    [Fact]
    public async Task DoesNotListADeviceFileTwiceWhenTheManifestHasIt()
    {
        var deviceFile = TestCompanyReformer.AdditionalCompanyFileNames[1];
        GiveManifest($$"""{ "fileName": "{{deviceFile.ToUpperInvariant()}}", "companyData": {{Company("Tag", FusilierId)}} }""");

        var lines = await _reformer.ReformAllAsync();

        Assert.Equal(2, lines.Count);
        Assert.Equal($"{deviceFile.ToUpperInvariant()}: reformed", lines[0]);
    }
}
