namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Units;

using SQLite;

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
