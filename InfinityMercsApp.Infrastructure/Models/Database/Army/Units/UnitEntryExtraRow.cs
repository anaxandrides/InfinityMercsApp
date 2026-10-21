namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Units;

using SQLite;

/// <summary>An extra (distance, ammo modifier...) of a <see cref="UnitEntryRow"/>.</summary>
[Table("unit_entry_extras")]
public class UnitEntryExtraRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public UnitEntryOwner Owner { get; set; }

    public int GroupIndex { get; set; }

    public int OwnerIndex { get; set; }

    public int Kind { get; set; }

    public int EntryPosition { get; set; }

    public int Position { get; set; }

    public int ExtraId { get; set; }
}
