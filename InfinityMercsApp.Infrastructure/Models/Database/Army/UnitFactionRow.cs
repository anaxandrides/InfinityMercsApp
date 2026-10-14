namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

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
