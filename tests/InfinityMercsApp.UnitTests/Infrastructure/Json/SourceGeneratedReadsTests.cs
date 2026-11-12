using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Infrastructure.API.InfinityArmy;
using InfinityMercsApp.Infrastructure.Json;
using InfinityMercsApp.Infrastructure.Json.Converters;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.UnitTests.Characterization;
using ApiMetadataDocument = InfinityMercsApp.Infrastructure.Models.API.Metadata.MetadataDocument;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json;

/// <summary>The real files the app reads come out the same through the source-generated contracts as through reflection.</summary>
public sealed class SourceGeneratedReadsTests
{
    private static readonly JsonSerializerOptions Dump = new(JsonSerializerDefaults.Web) { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };

    public static TheoryData<string> StoreFiles { get; } = new(
        Directory.GetFiles(Path.Combine(RepoFiles.Resolve("InfinityMercsApp/Resources/Raw/Stores/store-alpha-sec.json"), ".."), "*.json")
            .Select(Path.GetFileName)
            .OfType<string>()
            .Order(StringComparer.Ordinal));

    [Fact]
    public void Metadata_ReferenceFile_ReadsAsReflectionDid()
    {
        var json = File.ReadAllText(RepoFiles.Resolve("InfinityMercsApp/ReferenceData/metadata.json"));

        AssertSameRead<ApiMetadataDocument>(json, MetadataOptions(MetadataJsonContext.Default), MetadataOptions(new DefaultJsonTypeInfoResolver()));
    }

    [Theory]
    [MemberData(nameof(StoreFiles))]
    public void Store_BundledFile_ReadsAsReflectionDid(string fileName)
    {
        var json = File.ReadAllText(RepoFiles.Resolve($"InfinityMercsApp/Resources/Raw/Stores/{fileName}"));

        AssertSameRead<Store>(json, StoreOptions(StoreJsonContext.Default), StoreOptions(new DefaultJsonTypeInfoResolver()));
    }

    [Fact]
    public void ArmyDefinition_ReferenceArmy_WritesAsReflectionDid()
    {
        var army = ArmyJsonReader.Shared.ReadArmyPayload(File.ReadAllText(RepoFiles.Resolve(ReferenceCorpus.Army101Path)))
            ?? throw new InvalidOperationException("The reference army didn't read.");
        var byReflection = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new ArmyFilterCatalogConverter() },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        Assert.Equal(JsonSerializer.Serialize(army, byReflection), ArmyDefinitionJson.Write(army));
    }

    // As InfinityArmyAPI reads the metadata.
    private static JsonSerializerOptions MetadataOptions(IJsonTypeInfoResolver resolver) => new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new RelaxedInt32Converter(), new RelaxedNullableInt32Converter() },
        TypeInfoResolver = resolver
    };

    // As StoreProvider reads the market files.
    private static JsonSerializerOptions StoreOptions(IJsonTypeInfoResolver resolver) => new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = resolver
    };

    private static void AssertSameRead<T>(string json, JsonSerializerOptions generated, JsonSerializerOptions byReflection)
    {
        var expected = JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(json, byReflection), Dump);

        Assert.Equal(expected, JsonSerializer.Serialize(JsonSerializer.Deserialize(json, JsonContracts.For<T>(generated)), Dump));
    }
}
