namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("army_relations")]
public class ArmyRelationRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int RelationIndex { get; set; }

    public int? Min { get; set; }

    public int? Max { get; set; }

    public bool? IsGroup { get; set; }
}
