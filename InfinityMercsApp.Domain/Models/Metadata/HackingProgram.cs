namespace InfinityMercsApp.Domain.Models.Metadata;

public class HackingProgram
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Opponent { get; set; }

    public string? Special { get; set; }

    public string? Damage { get; set; }

    public string? Attack { get; set; }

    public string? Burst { get; set; }

    public int? Extra { get; set; }

    public IReadOnlyList<string> SkillTypes { get; set; } = [];

    /// <summary>Ids of the hacking devices that carry the program.</summary>
    public IReadOnlyList<int> Devices { get; set; } = [];

    public IReadOnlyList<string> Targets { get; set; } = [];
}
