using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Records;

/// <summary>
/// Company and season files read and written through the source-generated contracts come out exactly as they did
/// through reflection, with every member filled in.
/// </summary>
public sealed class RecordJsonContractsTests
{
    public static TheoryData<string> OptionNames { get; } = ["Read", "CompanyWrite", "SeasonWrite"];

    private static JsonSerializerOptions Options(string name) => name switch
    {
        "Read" => RecordJson.ReadOptions,
        "CompanyWrite" => RecordJson.CompanyWriteOptions,
        _ => RecordJson.SeasonWriteOptions
    };

    // The same options as before, but reading types by reflection.
    private static JsonSerializerOptions ByReflection(JsonSerializerOptions options) =>
        new(options) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void Company_EveryMemberFilled_WritesAndReadsAsReflectionDid(string name) => AssertSameAsReflection<SavedCompanyFile>(Options(name));

    [Theory]
    [MemberData(nameof(OptionNames))]
    public void Season_EveryMemberFilled_WritesAndReadsAsReflectionDid(string name) => AssertSameAsReflection<SeasonFile>(Options(name));

    [Fact]
    public void Entry_EveryMemberFilled_WritesAsReflectionDid() => AssertSameAsReflection<SavedCompanyEntry>(RecordJson.CompanyWriteOptions);

    [Fact]
    public void Company_Filled_HasSomethingInEveryList()
    {
        var json = JsonSerializer.Serialize(FilledObjects.Of<SavedCompanyFile>(), JsonContracts.For<SavedCompanyFile>(RecordJson.CompanyWriteOptions));

        Assert.DoesNotContain("[]", json, StringComparison.Ordinal);
    }

    private static void AssertSameAsReflection<T>(JsonSerializerOptions options)
        where T : new()
    {
        var filled = FilledObjects.Of<T>();
        var reflection = ByReflection(options);
        var expected = JsonSerializer.Serialize(filled, reflection);

        var written = JsonSerializer.Serialize(filled, JsonContracts.For<T>(options));
        var readBack = JsonSerializer.Deserialize(expected, JsonContracts.For<T>(options));

        Assert.Equal(expected, written);
        Assert.Equal(expected, JsonSerializer.Serialize(readBack, reflection));
    }
}
