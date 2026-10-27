using System.Text.Json.Serialization;
using InfinityMercsApp.Infrastructure.Json.Converters;

namespace InfinityMercsApp.UnitTests.Infrastructure.Json.Converters;

/// <summary>Holds a <see cref="NamedThing"/> read leniently, as the army DTOs hold their objects.</summary>
public sealed class NamedThingHolder
{
    [JsonConverter(typeof(LenientObjectConverter<NamedThing>))]
    public NamedThing? Thing { get; init; }
}
