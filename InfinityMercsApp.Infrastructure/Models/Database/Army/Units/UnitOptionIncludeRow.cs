namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Units;

using SQLite;

/// <summary>An option that also grants another option's entries.</summary>
[Table("unit_option_includes")]
public class UnitOptionIncludeRow
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

    public int IncludedGroupId { get; set; }

    public int IncludedOptionId { get; set; }
}
