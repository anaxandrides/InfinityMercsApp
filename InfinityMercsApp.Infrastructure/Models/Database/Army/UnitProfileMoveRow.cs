namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

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
