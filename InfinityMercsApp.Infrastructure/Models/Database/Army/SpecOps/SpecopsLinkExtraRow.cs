namespace InfinityMercsApp.Infrastructure.Models.Database.Army.SpecOps;

using SQLite;

/// <summary>An extra of a linked spec-ops item (e.g. the extra of a skill granted with equipment).</summary>
[Table("specops_link_extras")]
public class SpecopsLinkExtraRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    [Indexed]
    public string OwnerKey { get; set; } = string.Empty;

    public string LinkKind { get; set; } = string.Empty;

    public int LinkPosition { get; set; }

    public int Position { get; set; }

    public int ExtraId { get; set; }
}
