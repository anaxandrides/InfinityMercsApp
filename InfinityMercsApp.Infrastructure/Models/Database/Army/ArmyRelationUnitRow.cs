namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("army_relation_units")]
public class ArmyRelationUnitRow
{
    [PrimaryKey, AutoIncrement]
    public int RowId { get; set; }

    [Indexed]
    public int FactionId { get; set; }

    public int RelationIndex { get; set; }

    public int Position { get; set; }

    public int UnitId { get; set; }
}
