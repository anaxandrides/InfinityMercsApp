using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>Typed JSON contracts from a set of options, whose resolver is a source-generated context.</summary>
public static class JsonContracts
{
    /// <summary>
    /// The contract for <typeparamref name="T"/> under <paramref name="options"/>; throws <see cref="NotSupportedException"/>
    /// when the options' context doesn't list the type, rather than falling back to reflection.
    /// </summary>
    public static JsonTypeInfo<T> For<T>(JsonSerializerOptions options) => (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
}
