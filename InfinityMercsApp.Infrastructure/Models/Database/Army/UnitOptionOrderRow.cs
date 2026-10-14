namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("unit_option_orders")]
public class UnitOptionOrderRow
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

    public int Position { get; set; }

    public string? Type { get; set; }

    public int? List { get; set; }

    public int? Total { get; set; }
}
