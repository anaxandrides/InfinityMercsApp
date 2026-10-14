namespace InfinityMercsApp.Domain.Models.Army.Profiles;

/// <summary>
/// Section names of an army's <c>filters</c> payload: the id → name tables that profile entries refer to.
/// </summary>
public static class ArmyFilterSection
{
    public const string Ammunition = "ammunition";
    public const string Category = "category";
    public const string Chars = "chars";
    public const string Equip = "equip";
    public const string Extras = "extras";
    public const string Peripheral = "peripheral";
    public const string Points = "points";
    public const string Skills = "skills";
    public const string Swc = "swc";
    public const string Type = "type";
    public const string Weapons = "weapons";

    /// <summary>The section a kind of profile entry is named in; ammo entries share the ammunition section.</summary>
    public static string For(ProfileEntryKind kind) => kind switch
    {
        ProfileEntryKind.Chars => Chars,
        ProfileEntryKind.Skills => Skills,
        ProfileEntryKind.Equip => Equip,
        ProfileEntryKind.Weapons => Weapons,
        ProfileEntryKind.Peripheral => Peripheral,
        ProfileEntryKind.Ammunition or ProfileEntryKind.Ammo => Ammunition,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}

/// <summary>
/// An extra attached to an entry (e.g. a distance or ammo modifier) as described by the army filters.
/// </summary>
public sealed record ExtraDefinition(string Name, string Type, string? Url)
{
    public bool IsDistance => string.Equals(Type, "DISTANCE", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// One item of an army filter section.
/// </summary>
public sealed record ArmyFilterItem(int Id, string? Name, string? Type, string? Url)
{
    /// <summary>The item is flagged for MERCS (<c>"mercs": true</c>).</summary>
    public bool Mercs { get; init; }

    /// <summary>The item is flagged as a spec-ops choice (<c>"specops": true</c>).</summary>
    public bool SpecOps { get; init; }
}

/// <summary>
/// A filter item whose id is text rather than a number, e.g. the <c>attrs</c> section's
/// <c>{"id":"cc","name":"CC","abbr":"CC","min":0,"max":30}</c> or a fractional <c>swc</c> id such as "0.5".
/// </summary>
public sealed record ArmyFilterKeyedItem(string Key, string? Name, string? Abbr, int? Min, int? Max);

/// <summary>
/// An army's <c>filters</c> payload: for every section, the items with their names, types and wiki links.
/// </summary>
public sealed class ArmyFilterCatalog
{
    private static readonly IReadOnlyDictionary<int, string> EmptyLookup = new Dictionary<int, string>();

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<ArmyFilterKeyedItem>> NoKeyedSections =
        new Dictionary<string, IReadOnlyList<ArmyFilterKeyedItem>>();

    private readonly IReadOnlyDictionary<string, IReadOnlyList<ArmyFilterItem>> _sections;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<ArmyFilterKeyedItem>> _keyedSections;
    private readonly Dictionary<string, IReadOnlyDictionary<int, string>> _nameLookups = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IReadOnlyDictionary<int, string>> _linkLookups = new(StringComparer.Ordinal);
    private IReadOnlyDictionary<int, ExtraDefinition>? _extras;

    public static ArmyFilterCatalog Empty { get; } = new(new Dictionary<string, IReadOnlyList<ArmyFilterItem>>());

    public ArmyFilterCatalog(
        IReadOnlyDictionary<string, IReadOnlyList<ArmyFilterItem>> sections,
        IReadOnlyDictionary<string, IReadOnlyList<ArmyFilterKeyedItem>>? keyedSections = null)
    {
        _sections = sections;
        _keyedSections = keyedSections ?? NoKeyedSections;
    }

    /// <summary>Every section of the catalogue, in payload order.</summary>
    public IEnumerable<string> Sections => _sections.Keys;

    /// <summary>
    /// The sections that also hold text-id items (see <see cref="ArmyFilterKeyedItem"/>); every one of them is
    /// also listed in <see cref="Sections"/>.
    /// </summary>
    public IEnumerable<string> KeyedSections => _keyedSections.Keys;

    public IReadOnlyList<ArmyFilterItem> Items(string section) =>
        _sections.TryGetValue(section, out var items) ? items : [];

    public IReadOnlyList<ArmyFilterKeyedItem> KeyedItems(string section) =>
        _keyedSections.TryGetValue(section, out var items) ? items : [];

    /// <summary>
    /// Id → name for a section (e.g. <see cref="ArmyFilterSection.Skills"/>). Items without a name are skipped;
    /// when an id repeats, the later item wins.
    /// </summary>
    public IReadOnlyDictionary<int, string> Names(string section)
    {
        lock (_nameLookups)
        {
            if (_nameLookups.TryGetValue(section, out var cached))
            {
                return cached;
            }

            var map = new Dictionary<int, string>();
            foreach (var item in Items(section))
            {
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    map[item.Id] = item.Name;
                }
            }

            IReadOnlyDictionary<int, string> result = map.Count == 0 ? EmptyLookup : map;
            _nameLookups[section] = result;
            return result;
        }
    }

    /// <summary>Id → absolute http(s) link for a section; items without a link are skipped.</summary>
    public IReadOnlyDictionary<int, string> Links(string section)
    {
        lock (_linkLookups)
        {
            if (_linkLookups.TryGetValue(section, out var cached))
            {
                return cached;
            }

            var map = new Dictionary<int, string>();
            foreach (var item in Items(section))
            {
                if (!string.IsNullOrWhiteSpace(item.Url))
                {
                    map[item.Id] = item.Url;
                }
            }

            IReadOnlyDictionary<int, string> result = map.Count == 0 ? EmptyLookup : map;
            _linkLookups[section] = result;
            return result;
        }
    }

    /// <summary>Id → extra definition from the <see cref="ArmyFilterSection.Extras"/> section.</summary>
    public IReadOnlyDictionary<int, ExtraDefinition> Extras
    {
        get
        {
            if (_extras is not null)
            {
                return _extras;
            }

            var map = new Dictionary<int, ExtraDefinition>();
            foreach (var item in Items(ArmyFilterSection.Extras))
            {
                if (!string.IsNullOrWhiteSpace(item.Name))
                {
                    map[item.Id] = new ExtraDefinition(item.Name, item.Type ?? string.Empty, item.Url);
                }
            }

            _extras = map;
            return map;
        }
    }
}

/// <summary>
/// A unit's own <c>filters</c> payload: the ids of everything the unit can field, per section.
/// </summary>
public sealed class UnitFilterIds
{
    public static UnitFilterIds Empty { get; } = new();

    public IReadOnlyList<int> Categories { get; init; } = [];

    public IReadOnlyList<int> Types { get; init; } = [];

    public IReadOnlyList<int> Chars { get; init; } = [];

    public IReadOnlyList<int> Skills { get; init; } = [];

    public IReadOnlyList<int> Equip { get; init; } = [];

    public IReadOnlyList<int> Weapons { get; init; } = [];

    public IReadOnlyList<int> Ammunition { get; init; } = [];
}
