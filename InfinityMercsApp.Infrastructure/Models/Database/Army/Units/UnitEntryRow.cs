namespace InfinityMercsApp.Infrastructure.Models.Database.Army.Units;

using SQLite;

/// <summary>
/// One characteristic, skill, piece of equipment, weapon, peripheral or ammunition reference of a profile
/// or option. <see cref="Kind"/> holds the domain <c>ProfileEntryKind</c>.
/// </summary>
[Table("unit_entries")]
public class UnitEntryRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public UnitEntryOwner Owner { get; set; }

    public int GroupIndex { get; set; }

    /// <summary>The profile index or option index, depending on <see cref="Owner"/>.</summary>
    public int OwnerIndex { get; set; }

    public int Kind { get; set; }

    public int Position { get; set; }

    public int? EntryId { get; set; }

    public int Quantity { get; set; } = 1;

    /// <summary>The API display order (<c>order</c>); null when none was sent.</summary>
    public int? DisplayOrder { get; set; }
}
