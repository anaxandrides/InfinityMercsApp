namespace InfinityMercsApp.Domain.Season.RecruitRolls;

/// <summary>
/// Booty and Metachemistry are obligatory recruit skills: a trooper with one rolls once on its table when first
/// bought and keeps the result for good. The (Reroll) form allows one more roll, whose result is kept instead.
/// </summary>
public static class RecruitSkills
{
    /// <summary>
    /// The rolls a trooper with <paramref name="skills"/> still owes, one per table its skills name that it has
    /// not rolled on.
    /// </summary>
    public static IReadOnlyList<OwedRoll> Owed(IEnumerable<string> skills, IEnumerable<RecruitRoll> rolled)
    {
        var done = rolled.Select(roll => roll.Table).ToHashSet();
        var owed = new List<OwedRoll>();
        foreach (var skill in skills)
        {
            if (TableOf(skill) is { } table && !done.Contains(table) && owed.All(roll => roll.Table != table))
            {
                owed.Add(new OwedRoll(table, CanReroll(skill)));
            }
        }

        return owed;
    }

    /// <summary>The table a skill rolls on: "Booty" and "Booty (ReRoll)" both roll on Booty.</summary>
    public static RecruitRollTable? TableOf(string skill)
    {
        var bracket = skill.IndexOf('(', StringComparison.Ordinal);
        var name = (bracket < 0 ? skill : skill[..bracket]).Trim();
        return Enum.GetValues<RecruitRollTable>()
            .Where(table => string.Equals(table.ToString(), name, StringComparison.OrdinalIgnoreCase))
            .Select(table => (RecruitRollTable?)table)
            .FirstOrDefault();
    }

    private static bool CanReroll(string skill) =>
        skill.Replace("-", string.Empty, StringComparison.Ordinal).Contains("reroll", StringComparison.OrdinalIgnoreCase);
}
