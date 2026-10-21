namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Units;

using SQLite;

/// <summary>
/// An option of a profile group, or (with <see cref="Owner"/> = <see cref="UnitEntryOwner.UnitOption"/> and
/// <see cref="GroupIndex"/> = -1) an option declared on the unit itself.
/// </summary>
[Table("unit_options")]
public class UnitOptionRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public UnitEntryOwner Owner { get; set; }

    public int GroupIndex { get; set; }

    public int OptionIndex { get; set; }

    public int? OptionId { get; set; }

    public string? Name { get; set; }

    public string Swc { get; set; } = "-";

    public string Cost { get; set; } = "-";

    public int Minis { get; set; }

    public bool Disabled { get; set; }
}
