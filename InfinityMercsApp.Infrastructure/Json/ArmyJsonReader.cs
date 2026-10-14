using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army;
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

    /// <summary>Reads an army's <c>FireteamChartJson</c>.</summary>
    FireteamChart ReadFireteamChart(string? fireteamChartJson);

    /// <summary>
    /// Reads a stored id array such as a spec-ops choice's <c>ExtrasJson</c>: numbers, numeric strings and
    /// <c>{"id":..}</c> objects, in order; anything else is skipped.
    /// </summary>
    IReadOnlyList<int> ReadIdList(string? idListJson);

    /// <summary>Reads a unit's own <c>options</c> array (options declared outside its profile groups).</summary>
    IReadOnlyList<ProfileOption> ReadUnitOptions(string? unitOptionsJson);

    /// <summary>Reads an army's <c>relations</c> array.</summary>
    IReadOnlyList<ArmyRelation> ReadRelations(string? relationsJson);

    /// <summary>Reads an army's <c>reinforcements</c> value: the id of the army that provides them.</summary>
    int? ReadReinforcements(string? reinforcementsJson);

    /// <summary>Reads an army's <c>specops</c> object.</summary>
    SpecopsCatalog ReadSpecops(string? specopsJson);

    /// <summary>
    /// Reads a whole army payload as the API sends it. Returns <c>null</c> when the text is not a JSON object.
    /// </summary>
    ArmyDefinition? ReadArmyPayload(string? armyPayloadJson);
}

/// <summary>
/// <see cref="IArmyJsonReader"/> over the source-generated <see cref="ArmyJsonContext"/>.
/// Parsed results are immutable and cached by JSON content, so callers can ask for the same blob
/// repeatedly (e.g. while filtering a unit list) without re-parsing it.
/// </summary>
public sealed class ArmyJsonReader : IArmyJsonReader
{
    private const int MaxCachedEntries = 8192;

    private static readonly IReadOnlyList<string> StatContainerNames = ["stats", "Stats", "attributes", "Attributes", "attrs", "Attrs"];
    private static readonly IReadOnlyList<string> LinkPropertyOrder = ["url", "href", "link", "wiki", "web"];

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

        return MapUnitFilters(dto);
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

