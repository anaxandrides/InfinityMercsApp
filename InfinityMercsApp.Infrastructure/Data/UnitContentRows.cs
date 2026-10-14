using InfinityMercsApp.Domain.Models.Army.Profiles;
using InfinityMercsApp.Infrastructure.Models.Database.Army;

namespace InfinityMercsApp.Infrastructure.Data;

/// <summary>
/// The typed content of one unit (an army unit or a spec-ops unit): its profile groups, the options
/// declared on the unit itself, its own filter ids and the armies it belongs to.
/// </summary>
public sealed record UnitContent(
    UnitProfileGroups ProfileGroups,
    IReadOnlyList<ProfileOption> Options,
    UnitFilterIds Filters,
    IReadOnlyList<int> FactionIds)
{
    public static UnitContent Empty { get; } = new(UnitProfileGroups.Empty, [], UnitFilterIds.Empty, []);
}

/// <summary>
/// The rows of the unit content tables, and the mapping between them and <see cref="UnitContent"/>.
/// Use <see cref="Add"/> to turn content into rows for insertion, or fill the lists from the database and
/// call <see cref="Assemble"/> to rebuild the content of every unit they describe.
/// </summary>
public sealed class UnitContentRows
{
    private const int UnitOptionGroupIndex = -1;

    private static readonly IReadOnlyList<ProfileEntryKind> EntryKinds = Enum.GetValues<ProfileEntryKind>();

    public List<UnitProfileGroupRow> Groups { get; } = [];

    public List<UnitProfileRow> Profiles { get; } = [];

    public List<UnitProfileMoveRow> Moves { get; } = [];

    public List<UnitOptionRow> Options { get; } = [];

    public List<UnitOptionOrderRow> Orders { get; } = [];

    public List<UnitOptionIncludeRow> Includes { get; } = [];

    public List<UnitEntryRow> Entries { get; } = [];

    public List<UnitEntryExtraRow> Extras { get; } = [];

    public List<UnitFilterIdRow> FilterIds { get; } = [];

    public List<UnitFactionRow> Factions { get; } = [];

    /// <summary>Adds the rows describing <paramref name="content"/> for the unit <paramref name="ownerKey"/>.</summary>
    public void Add(int factionId, string ownerKey, UnitContent content)
    {
        var groups = content.ProfileGroups.Groups;
        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            var group = groups[groupIndex];
            Groups.Add(new UnitProfileGroupRow
            {
                FactionId = factionId,
                OwnerKey = ownerKey,
                GroupIndex = groupIndex,
                GroupId = group.Id,
                Isc = group.Isc,
                Category = group.Category
            });

            for (var profileIndex = 0; profileIndex < group.Profiles.Count; profileIndex++)
            {
                AddProfile(factionId, ownerKey, groupIndex, profileIndex, group.Profiles[profileIndex]);
            }

            for (var optionIndex = 0; optionIndex < group.Options.Count; optionIndex++)
            {
                AddOption(factionId, ownerKey, UnitEntryOwner.Option, groupIndex, optionIndex, group.Options[optionIndex]);
            }
        }

        for (var optionIndex = 0; optionIndex < content.Options.Count; optionIndex++)
        {
            AddOption(factionId, ownerKey, UnitEntryOwner.UnitOption, UnitOptionGroupIndex, optionIndex, content.Options[optionIndex]);
        }

        foreach (var (section, ids) in FilterSections(content.Filters))
        {
            for (var position = 0; position < ids.Count; position++)
            {
                FilterIds.Add(new UnitFilterIdRow
                {
                    FactionId = factionId,
                    OwnerKey = ownerKey,
                    Section = section,
                    Position = position,
                    Value = ids[position]
                });
            }
        }

