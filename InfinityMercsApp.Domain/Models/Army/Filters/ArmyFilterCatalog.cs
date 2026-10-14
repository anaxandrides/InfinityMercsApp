namespace InfinityMercsApp.Domain.Models.Army.Filters;

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
