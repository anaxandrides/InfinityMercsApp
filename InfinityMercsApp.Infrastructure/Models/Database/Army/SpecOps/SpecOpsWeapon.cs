namespace InfinityMercsApp.Infrastructure.Models.Database.Army.SpecOps;

using SQLite;

[Table("specops_weapons")]
public class SpecopsWeapon
{
    [PrimaryKey]
    public string SpecopsWeaponKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int WeaponId { get; set; }

    public int Exp { get; set; }

}