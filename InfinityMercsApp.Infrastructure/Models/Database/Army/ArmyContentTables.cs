namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

// The typed content of an army (the armies table row's filters, relations and fireteam chart).

/// <summary>
/// One item of an army filter section: the id → name/link tables that unit entries refer to.
/// Numeric-id sections use <see cref="ItemId"/>; text-id sections (<c>attrs</c>, <c>swc</c>) use
/// <see cref="ItemKey"/> with <see cref="Abbr"/>, <see cref="Min"/> and <see cref="Max"/>.
/// </summary>
[Table("army_filter_items")]
public class ArmyFilterItemRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public string Section { get; set; } = string.Empty;

    /// <summary>Order of the section within the army's filters.</summary>
    public int SectionIndex { get; set; }

    public int Position { get; set; }

    public int? ItemId { get; set; }

    public string? ItemKey { get; set; }

    public string? Name { get; set; }

    public string? Type { get; set; }

    public string? Url { get; set; }

    public string? Abbr { get; set; }

    public int? Min { get; set; }

    public int? Max { get; set; }

    public bool Mercs { get; set; }

    public bool SpecOps { get; set; }
}

[Table("army_relations")]
public class ArmyRelationRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int RelationIndex { get; set; }

    public int? Min { get; set; }

    public int? Max { get; set; }

    public bool? IsGroup { get; set; }
}

[Table("army_relation_units")]
public class ArmyRelationUnitRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int RelationIndex { get; set; }

    public int Position { get; set; }

    public int UnitId { get; set; }
}

/// <summary>A fireteam chart <c>spec</c> entry, e.g. CORE = 1.</summary>
[Table("fireteam_chart_specs")]
public class FireteamChartSpecRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int Position { get; set; }

    public string Key { get; set; } = string.Empty;

    public int? Value { get; set; }
}

[Table("fireteam_chart_teams")]
public class FireteamChartTeamRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int TeamIndex { get; set; }

    public string? Name { get; set; }

    /// <summary>True when the payload sends <c>type</c> as an array (its items are in the team types table).</summary>
    public bool HasTypeList { get; set; }

    /// <summary>The <c>type</c> when the payload sends a single string.</summary>
    public string? TypeText { get; set; }
}

[Table("fireteam_chart_team_types")]
public class FireteamChartTeamTypeRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int TeamIndex { get; set; }

    public int Position { get; set; }

    public string Type { get; set; } = string.Empty;
}

[Table("fireteam_chart_team_units")]
public class FireteamChartTeamUnitRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int TeamIndex { get; set; }

    public int Position { get; set; }

    public string? Name { get; set; }

    public string? Slug { get; set; }

    public string? Comment { get; set; }

    public int? Min { get; set; }

    public int? Max { get; set; }

    public bool MinIsAsterisk { get; set; }

    public bool? Required { get; set; }
}

/// <summary>
/// An item a spec-ops choice links to (<see cref="LinkKind"/> = <c>skills</c>, <c>equip</c> or
/// <c>weapons</c>), or one of the choice's own extras (<see cref="LinkKind"/> = <c>extras</c>).
/// <see cref="OwnerKey"/> is the choice's spec-ops skill/equipment/weapon key.
/// </summary>
[Table("specops_links")]
public class SpecopsLinkRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public string LinkKind { get; set; } = string.Empty;

    public int Position { get; set; }

    public int LinkedId { get; set; }
}

/// <summary>An extra of a linked spec-ops item (e.g. the extra of a skill granted with equipment).</summary>
[Table("specops_link_extras")]
public class SpecopsLinkExtraRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public string LinkKind { get; set; } = string.Empty;

    public int LinkPosition { get; set; }

    public int Position { get; set; }

    public int ExtraId { get; set; }
}

/// <summary>A CORE fireteam name found valid for a cohesive-company faction filter.</summary>
[Table("cc_faction_valid_core_fireteams")]
public class CCFactionValidCoreFireteamRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public string CacheKey { get; set; } = string.Empty;

    public int Position { get; set; }

    public string Name { get; set; } = string.Empty;
}
