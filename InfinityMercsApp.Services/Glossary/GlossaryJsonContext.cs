using System.Text.Json.Serialization;

namespace InfinityMercsApp.Services.Glossary;

/// <summary>Source-generated JSON contract for the glossary index.</summary>
[JsonSerializable(typeof(GlossaryPayload))]
internal sealed partial class GlossaryJsonContext : JsonSerializerContext;
