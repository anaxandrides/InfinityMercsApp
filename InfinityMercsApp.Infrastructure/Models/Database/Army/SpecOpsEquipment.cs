namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("specops_equipment")]
public class SpecopsEquipment
{
    [PrimaryKey]
    public string SpecopsEquipmentKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int EquipmentId { get; set; }

    public int Exp { get; set; }

}