        for (var position = 0; position < content.FactionIds.Count; position++)
        {
            Factions.Add(new UnitFactionRow
            {
                FactionId = factionId,
                OwnerKey = ownerKey,
                Position = position,
                MemberFactionId = content.FactionIds[position]
            });
        }
    }

    /// <summary>
    /// Rebuilds the content of every unit the rows describe, keyed by owner key. Units that only appear in
    /// <paramref name="ownerKeys"/> (no content rows at all) get <see cref="UnitContent.Empty"/>.
    /// </summary>
    public Dictionary<string, UnitContent> Assemble(IEnumerable<string>? ownerKeys = null)
    {
        var builders = new Dictionary<string, UnitBuilder>(StringComparer.Ordinal);
        UnitBuilder Builder(string ownerKey)
        {
            if (!builders.TryGetValue(ownerKey, out var builder))
            {
                builder = new UnitBuilder();
                builders[ownerKey] = builder;
            }

            return builder;
        }

        foreach (var row in Groups)
        {
            Builder(row.OwnerKey).Group(row.GroupIndex).Row = row;
        }

        foreach (var row in Profiles)
        {
            Builder(row.OwnerKey).Group(row.GroupIndex).Profile(row.ProfileIndex).Row = row;
        }

        foreach (var row in Moves)
        {
            Builder(row.OwnerKey).Group(row.GroupIndex).Profile(row.ProfileIndex).Moves.Add((row.Position, row.Cm));
        }

        foreach (var row in Options)
        {
            Builder(row.OwnerKey).Option(row.Owner, row.GroupIndex, row.OptionIndex).Row = row;
        }

        foreach (var row in Orders)
        {
            Builder(row.OwnerKey).Option(row.Owner, row.GroupIndex, row.OptionIndex).Orders
                .Add((row.Position, new OrderEntry(row.Type, row.List, row.Total)));
        }

        foreach (var row in Includes)
        {
            Builder(row.OwnerKey).Option(row.Owner, row.GroupIndex, row.OptionIndex).Includes
                .Add((row.Position, new IncludeReference(row.IncludedGroupId, row.IncludedOptionId)));
        }

        foreach (var row in Entries)
        {
            Builder(row.OwnerKey).EntryList(row.Owner, row.GroupIndex, row.OwnerIndex, (ProfileEntryKind)row.Kind)
                .Entry(row.Position).Row = row;
        }

        foreach (var row in Extras)
        {
            Builder(row.OwnerKey).EntryList(row.Owner, row.GroupIndex, row.OwnerIndex, (ProfileEntryKind)row.Kind)
                .Entry(row.EntryPosition).Extras.Add((row.Position, row.ExtraId));
        }

        foreach (var row in FilterIds)
        {
            Builder(row.OwnerKey).Filters(row.Section).Add((row.Position, row.Value));
        }

        foreach (var row in Factions)
        {
            Builder(row.OwnerKey).FactionIds.Add((row.Position, row.MemberFactionId));
        }

        var result = new Dictionary<string, UnitContent>(builders.Count, StringComparer.Ordinal);
        foreach (var (ownerKey, builder) in builders)
        {
            result[ownerKey] = builder.Build();
        }

        foreach (var ownerKey in ownerKeys ?? [])
        {
            result.TryAdd(ownerKey, UnitContent.Empty);
        }

        return result;
    }

    private void AddProfile(int factionId, string ownerKey, int groupIndex, int profileIndex, UnitProfile profile)
    {
        Profiles.Add(new UnitProfileRow
        {
            FactionId = factionId,
            OwnerKey = ownerKey,
            GroupIndex = groupIndex,
            ProfileIndex = profileIndex,
            ProfileId = profile.Id,
            Name = profile.Name,
            Logo = profile.Logo,
            Type = profile.Type,
            Str = profile.Str,
            StrValue = profile.StrValue,
            Move = profile.Move,
            Cc = profile.Cc,
            Bs = profile.Bs,
            Ph = profile.Ph,
            Wip = profile.Wip,
            Arm = profile.Arm,
            Bts = profile.Bts,
            W = profile.W,
            S = profile.S,
            Ava = profile.Ava,
            Vita = profile.Vita,
            HasStatFields = profile.HasStatFields
        });

        for (var position = 0; position < profile.MoveCm.Count; position++)
        {
            Moves.Add(new UnitProfileMoveRow
            {
                FactionId = factionId,
                OwnerKey = ownerKey,
                GroupIndex = groupIndex,
                ProfileIndex = profileIndex,
                Position = position,
                Cm = profile.MoveCm[position]
            });
        }

        foreach (var kind in EntryKinds)
        {
            AddEntries(factionId, ownerKey, UnitEntryOwner.Profile, groupIndex, profileIndex, kind, profile.Entries(kind));
        }
    }

    private void AddOption(int factionId, string ownerKey, UnitEntryOwner owner, int groupIndex, int optionIndex, ProfileOption option)
    {
        Options.Add(new UnitOptionRow
        {
            FactionId = factionId,
            OwnerKey = ownerKey,
            Owner = owner,
            GroupIndex = groupIndex,
            OptionIndex = optionIndex,
            OptionId = option.Id,
            Name = option.Name,
            Swc = option.Swc,
            Cost = option.Cost,
            Minis = option.Minis,
            Disabled = option.Disabled
        });

        for (var position = 0; position < option.Orders.Count; position++)
        {
            var order = option.Orders[position];
            Orders.Add(new UnitOptionOrderRow
            {
                FactionId = factionId,
                OwnerKey = ownerKey,
                Owner = owner,
                GroupIndex = groupIndex,
                OptionIndex = optionIndex,
                Position = position,
                Type = order.Type,
                List = order.List,
                Total = order.Total
            });
        }

        for (var position = 0; position < option.Includes.Count; position++)
        {
            var include = option.Includes[position];
            Includes.Add(new UnitOptionIncludeRow
            {
                FactionId = factionId,
                OwnerKey = ownerKey,
                Owner = owner,
                GroupIndex = groupIndex,
                OptionIndex = optionIndex,
                Position = position,
                IncludedGroupId = include.GroupId,
                IncludedOptionId = include.OptionId
            });
        }

        foreach (var kind in EntryKinds)
        {
            AddEntries(factionId, ownerKey, owner, groupIndex, optionIndex, kind, option.Entries(kind));
        }
    }

    private void AddEntries(
        int factionId,
        string ownerKey,
        UnitEntryOwner owner,
        int groupIndex,
        int ownerIndex,
        ProfileEntryKind kind,
        IReadOnlyList<ProfileEntry> entries)
    {
        for (var position = 0; position < entries.Count; position++)
        {
            var entry = entries[position];
            Entries.Add(new UnitEntryRow
            {
                FactionId = factionId,
                OwnerKey = ownerKey,
                Owner = owner,
                GroupIndex = groupIndex,
                OwnerIndex = ownerIndex,
                Kind = (int)kind,
                Position = position,
                EntryId = entry.Id,
                Quantity = entry.Quantity,
                DisplayOrder = entry.Order
            });

            for (var extraPosition = 0; extraPosition < entry.Extras.Count; extraPosition++)
            {
                Extras.Add(new UnitEntryExtraRow
                {
                    FactionId = factionId,
                    OwnerKey = ownerKey,
                    Owner = owner,
                    GroupIndex = groupIndex,
                    OwnerIndex = ownerIndex,
                    Kind = (int)kind,
                    EntryPosition = position,
                    Position = extraPosition,
                    ExtraId = entry.Extras[extraPosition]
                });
            }
        }
    }

    private static IEnumerable<(string Section, IReadOnlyList<int> Ids)> FilterSections(UnitFilterIds filters)
    {
        yield return (UnitFilterSection.Categories, filters.Categories);
        yield return (UnitFilterSection.Types, filters.Types);
        yield return (UnitFilterSection.Chars, filters.Chars);
        yield return (UnitFilterSection.Skills, filters.Skills);
        yield return (UnitFilterSection.Equip, filters.Equip);
        yield return (UnitFilterSection.Weapons, filters.Weapons);
        yield return (UnitFilterSection.Ammunition, filters.Ammunition);
    }

    private static IReadOnlyList<T> Ordered<T>(List<(int Position, T Value)> items) =>
        items.Count == 0 ? [] : items.OrderBy(item => item.Position).Select(item => item.Value).ToList();

    /// <summary>Section names of <see cref="UnitFilterIdRow.Section"/>.</summary>
    private static class UnitFilterSection
    {
        public const string Categories = "categories";
        public const string Types = "types";
        public const string Chars = "chars";
        public const string Skills = "skills";
        public const string Equip = "equip";
        public const string Weapons = "weapons";
        public const string Ammunition = "ammunition";
    }

    private sealed class UnitBuilder
    {
        private readonly SortedDictionary<int, GroupBuilder> _groups = new();
        private readonly SortedDictionary<int, OptionBuilder> _unitOptions = new();
        private readonly Dictionary<string, List<(int, int)>> _filters = new(StringComparer.Ordinal);

        public List<(int Position, int Value)> FactionIds { get; } = [];

        public GroupBuilder Group(int groupIndex)
        {
            if (!_groups.TryGetValue(groupIndex, out var group))
            {
                group = new GroupBuilder();
                _groups[groupIndex] = group;
            }

            return group;
        }

        public OptionBuilder Option(UnitEntryOwner owner, int groupIndex, int optionIndex) =>
            owner == UnitEntryOwner.UnitOption ? GetOrAdd(_unitOptions, optionIndex) : Group(groupIndex).Option(optionIndex);

        public EntryListBuilder EntryList(UnitEntryOwner owner, int groupIndex, int ownerIndex, ProfileEntryKind kind) =>
            owner == UnitEntryOwner.Profile
                ? Group(groupIndex).Profile(ownerIndex).EntryList(kind)
                : Option(owner, groupIndex, ownerIndex).EntryList(kind);

        public List<(int, int)> Filters(string section)
        {
            if (!_filters.TryGetValue(section, out var list))
            {
                list = [];
                _filters[section] = list;
            }

            return list;
        }

        public UnitContent Build()
        {
            var groups = _groups.Values.Select(group => group.Build()).ToList();
            var filters = new UnitFilterIds
            {
                Categories = FilterIds(UnitFilterSection.Categories),
                Types = FilterIds(UnitFilterSection.Types),
                Chars = FilterIds(UnitFilterSection.Chars),
                Skills = FilterIds(UnitFilterSection.Skills),
                Equip = FilterIds(UnitFilterSection.Equip),
                Weapons = FilterIds(UnitFilterSection.Weapons),
                Ammunition = FilterIds(UnitFilterSection.Ammunition)
            };

            return new UnitContent(
                groups.Count == 0 ? UnitProfileGroups.Empty : new UnitProfileGroups(groups),
                _unitOptions.Values.Select(option => option.Build()).ToList(),
                filters,
                Ordered(FactionIds));
        }

        private IReadOnlyList<int> FilterIds(string section) =>
            _filters.TryGetValue(section, out var list) ? Ordered(list) : [];
    }

    private sealed class GroupBuilder
    {
        private readonly SortedDictionary<int, ProfileBuilder> _profiles = new();
        private readonly SortedDictionary<int, OptionBuilder> _options = new();

        public UnitProfileGroupRow? Row { get; set; }

        public ProfileBuilder Profile(int profileIndex) => GetOrAdd(_profiles, profileIndex);

        public OptionBuilder Option(int optionIndex) => GetOrAdd(_options, optionIndex);

        public ProfileGroup Build() => new()
        {
            Id = Row?.GroupId,
            Isc = Row?.Isc,
            Category = Row?.Category,
            Profiles = _profiles.Values.Select(profile => profile.Build()).ToList(),
            Options = _options.Values.Select(option => option.Build()).ToList()
        };
    }

    private sealed class ProfileBuilder
    {
        private readonly Dictionary<ProfileEntryKind, EntryListBuilder> _entries = [];

        public UnitProfileRow? Row { get; set; }

        public List<(int Position, int Cm)> Moves { get; } = [];

        public EntryListBuilder EntryList(ProfileEntryKind kind) => GetOrAdd(_entries, kind);

        public UnitProfile Build()
        {
            var row = Row ?? new UnitProfileRow();
            return new UnitProfile
            {
                Id = row.ProfileId,
                Name = row.Name,
                Logo = row.Logo,
                Type = row.Type,
                Str = row.Str,
                StrValue = row.StrValue,
                Move = row.Move,
                MoveCm = Ordered(Moves),
                Cc = row.Cc,
                Bs = row.Bs,
                Ph = row.Ph,
                Wip = row.Wip,
                Arm = row.Arm,
                Bts = row.Bts,
                W = row.W,
                S = row.S,
                Ava = row.Ava,
                Vita = row.Vita,
                HasStatFields = row.HasStatFields,
                Chars = Entries(ProfileEntryKind.Chars),
                Skills = Entries(ProfileEntryKind.Skills),
                Equip = Entries(ProfileEntryKind.Equip),
                Weapons = Entries(ProfileEntryKind.Weapons),
                Peripheral = Entries(ProfileEntryKind.Peripheral),
                Ammunition = Entries(ProfileEntryKind.Ammunition),
                Ammo = Entries(ProfileEntryKind.Ammo)
            };
        }

        private IReadOnlyList<ProfileEntry> Entries(ProfileEntryKind kind) =>
            _entries.TryGetValue(kind, out var list) ? list.Build() : [];
    }

    private sealed class OptionBuilder
    {
        private readonly Dictionary<ProfileEntryKind, EntryListBuilder> _entries = [];

        public UnitOptionRow? Row { get; set; }

        public List<(int Position, OrderEntry Order)> Orders { get; } = [];

        public List<(int Position, IncludeReference Include)> Includes { get; } = [];

        public EntryListBuilder EntryList(ProfileEntryKind kind) => GetOrAdd(_entries, kind);

        public ProfileOption Build()
        {
            var row = Row ?? new UnitOptionRow();
            return new ProfileOption
            {
                Id = row.OptionId,
                Name = row.Name,
                Swc = row.Swc,
                Cost = row.Cost,
                Minis = row.Minis,
                Disabled = row.Disabled,
                Orders = Ordered(Orders),
                Includes = Ordered(Includes),
                Chars = Entries(ProfileEntryKind.Chars),
                Skills = Entries(ProfileEntryKind.Skills),
                Equip = Entries(ProfileEntryKind.Equip),
                Weapons = Entries(ProfileEntryKind.Weapons),
                Peripheral = Entries(ProfileEntryKind.Peripheral),
                Ammunition = Entries(ProfileEntryKind.Ammunition),
                Ammo = Entries(ProfileEntryKind.Ammo)
            };
        }

        private IReadOnlyList<ProfileEntry> Entries(ProfileEntryKind kind) =>
            _entries.TryGetValue(kind, out var list) ? list.Build() : [];
    }

    private sealed class EntryListBuilder
    {
        private readonly SortedDictionary<int, EntryBuilder> _entries = new();

        public EntryBuilder Entry(int position) => GetOrAdd(_entries, position);

        public IReadOnlyList<ProfileEntry> Build() => _entries.Values.Select(entry => entry.Build()).ToList();
    }

    private sealed class EntryBuilder
    {
        public UnitEntryRow? Row { get; set; }

        public List<(int Position, int ExtraId)> Extras { get; } = [];

        public ProfileEntry Build()
        {
            var row = Row ?? new UnitEntryRow();
            return new ProfileEntry(row.EntryId, row.Quantity, row.DisplayOrder, Ordered(Extras));
        }
    }

    private static TValue GetOrAdd<TKey, TValue>(IDictionary<TKey, TValue> map, TKey key)
        where TValue : new()
    {
        if (!map.TryGetValue(key, out var value))
        {
            value = new TValue();
            map[key] = value;
        }

        return value;
    }
}