        return MapFactionIds(ids);
    }

    public FireteamChart ReadFireteamChart(string? fireteamChartJson)
    {
        if (string.IsNullOrWhiteSpace(fireteamChartJson))
        {
            return FireteamChart.Empty;
        }

        FireteamChartDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize(fireteamChartJson, ArmyJsonContext.Default.FireteamChart);
        }
        catch (JsonException)
        {
            return FireteamChart.Empty;
        }

        return MapFireteamChart(dto);
    }

    public IReadOnlyList<ProfileOption> ReadUnitOptions(string? unitOptionsJson)
    {
        if (string.IsNullOrWhiteSpace(unitOptionsJson))
        {
            return [];
        }

        try
        {
            var dtos = JsonSerializer.Deserialize(unitOptionsJson, ArmyJsonContext.Default.ProfileOptions);
            return MapOptions(dtos);
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public IReadOnlyList<ArmyRelation> ReadRelations(string? relationsJson)
    {
        if (string.IsNullOrWhiteSpace(relationsJson))
        {
            return [];
        }

        try
        {
            return MapRelations(JsonSerializer.Deserialize(relationsJson, ArmyJsonContext.Default.Relations));
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public int? ReadReinforcements(string? reinforcementsJson)
    {
        if (string.IsNullOrWhiteSpace(reinforcementsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(reinforcementsJson, ArmyJsonContext.Default.JsonScalar).AsInvariantInt32();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public SpecopsCatalog ReadSpecops(string? specopsJson)
    {
        if (string.IsNullOrWhiteSpace(specopsJson))
        {
            return SpecopsCatalog.Empty;
        }

        try
        {
            return MapSpecops(JsonSerializer.Deserialize(specopsJson, ArmyJsonContext.Default.Specops));
        }
        catch (JsonException)
        {
            return SpecopsCatalog.Empty;
        }
    }

    public ArmyDefinition? ReadArmyPayload(string? armyPayloadJson)
    {
        if (string.IsNullOrWhiteSpace(armyPayloadJson))
        {
            return null;
        }

        ArmyPayloadDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize(armyPayloadJson, ArmyJsonContext.Default.ArmyPayload);
        }
        catch (JsonException)
        {
            return null;
        }

        if (dto is null)
        {
            return null;
        }

        return new ArmyDefinition
        {
            Version = dto.Version.Text ?? string.Empty,
            Units = dto.Units?.Select(unit => MapUnit(unit, unit.Id ?? 0)).ToList() ?? [],
            Resume = dto.Resume?.Select(MapResume).ToList() ?? [],
            ReinforcementsFactionId = dto.Reinforcements.AsInvariantInt32(),
            Filters = MapArmyFilters(dto.Filters),
            Relations = MapRelations(dto.Relations),
            Specops = MapSpecops(dto.Specops),
            FireteamChart = MapFireteamChart(dto.FireteamChart)
        };
    }

    internal static FireteamChart MapFireteamChart(FireteamChartDto? dto)
    {
        if (dto is null)
        {
            return FireteamChart.Empty;
        }

        return new FireteamChart
        {
            Spec = dto.Spec?.Entries.Select(entry => new FireteamSpecEntry(entry.Key, entry.Value.AsInvariantInt32())).ToList() ?? [],
            Teams = dto.Teams?.Select(team => new FireteamTeam
            {
                Name = team.Name,
                TypeList = team.Type.Items?
                    .Where(item => item.Kind == JsonScalarKind.String)
                    .Select(item => item.Text ?? string.Empty)
                    .ToList(),
                TypeText = team.Type.Items is null ? team.Type.Text : null,
                Units = team.Units?.Select(unit => new FireteamUnitLimit
                {
                    Name = unit.Name,
                    Slug = unit.Slug,
                    Comment = unit.Comment,
                    Min = unit.Min.AsInvariantInt32(),
                    Max = unit.Max.AsInvariantInt32(),
                    MinIsAsterisk = string.Equals(unit.Min.AsString()?.Trim(), "*", StringComparison.Ordinal),
                    Required = unit.Required.AsBool()
                }).ToList() ?? []
            }).ToList() ?? []
        };
    }

    public IReadOnlyList<int> ReadIdList(string? idListJson)
    {
        if (string.IsNullOrWhiteSpace(idListJson))
        {
            return [];
        }

        try
        {
            var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(idListJson));
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
            {
                return [];
            }

            var ids = new List<int>();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (FlexibleIdConverter.ReadId(ref reader) is int id)
                {
                    ids.Add(id);
                }
            }

            // Reading past the array validates there is nothing after it.
            reader.Read();
            return ids;
        }
        catch (JsonException)
        {
            return [];
        }
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

        return MapProfileGroups(dtos);
    }

    internal static UnitProfileGroups MapProfileGroups(List<ProfileGroupDto>? dtos)
    {
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

    internal static UnitFilterIds MapUnitFilters(UnitFiltersDto? dto)
    {
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

    internal static IReadOnlyList<int> MapFactionIds(List<JsonScalar>? ids)
    {
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

    internal static IReadOnlyList<ProfileOption> MapOptions(List<ProfileOptionDto>? dtos) =>
        dtos?.Where(dto => dto is not null).Select(MapOption).ToList() ?? [];

    internal static IReadOnlyList<ArmyRelation> MapRelations(List<ArmyRelationDto>? dtos) =>
        dtos?.Where(dto => dto is not null).Select(dto => new ArmyRelation(
            dto.Min.AsInvariantInt32(),
            dto.Max.AsInvariantInt32(),
            dto.Group.AsBool(),
            dto.Units?.Where(unit => unit?.Unit is not null).Select(unit => unit.Unit!.Value).ToList() ?? [])).ToList() ?? [];

    internal static SpecopsCatalog MapSpecops(SpecopsDto? dto)
    {
        if (dto is null)
        {
            return SpecopsCatalog.Empty;
        }

        return new SpecopsCatalog
        {
            Skills = MapChoices(dto.Skills),
            Equip = MapChoices(dto.Equip),
            Weapons = MapChoices(dto.Weapons),
            // A spec-ops unit without an id cannot be stored or looked up.
            Units = dto.Units?.Where(unit => unit?.Id is not null).Select(unit => MapUnit(unit, unit.Id!.Value)).ToList() ?? []
        };
    }

    private static IReadOnlyList<SpecopsChoice>? MapChoices(List<SpecopsChoiceDto>? dtos) =>
        dtos?.Where(dto => dto?.Id is not null).Select(dto => new SpecopsChoice(
            dto.Id!.Value,
            dto.Exp.AsInvariantInt32() ?? 0,
            dto.Extras ?? [],
            MapLinks(dto.Skills),
            MapLinks(dto.Equip),
            MapLinks(dto.Weapons))).ToList();

    private static IReadOnlyList<SpecopsLink> MapLinks(List<SpecopsLinkDto>? dtos) =>
        dtos?.Select(link => new SpecopsLink(link.Id, link.Extras)).ToList() ?? [];

    private static ArmyUnitDefinition MapUnit(ArmyUnitDto dto, int id) => new()
    {
        Id = id,
        IdArmy = dto.IdArmy.AsInvariantInt32(),
        Canonical = dto.Canonical.AsInvariantInt32(),
        Isc = dto.Isc,
        IscAbbr = dto.IscAbbr,
        Name = dto.Name ?? string.Empty,
        Slug = dto.Slug,
        ProfileGroups = MapProfileGroups(dto.ProfileGroups),
        Options = MapOptions(dto.Options),
        Filters = MapUnitFilters(dto.Filters),
        FactionIds = MapFactionIds(dto.Factions)
    };

    private static ArmyImportResume MapResume(ArmyResumeDto dto) => new()
    {
        Id = dto.Id ?? 0,
        Isc = dto.Isc,
        IdArmy = dto.IdArmy.AsInvariantInt32(),
        Name = dto.Name ?? string.Empty,
        Slug = dto.Slug,
        Logo = dto.Logo,
        Type = dto.Type.AsInvariantInt32(),
        Category = dto.Category.AsInvariantInt32()
    };

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

        return MapArmyFilters(dto);
    }

    internal static ArmyFilterCatalog MapArmyFilters(Dictionary<string, ArmyFilterSectionDto>? dto)
    {
        if (dto is null)
        {
            return ArmyFilterCatalog.Empty;
        }

        var sections = new Dictionary<string, IReadOnlyList<ArmyFilterItem>>(StringComparer.Ordinal);
        var keyedSections = new Dictionary<string, IReadOnlyList<ArmyFilterKeyedItem>>(StringComparer.Ordinal);
        foreach (var (sectionName, section) in dto)
        {
            var items = new List<ArmyFilterItem>();
            var keyedItems = new List<ArmyFilterKeyedItem>();
            foreach (var item in section?.Items ?? [])
            {
                // Lookups need numeric ids (numbers or numeric strings); other ids ("cc", "0.5") are kept as keyed items.
                if (item.Id.AsInt32() is not int id)
                {
                    if (!string.IsNullOrEmpty(item.Id.Text))
                    {
                        keyedItems.Add(new ArmyFilterKeyedItem(
                            item.Id.Text,
                            item.Name,
                            item.Abbr,
                            item.Min.AsInvariantInt32(),
                            item.Max.AsInvariantInt32()));
                    }

                    continue;
                }

                items.Add(new ArmyFilterItem(id, item.Name, item.Type, FirstHttpLink(item))
                {
                    Mercs = item.Mercs.AsBool() == true,
                    SpecOps = item.SpecOps.AsBool() == true
                });
            }

            sections[sectionName] = items;
            if (keyedItems.Count > 0)
            {
                keyedSections[sectionName] = keyedItems;
            }
        }

        return new ArmyFilterCatalog(sections, keyedSections);
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
        StrValue = ReadStat(dto, dto.Str, "str"),
        Vita = ReadUndeclaredStat(dto, "vita"),
        MoveCm = ReadMoveCm(dto),
        HasStatFields = !dto.Cc.IsMissing || !dto.Bs.IsMissing || !dto.Ph.IsMissing || !dto.Wip.IsMissing ||
                        !dto.Arm.IsMissing || !dto.Bts.IsMissing || !dto.W.IsMissing || !dto.Str.IsMissing,
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
        if (dto.Move.Items is not null)
        {
            var parts = dto.Move.Items
                .Select(part => part.AsInt32())
                .Where(part => part.HasValue)
                .Select(part => part!.Value.ToString())
                .ToList();
            if (parts.Count > 0)
            {
                return string.Join("-", parts);
            }
        }
        else if (dto.Move.IsMissing &&
                 FindFlexible(dto, "move", skipDeclared: true) is { ValueKind: JsonValueKind.Array } moveElement)
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
    /// Movement values in cm: the <c>move</c> property (any letter case), else <c>mov</c>; an array of
    /// numbers/numeric strings or a <c>"10-5"</c> string.
    /// </summary>
    private static IReadOnlyList<int> ReadMoveCm(ProfileDto dto)
    {
        if (!dto.Move.IsMissing)
        {
            if (dto.Move.Items is not null)
            {
                return dto.Move.Items.Select(x => x.AsInvariantInt32()).Where(x => x.HasValue).Select(x => x!.Value).ToList();
            }

            return dto.Move.Text is not null ? SplitMoveText(dto.Move.Text) : [];
        }

        var element = FindIgnoringCase(dto.ExtensionData, "move") ?? FindIgnoringCase(dto.ExtensionData, "mov");
        if (element is not JsonElement value)
        {
            return [];
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => SplitMoveText(value.GetString() ?? string.Empty),
            JsonValueKind.Array => value.EnumerateArray()
                .Select(item => item.ValueKind switch
                {
                    JsonValueKind.Number when item.TryGetInt32(out var number) => (int?)number,
                    JsonValueKind.String when int.TryParse(item.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
                    _ => null
                })
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList(),
            _ => []
        };
    }

    private static List<int> SplitMoveText(string text) =>
        text.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? (int?)parsed : null)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .ToList();

    /// <summary>An undeclared property by exact name, else the first one whose name matches ignoring case.</summary>
    private static JsonElement? FindIgnoringCase(Dictionary<string, JsonElement>? extension, string name)
    {
        if (extension is null)
        {
            return null;
        }

        if (extension.TryGetValue(name, out var exact))
        {
            return exact;
        }

        foreach (var (key, value) in extension)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
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

        return ReadUndeclaredStat(dto, name, skipDeclared: true);
    }

    /// <summary>A stat the DTO does not declare (e.g. <c>vita</c>), looked up flexibly.</summary>
    private static int? ReadUndeclaredStat(ProfileDto dto, string name, bool skipDeclared = false)
    {
        if (FindFlexible(dto, name, skipDeclared) is JsonElement element)
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

        List<string> spellings =
        [
            name,
            name.ToLowerInvariant(),
            name.ToUpperInvariant(),
            char.ToUpperInvariant(name[0]) + name.Substring(1).ToLowerInvariant()
        ];
        var variants = spellings.Distinct(StringComparer.Ordinal).ToList();

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
