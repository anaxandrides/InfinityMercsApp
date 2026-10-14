using System.Globalization;
using System.Text.Json.Serialization;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>
/// A JSON value the Infinity Army API is not consistent about (numbers that are sometimes strings, costs
/// that are sometimes absent). It keeps exactly what was sent so the mapping layer can apply the app's
/// interpretation rules in one place.
/// </summary>
[JsonConverter(typeof(JsonScalarConverter))]
internal readonly struct JsonScalar
{
    private readonly int? _int32;
    private readonly decimal? _decimal;

    private JsonScalar(JsonScalarKind kind, string? text, int? int32, decimal? @decimal)
    {
        Kind = kind;
        Text = text;
        _int32 = int32;
        _decimal = @decimal;
    }

    public JsonScalarKind Kind { get; }

    /// <summary>The string value, or the raw number text; <c>null</c> for other kinds.</summary>
    public string? Text { get; }

    public bool IsMissing => Kind == JsonScalarKind.Missing;

    public static JsonScalar Null { get; } = new(JsonScalarKind.Null, null, null, null);

    public static JsonScalar Other { get; } = new(JsonScalarKind.Other, null, null, null);

    public static JsonScalar FromNumber(string rawText, int? int32, decimal? @decimal) =>
        new(JsonScalarKind.Number, rawText, int32, @decimal);

    public static JsonScalar FromString(string? text) => new(JsonScalarKind.String, text ?? string.Empty, null, null);

    public static JsonScalar FromBool(bool value) => new(value ? JsonScalarKind.True : JsonScalarKind.False, null, null, null);

    /// <summary>A JSON number that fits in an <see cref="int"/>.</summary>
    public bool TryGetNumberInt32(out int value)
    {
        value = _int32 ?? 0;
        return Kind == JsonScalarKind.Number && _int32.HasValue;
    }

    /// <summary>A JSON number that fits in a <see cref="decimal"/>.</summary>
    public bool TryGetNumberDecimal(out decimal value)
    {
        value = _decimal ?? 0m;
        return Kind == JsonScalarKind.Number && _decimal.HasValue;
    }

    /// <summary>A JSON number that fits in an <see cref="int"/>, or a string holding one (current culture).</summary>
    public int? AsInt32()
    {
        if (TryGetNumberInt32(out var number))
        {
            return number;
        }

        return Kind == JsonScalarKind.String && int.TryParse(Text, out var parsed) ? parsed : null;
    }

    /// <summary>Like <see cref="AsInt32"/> but parses strings as invariant integers.</summary>
    public int? AsInvariantInt32()
    {
        if (TryGetNumberInt32(out var number))
        {
            return number;
        }

        return Kind == JsonScalarKind.String &&
               int.TryParse(Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    /// <summary><c>true</c>/<c>false</c> literals, or a string <c>"true"</c>/<c>"false"</c>.</summary>
    public bool? AsBool() => Kind switch
    {
        JsonScalarKind.True => true,
        JsonScalarKind.False => false,
        JsonScalarKind.String when bool.TryParse(Text, out var parsed) => parsed,
        _ => null
    };

    /// <summary>The string value when the payload sent a string; otherwise <c>null</c>.</summary>
    public string? AsString() => Kind == JsonScalarKind.String ? Text : null;

    public override string ToString() => $"{Kind}:{Text}";
}
