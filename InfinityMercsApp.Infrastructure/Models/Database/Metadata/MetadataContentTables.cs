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

/// <summary>
/// One value of a hacking program's lists: <see cref="ListKind"/> is <c>skillType</c> or <c>target</c>
/// (text in <see cref="Text"/>) or <c>devices</c> (an id in <see cref="Number"/>).
/// </summary>
[Table("hacking_program_values")]
public class HackingProgramValueRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int ProgramId { get; set; }

    public string ListKind { get; set; } = string.Empty;

    public int Position { get; set; }

    public string? Text { get; set; }

    public int? Number { get; set; }
}
