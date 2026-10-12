using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Infrastructure.Json;

/// <summary>
/// The single place that turns stored Infinity Army JSON (the SQLite blob columns such as
/// <c>ProfileGroupsJson</c> and <c>FiltersJson</c>) into typed domain models.
/// Missing or malformed JSON reads as an empty model; it never throws.
/// </summary>
public interface IArmyJsonReader
{
    /// <summary>Reads a unit's <c>ProfileGroupsJson</c>.</summary>
    UnitProfileGroups ReadProfileGroups(string? profileGroupsJson);

    /// <summary>Reads a unit's own <c>FiltersJson</c> (the ids it can field).</summary>
    UnitFilterIds ReadUnitFilters(string? unitFiltersJson);

    /// <summary>Reads an army's <c>FiltersJson</c> (the id → name/link tables).</summary>
    ArmyFilterCatalog ReadArmyFilters(string? armyFiltersJson);

    /// <summary>Reads a unit's <c>FactionsJson</c>: integer and numeric-string ids of a JSON array.</summary>
    IReadOnlyList<int> ReadFactionIds(string? factionsJson);
}

/// <summary>
/// <see cref="IArmyJsonReader"/> over the source-generated <see cref="ArmyJsonContext"/>.
/// Parsed results are immutable and cached by JSON content, so callers can ask for the same blob
/// repeatedly (e.g. while filtering a unit list) without re-parsing it.
/// </summary>
public sealed class ArmyJsonReader : IArmyJsonReader
{
    private const int MaxCachedEntries = 8192;

    private static readonly string[] StatContainerNames = ["stats", "Stats", "attributes", "Attributes", "attrs", "Attrs"];
    private static readonly string[] LinkPropertyOrder = ["url", "href", "link", "wiki", "web"];

    private readonly ConcurrentDictionary<string, UnitProfileGroups> _profileGroupsCache = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ArmyFilterCatalog> _armyFiltersCache = new(StringComparer.Ordinal);

    /// <summary>
    /// A process-wide instance for static helpers that cannot take constructor dependencies yet.
    /// DI registers this same instance, so both paths share one cache.
    /// </summary>
    public static ArmyJsonReader Shared { get; } = new();

