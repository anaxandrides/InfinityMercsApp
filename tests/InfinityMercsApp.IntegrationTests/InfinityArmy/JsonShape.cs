using System.Text.Json;

namespace InfinityMercsApp.IntegrationTests.InfinityArmy;

/// <summary>
/// The shape of a JSON document: every property path, with list items under "[]" and numeric (id) keys under
/// "&lt;id&gt;", and the kinds of value seen there. Two documents of the same shape have the same paths.
/// </summary>
internal sealed class JsonShape
{
    private readonly Dictionary<string, HashSet<JsonValueKind>> _paths = new(StringComparer.Ordinal);

    private JsonShape()
    {
    }

    public IReadOnlyCollection<string> Paths => _paths.Keys;

    public static JsonShape Of(string json)
    {
        using var document = JsonDocument.Parse(json);
        var shape = new JsonShape();
        shape.Add("$", document.RootElement);
        return shape;
    }

    public bool Has(string path) => _paths.ContainsKey(path);

    /// <summary>The kinds of value at the path other than null, which any field may be.</summary>
    public IReadOnlySet<JsonValueKind> KindsAt(string path) =>
        _paths.TryGetValue(path, out var kinds) ? kinds.Where(kind => kind != JsonValueKind.Null).ToHashSet() : [];

    private void Add(string path, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var name = property.Name.All(char.IsAsciiDigit) ? "<id>" : property.Name;
                    Record($"{path}.{name}", property.Value);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Record($"{path}[]", item);
                }

                break;
        }
    }

    private void Record(string path, JsonElement value)
    {
        if (!_paths.TryGetValue(path, out var kinds))
        {
            _paths[path] = kinds = [];
        }

        // True and false are one kind of value.
        kinds.Add(value.ValueKind == JsonValueKind.False ? JsonValueKind.True : value.ValueKind);
        Add(path, value);
    }
}
