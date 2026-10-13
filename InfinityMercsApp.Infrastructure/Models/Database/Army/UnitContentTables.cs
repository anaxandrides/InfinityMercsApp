namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

// The typed content of a unit (army units and spec-ops units alike), one row per item.
// Every row carries the owning army (FactionId) so a whole army can be loaded or deleted in one query,
// and the owning unit (OwnerKey: the unit's UnitKey or SpecopsUnitKey). Positions keep payload order.

/// <summary>Distinguishes which list a <see cref="UnitEntryRow"/> or option row hangs off.</summary>
public enum UnitEntryOwner
{
    /// <summary>A profile (statline) of a profile group.</summary>
    Profile = 0,

    /// <summary>An option of a profile group.</summary>
    Option = 1,

    /// <summary>An option declared on the unit itself rather than in a profile group.</summary>
    UnitOption = 2
}

[Table("unit_profile_groups")]
public class UnitProfileGroupRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public int GroupIndex { get; set; }

    public int? GroupId { get; set; }

    public string? Isc { get; set; }

    public int? Category { get; set; }
}

[Table("unit_profiles")]
public class UnitProfileRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public int GroupIndex { get; set; }

    public int ProfileIndex { get; set; }

    public int? ProfileId { get; set; }

    public string? Name { get; set; }

    public string? Logo { get; set; }

    public int? Type { get; set; }

    public bool Str { get; set; }

    public int? StrValue { get; set; }

    public string Move { get; set; } = "-";

    public int? Cc { get; set; }

    public int? Bs { get; set; }

    public int? Ph { get; set; }

    public int? Wip { get; set; }

    public int? Arm { get; set; }

    public int? Bts { get; set; }

    public int? W { get; set; }

    public int? S { get; set; }

    public int? Ava { get; set; }

    public int? Vita { get; set; }

    public bool HasStatFields { get; set; }
}

/// <summary>A profile's movement values in centimetres, e.g. 10 then 5 for "10-5".</summary>
[Table("unit_profile_moves")]
public class UnitProfileMoveRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public int GroupIndex { get; set; }

    public int ProfileIndex { get; set; }

    public int Position { get; set; }

    public int Cm { get; set; }
}

/// <summary>
/// An option of a profile group, or (with <see cref="Owner"/> = <see cref="UnitEntryOwner.UnitOption"/> and
/// <see cref="GroupIndex"/> = -1) an option declared on the unit itself.
/// </summary>
[Table("unit_options")]
public class UnitOptionRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public UnitEntryOwner Owner { get; set; }

    public int GroupIndex { get; set; }

    public int OptionIndex { get; set; }

    public int? OptionId { get; set; }

    public string? Name { get; set; }

    public string Swc { get; set; } = "-";

    public string Cost { get; set; } = "-";

    public int Minis { get; set; }

    public bool Disabled { get; set; }
}

[Table("unit_option_orders")]
public class UnitOptionOrderRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public UnitEntryOwner Owner { get; set; }

    public int GroupIndex { get; set; }

    public int OptionIndex { get; set; }

    public int Position { get; set; }

    public string? Type { get; set; }

    public int? List { get; set; }

    public int? Total { get; set; }
}

/// <summary>An option that also grants another option's entries.</summary>
[Table("unit_option_includes")]
public class UnitOptionIncludeRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public UnitEntryOwner Owner { get; set; }

    public int GroupIndex { get; set; }

    public int OptionIndex { get; set; }

    public int Position { get; set; }

    public int IncludedGroupId { get; set; }

    public int IncludedOptionId { get; set; }
}

/// <summary>
/// One characteristic, skill, piece of equipment, weapon, peripheral or ammunition reference of a profile
/// or option. <see cref="Kind"/> holds the domain <c>ProfileEntryKind</c>.
/// </summary>
[Table("unit_entries")]
public class UnitEntryRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public UnitEntryOwner Owner { get; set; }

    public int GroupIndex { get; set; }

    /// <summary>The profile index or option index, depending on <see cref="Owner"/>.</summary>
    public int OwnerIndex { get; set; }

    public int Kind { get; set; }

    public int Position { get; set; }

    public int? EntryId { get; set; }

    public int Quantity { get; set; } = 1;

    /// <summary>The API display order (<c>order</c>); null when none was sent.</summary>
    public int? DisplayOrder { get; set; }
}

/// <summary>An extra (distance, ammo modifier...) of a <see cref="UnitEntryRow"/>.</summary>
[Table("unit_entry_extras")]
public class UnitEntryExtraRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public UnitEntryOwner Owner { get; set; }

    public int GroupIndex { get; set; }

    public int OwnerIndex { get; set; }

    public int Kind { get; set; }

    public int EntryPosition { get; set; }

    public int Position { get; set; }

    public int ExtraId { get; set; }
}

/// <summary>
/// An id from a unit's own filters: what the unit can field, per section
/// (<c>categories</c>, <c>types</c>, <c>chars</c>, <c>skills</c>, <c>equip</c>, <c>weapons</c>, <c>ammunition</c>).
/// </summary>
[Table("unit_filter_ids")]
public class UnitFilterIdRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public string Section { get; set; } = string.Empty;

    public int Position { get; set; }

    public int Value { get; set; }
}

/// <summary>An army the unit belongs to (the unit's <c>factions</c> list).</summary>
[Table("unit_factions")]
public class UnitFactionRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public int Position { get; set; }

    public int MemberFactionId { get; set; }
}
