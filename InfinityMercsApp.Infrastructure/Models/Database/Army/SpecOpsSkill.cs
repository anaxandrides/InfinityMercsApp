namespace InfinityMercsApp.Infrastructure.Models.Database.Army;

using SQLite;

[Table("specops_skills")]
public class SpecopsSkill
{
    [PrimaryKey]
    public string SpecopsSkillKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int SkillId { get; set; }

    public int Exp { get; set; }

}