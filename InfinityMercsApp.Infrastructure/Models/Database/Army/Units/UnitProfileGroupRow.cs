namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Units;

using SQLite;

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
