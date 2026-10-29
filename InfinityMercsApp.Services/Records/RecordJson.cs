using System.Text.Json;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Services.Records;

/// <summary>How company and season files are written and read, wherever they are written from.</summary>
public static class RecordJson
{
    public static JsonSerializerOptions ReadOptions { get; } = new() { PropertyNameCaseInsensitive = true, TypeInfoResolver = RecordJsonContext.Default };

    public static JsonSerializerOptions CompanyWriteOptions { get; } = new() { WriteIndented = true, TypeInfoResolver = RecordJsonContext.Default };

    public static JsonSerializerOptions SeasonWriteOptions { get; } = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = RecordJsonContext.Default
    };
}
