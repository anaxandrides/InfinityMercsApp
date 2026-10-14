using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>
/// A value that is normally an array of scalars but may be a single string — a profile's <c>move</c>
/// (<c>[10, 5]</c> or <c>"10-5"</c>) or a fireteam's <c>type</c> (<c>["CORE","HARIS"]</c> or <c>"CORE"</c>).
/// </summary>
[JsonConverter(typeof(ScalarOrArrayConverter))]
internal readonly struct ScalarOrArrayDto
{
    public ScalarOrArrayDto(JsonScalarKind kind, List<JsonScalar>? items, string? text)
    {
        Kind = kind;
        Items = items;
        Text = text;
    }

    /// <summary><see cref="JsonScalarKind.Other"/> for an array (see <see cref="Items"/>); otherwise the scalar kind.</summary>
    public JsonScalarKind Kind { get; }

    /// <summary>The array items when the value was an array.</summary>
    public List<JsonScalar>? Items { get; }

    /// <summary>The string when the value was a string.</summary>
    public string? Text { get; }

    public bool IsMissing => Kind == JsonScalarKind.Missing;
}
