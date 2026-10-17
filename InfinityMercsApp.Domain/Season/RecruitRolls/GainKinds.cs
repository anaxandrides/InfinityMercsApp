namespace InfinityMercsApp.Domain.Season.RecruitRolls;

/// <summary>
/// Tells a named result apart by the army data's names: a weapon by its name, a skill by its name before any
/// brackets ("Mimetism (-6)" is Mimetism), and anything else is equipment.
/// </summary>
public sealed class GainKinds(IEnumerable<string> weapons, IEnumerable<string> skills)
{
    private readonly HashSet<string> _weapons = new(weapons.Select(name => name.Trim()), StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _skills = new(skills.Select(name => name.Trim()), StringComparer.OrdinalIgnoreCase);

    public static GainKinds None { get; } = new([], []);

    public GainKind Of(string name)
    {
        if (_weapons.Contains(name.Trim()))
        {
            return GainKind.Weapon;
        }

        var bracket = name.IndexOf('(', StringComparison.Ordinal);
        return _skills.Contains((bracket < 0 ? name : name[..bracket]).Trim()) ? GainKind.Skill : GainKind.Equipment;
    }
}
