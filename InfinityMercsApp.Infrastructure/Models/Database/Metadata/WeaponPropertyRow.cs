namespace InfinityMercsApp.Infrastructure.Models.Database.Metadata;

using SQLite;

/// <summary>A weapon property, e.g. "Non-lethal". <see cref="WeaponKey"/> matches the weapons table.</summary>
[Table("weapon_properties")]
public class WeaponPropertyRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public string WeaponKey { get; set; } = string.Empty;

    public int Position { get; set; }

    public string Property { get; set; } = string.Empty;
}
