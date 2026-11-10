namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Units;

using SQLite;

/// <summary>An extra (distance, ammo...) of a <see cref="UnitUpgradePartRow"/>.</summary>
[Table("unit_upgrade_extras")]
public class UnitUpgradeExtraRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public int UpgradeIndex { get; set; }

    public int PartPosition { get; set; }

    public int Position { get; set; }

    public int ExtraId { get; set; }
}
