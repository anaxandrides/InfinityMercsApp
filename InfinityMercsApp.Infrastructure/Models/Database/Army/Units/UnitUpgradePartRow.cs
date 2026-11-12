namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Units;

using SQLite;

/// <summary>
/// One part of an upgrade in a Spec-Ops or Team-Ops unit's table. The parts of one upgrade share
/// <see cref="UpgradeIndex"/>; <see cref="Kind"/> holds the domain <c>SpecUpgradePartKind</c>.
/// </summary>
[Table("unit_upgrade_parts")]
public class UnitUpgradePartRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public int UpgradeIndex { get; set; }

    public int Position { get; set; }

    public int Kind { get; set; }

    public int? EntryId { get; set; }

    public string? StatKey { get; set; }

    public int Quantity { get; set; } = 1;
}
