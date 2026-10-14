namespace InfinityMercsApp.Infrastructure.Models.Database.Metadata;

using SQLite;

/// <summary>
/// One range band of a weapon (<c>short</c>, <c>med</c>, <c>long</c>, <c>max</c>, and rarely others such
/// as <c>xlong</c>): the band's upper bound and its modifier text.
/// </summary>
[Table("weapon_distance_bands")]
public class WeaponDistanceBandRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public string WeaponKey { get; set; } = string.Empty;

    public int Position { get; set; }

    public string Band { get; set; } = string.Empty;

    public int Max { get; set; }

    public string Mod { get; set; } = "0";
}
