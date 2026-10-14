namespace InfinityMercsApp.Infrastructure.Models.Database.Army.SpecOps;

using SQLite;

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