    public UnitProfileGroups ReadProfileGroups(string? profileGroupsJson)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return UnitProfileGroups.Empty;
        }

        return GetOrAdd(_profileGroupsCache, profileGroupsJson, ParseProfileGroups);
    }

    public UnitFilterIds ReadUnitFilters(string? unitFiltersJson)
    {
        if (string.IsNullOrWhiteSpace(unitFiltersJson))
        {
            return UnitFilterIds.Empty;
        }

        UnitFiltersDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize(unitFiltersJson, ArmyJsonContext.Default.UnitFilters);
        }
        catch (JsonException)
        {
            return UnitFilterIds.Empty;
        }

        if (dto is null)
        {
            return UnitFilterIds.Empty;
        }

        return new UnitFilterIds
        {
            Categories = Ints(dto.Categories),
            Types = Ints(dto.Types),
            Chars = Ints(dto.Chars),
            Skills = Ints(dto.Skills),
            Equip = Ints(dto.Equip),
            Weapons = Ints(dto.Weapons),
            Ammunition = Ints(dto.Ammunition)
        };
    }

    public ArmyFilterCatalog ReadArmyFilters(string? armyFiltersJson)
    {
        if (string.IsNullOrWhiteSpace(armyFiltersJson))
        {
            return ArmyFilterCatalog.Empty;
        }

        return GetOrAdd(_armyFiltersCache, armyFiltersJson, ParseArmyFilters);
    }

    public IReadOnlyList<int> ReadFactionIds(string? factionsJson)
    {
        if (string.IsNullOrWhiteSpace(factionsJson))
        {
            return [];
        }

        // Generated company factions store an object here (source faction metadata), not an id array.
        List<JsonScalar>? ids;
        try
        {
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(factionsJson));
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            {
                return [];
            }

            ids = new JsonScalarListConverter().Read(ref reader, typeof(List<JsonScalar>), ArmyJsonContext.Default.Options);
            if (reader.Read())
            {
                // Trailing content after the array means the blob is not valid JSON.
                return [];
            }
        }
        catch (JsonException)
        {
            return [];
        }

        var result = new List<int>();
        foreach (var id in ids ?? [])
        {
            if (id.AsInvariantInt32() is int value)
            {
                result.Add(value);
            }
        }

        return result;
    }

    private static UnitProfileGroups ParseProfileGroups(string json)
    {
        List<ProfileGroupDto>? dtos;
        try
        {
            dtos = JsonSerializer.Deserialize(json, ArmyJsonContext.Default.ProfileGroups);
        }
        catch (JsonException)
        {
            return UnitProfileGroups.Empty;
        }
        catch (InvalidOperationException)
        {
            return UnitProfileGroups.Empty;
        }

        if (dtos is null || dtos.Count == 0)
        {
            return UnitProfileGroups.Empty;
        }

        var groups = new List<ProfileGroup>(dtos.Count);
        foreach (var dto in dtos)
        {
            if (dto is not null)
            {
                groups.Add(MapGroup(dto));
            }
        }

        return new UnitProfileGroups(groups);
    }

    private static ArmyFilterCatalog ParseArmyFilters(string json)
    {
        Dictionary<string, ArmyFilterSectionDto>? dto;
        try
        {
            dto = JsonSerializer.Deserialize(json, ArmyJsonContext.Default.ArmyFilters);
        }
        catch (JsonException)
        {
            return ArmyFilterCatalog.Empty;
        }

        if (dto is null)
        {
            return ArmyFilterCatalog.Empty;
        }

        var sections = new Dictionary<string, IReadOnlyList<ArmyFilterItem>>(StringComparer.Ordinal);
        foreach (var (sectionName, section) in dto)
        {
            var items = new List<ArmyFilterItem>();
            foreach (var item in section?.Items ?? [])
            {
                // Filter ids are numbers or numeric strings; an item without one cannot be looked up.
                if (item.Id.AsInt32() is not int id)
                {
                    continue;
                }

                items.Add(new ArmyFilterItem(id, item.Name, item.Type, FirstHttpLink(item)));
            }

            sections[sectionName] = items;
        }

        return new ArmyFilterCatalog(sections);
    }

    private static ProfileGroup MapGroup(ProfileGroupDto dto) => new()
    {
        Id = dto.Id,
        Isc = dto.Isc,
        Category = dto.Category.AsInt32(),
        Profiles = dto.Profiles?.Select(MapProfile).ToList() ?? [],
        Options = dto.Options?.Select(MapOption).ToList() ?? []
    };

    private static UnitProfile MapProfile(ProfileDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Logo = dto.Logo,
        Type = dto.Type.AsInt32(),
        Str = dto.Str.AsBool() ?? false,
        Move = ReadMove(dto),
        Cc = ReadStat(dto, dto.Cc, "cc"),
        Bs = ReadStat(dto, dto.Bs, "bs"),
        Ph = ReadStat(dto, dto.Ph, "ph"),
        Wip = ReadStat(dto, dto.Wip, "wip"),
        Arm = ReadStat(dto, dto.Arm, "arm"),
        Bts = ReadStat(dto, dto.Bts, "bts"),
        W = ReadStat(dto, dto.W, "w"),
        S = ReadStat(dto, dto.S, "s"),
        Ava = ReadStat(dto, dto.Ava, "ava"),
        Chars = MapEntries(dto.Chars),
        Skills = MapEntries(dto.Skills),
        Equip = MapEntries(dto.Equip),
        Weapons = MapEntries(dto.Weapons),
        Peripheral = MapEntries(dto.Peripheral),
        Ammunition = MapEntries(dto.Ammunition),
        Ammo = MapEntries(dto.Ammo)
    };

    private static ProfileOption MapOption(ProfileOptionDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Swc = ReadSwc(dto.Swc),
        Cost = ReadCostText(dto.Points) ?? ReadCostText(dto.Cost) ?? ReadCostText(dto.Pts) ?? "-",
        Minis = Math.Max(0, dto.Minis.AsInvariantInt32() ?? 0),
        Disabled = dto.Disabled.AsBool() ?? false,
        Orders = dto.Orders?.Select(order => new OrderEntry(order.Type, order.List.AsInt32(), order.Total.AsInt32())).ToList() ?? [],
        Includes = dto.Includes?
            .Where(include => include.Group.HasValue && include.Option.HasValue)
            .Select(include => new IncludeReference(include.Group!.Value, include.Option!.Value))
            .ToList() ?? [],
        Chars = MapEntries(dto.Chars),
        Skills = MapEntries(dto.Skills),
        Equip = MapEntries(dto.Equip),
        Weapons = MapEntries(dto.Weapons),
        Peripheral = MapEntries(dto.Peripheral),
        Ammunition = MapEntries(dto.Ammunition),
        Ammo = MapEntries(dto.Ammo)
    };

    private static IReadOnlyList<ProfileEntry> MapEntries(List<ProfileEntryDto>? entries)
    {
        if (entries is null || entries.Count == 0)
        {
            return [];
        }

        var mapped = new List<ProfileEntry>(entries.Count);
        foreach (var entry in entries)
        {
            if (!entry.IsObject)
            {
                mapped.Add(new ProfileEntry(entry.Id, 1, null, []));
                continue;
            }

            var quantity = Math.Max(1, entry.Q.AsInvariantInt32() ?? 1);
            int? order = entry.Order.TryGetNumberInt32(out var parsedOrder) ? parsedOrder : null;
            IReadOnlyList<int> extras = entry.Extra is null
                ? []
                : entry.Extra.Where(x => x.HasValue).Select(x => x!.Value).ToList();
            mapped.Add(new ProfileEntry(entry.Id, quantity, order, extras));
        }

        return mapped;
    }

    /// <summary>SWC as text: numbers in invariant form, strings as sent, otherwise "-".</summary>
    private static string ReadSwc(JsonScalar swc)
    {
        if (swc.TryGetNumberDecimal(out var number))
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        return swc.Kind == JsonScalarKind.String ? swc.Text ?? "-" : "-";
    }

    /// <summary>
    /// A cost field as text: whole numbers in invariant form, non-blank strings as sent, blank strings as "-".
    /// Returns <c>null</c> (try the next field) for anything else, including fractional numbers.
    /// </summary>
    private static string? ReadCostText(JsonScalar cost)
    {
        if (cost.TryGetNumberInt32(out var number))
        {
            return number.ToString(CultureInfo.InvariantCulture);
        }

        if (cost.Kind == JsonScalarKind.String)
        {
            return string.IsNullOrWhiteSpace(cost.Text) ? "-" : cost.Text;
        }

        return null;
    }

    /// <summary>
    /// Movement text from the <c>move</c> array ("10-5"); falls back to a <c>mov</c> value (array or string)
    /// for payloads that use that spelling.
    /// </summary>
    private static string ReadMove(ProfileDto dto)
    {
        if (dto.Move is not null)
        {
            var parts = dto.Move
                .Select(part => part.AsInt32())
                .Where(part => part.HasValue)
                .Select(part => part!.Value.ToString())
                .ToList();
            if (parts.Count > 0)
            {
                return string.Join("-", parts);
            }
        }
        else if (FindFlexible(dto, "move", skipDeclared: true) is { ValueKind: JsonValueKind.Array } moveElement)
        {
            var parts = moveElement.EnumerateArray()
                .Select(ReadIntText)
                .Where(part => part != "-")
                .ToList();
            if (parts.Count > 0)
            {
                return string.Join("-", parts);
            }
        }

        if (FindFlexible(dto, "mov", skipDeclared: false) is not JsonElement mov)
        {
            return "-";
        }

        if (mov.ValueKind == JsonValueKind.Array)
        {
            var parts = mov.EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out var n) ? n.ToString() : ScalarText(x))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            return parts.Count == 0 ? "-" : string.Join("-", parts);
        }

        return mov.ValueKind == JsonValueKind.String ? mov.GetString() ?? "-" : "-";
    }

    /// <summary>
    /// A stat as an integer: the profile's own property when present (number, or numeric string), otherwise
    /// the upper/title-case spelling, otherwise the same names inside a nested stats container.
    /// </summary>
    private static int? ReadStat(ProfileDto dto, JsonScalar declared, string name)
    {
        if (!declared.IsMissing)
        {
            return declared.AsInt32();
        }

        if (FindFlexible(dto, name, skipDeclared: true) is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Number when element.TryGetInt32(out var number) => number,
                JsonValueKind.String when int.TryParse(element.GetString(), out var parsed) => parsed,
                _ => null
            };
        }

        return null;
    }

    /// <summary>
    /// Looks a property up in the profile's undeclared properties by its lower/UPPER/Title spellings, then
    /// inside <c>stats</c>/<c>attributes</c>/<c>attrs</c> containers. When <paramref name="skipDeclared"/> is
    /// set, the lower-case spelling on the profile itself is skipped because the DTO already declares it.
    /// </summary>
    private static JsonElement? FindFlexible(ProfileDto dto, string name, bool skipDeclared)
    {
        var extension = dto.ExtensionData;
        if (extension is null || extension.Count == 0)
        {
            return null;
        }

        var variants = new[]
            {
                name,
                name.ToLowerInvariant(),
                name.ToUpperInvariant(),
                char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant()
            }
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var variant in variants)
        {
            if (skipDeclared && variant == name.ToLowerInvariant())
            {
                continue;
            }

            if (extension.TryGetValue(variant, out var value))
            {
                return value;
            }
        }

        foreach (var containerName in StatContainerNames)
        {
            if (!extension.TryGetValue(containerName, out var container) || container.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var variant in variants)
            {
                if (container.TryGetProperty(variant, out var value))
                {
                    return value;
                }
            }
        }

        return null;
    }

    private static string ReadIntText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number when element.TryGetInt32(out var number) => number.ToString(),
        JsonValueKind.String when int.TryParse(element.GetString(), out var parsed) => parsed.ToString(),
        _ => "-"
    };

    private static string? ScalarText(JsonElement element) =>
        element.ValueKind == JsonValueKind.String ? element.GetString() : null;

    private static string? FirstHttpLink(ArmyFilterItemDto item)
    {
        foreach (var key in LinkPropertyOrder)
        {
            var value = key switch
            {
                "url" => item.Url,
                "href" => item.Href,
                "link" => item.Link,
                "wiki" => item.Wiki,
                _ => item.Web
            };

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return value;
            }
        }

        return null;
    }

    private static IReadOnlyList<int> Ints(List<JsonScalar>? values)
    {
        if (values is null || values.Count == 0)
        {
            return [];
        }

        var ints = new List<int>(values.Count);
        foreach (var value in values)
        {
            if (value.AsInt32() is int id)
            {
                ints.Add(id);
            }
        }

        return ints;
    }

    private static T GetOrAdd<T>(ConcurrentDictionary<string, T> cache, string json, Func<string, T> parse)
    {
        if (cache.TryGetValue(json, out var cached))
        {
            return cached;
        }

        var parsed = parse(json);
        if (cache.Count >= MaxCachedEntries)
        {
            cache.Clear();
        }

        cache.TryAdd(json, parsed);
        return parsed;
    }
}
