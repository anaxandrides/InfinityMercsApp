namespace InfinityMercsApp.Domain.Models.Army;

public class SpecopsSkill
{
    public string SpecopsSkillKey { get; set; } = string.Empty;

    public int FactionId { get; set; }

    public int EntryOrder { get; set; }

    public int SkillId { get; set; }

    public int Exp { get; set; }

    public IReadOnlyList<int> Extras { get; set; } = [];

    /// <summary>Skills the choice grants alongside it.</summary>
    public IReadOnlyList<SpecopsLink> Skills { get; set; } = [];

    /// <summary>Equipment the choice grants alongside it.</summary>
    public IReadOnlyList<SpecopsLink> Equip { get; set; } = [];

    /// <summary>Weapons the choice grants alongside it.</summary>
    public IReadOnlyList<SpecopsLink> Weapons { get; set; } = [];
}