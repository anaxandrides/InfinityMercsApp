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
