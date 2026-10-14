namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("unit_profiles")]
public class UnitProfileRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public int GroupIndex { get; set; }

    public int ProfileIndex { get; set; }

    public int? ProfileId { get; set; }

    public string? Name { get; set; }

    public string? Logo { get; set; }

    public int? Type { get; set; }

    public bool Str { get; set; }

    public int? StrValue { get; set; }

    public string Move { get; set; } = "-";

    public int? Cc { get; set; }

    public int? Bs { get; set; }

    public int? Ph { get; set; }

    public int? Wip { get; set; }

    public int? Arm { get; set; }

    public int? Bts { get; set; }

    public int? W { get; set; }

    public int? S { get; set; }

    public int? Ava { get; set; }

    public int? Vita { get; set; }

    public bool HasStatFields { get; set; }
}
