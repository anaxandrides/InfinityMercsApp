using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Domain.Captain;

/// <summary>
/// Checks a captain build against the rules: the experience spent fits the budget, every stat upgrade is a tier
/// the rules offer that stat (so no stat goes past its cap), and every weapon, skill and equipment choice is on
/// offer and chosen only once.
/// </summary>
public static class CaptainBuildValidation
{
    /// <summary>Everything wrong with the build; none when it can be confirmed.</summary>
    public static IReadOnlyList<CaptainBuildProblem> Problems(CaptainBuild build)
    {
        List<CaptainBuildProblem> problems = [];
        if (build.ExperienceRemaining < 0)
        {
            problems.Add(new(CaptainBuildProblemKind.Overspent, $"{-build.ExperienceRemaining} XP over budget."));
        }

        foreach (var stat in build.Stats.Where(stat => stat.Tier != 0 && stat.Option is null))
        {
            problems.Add(new(CaptainBuildProblemKind.StatTierNotOffered, $"{stat.Stat.Key()} can't take upgrade tier {stat.Tier}."));
        }

        foreach (var group in build.Choices)
        {
            foreach (var label in group.Filled.Where(label => !group.Offered.Contains(label.Trim(), StringComparer.OrdinalIgnoreCase)))
            {
                problems.Add(new(CaptainBuildProblemKind.ChoiceNotOffered, $"{CaptainChoiceLabel.Name(label)} isn't offered in {group.Group}."));
            }

            var repeated = group.Filled
                .Select(CaptainChoiceLabel.Name)
                .GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
                .Where(names => names.Count() > 1)
                .Select(names => names.Key);
            foreach (var name in repeated)
            {
                problems.Add(new(CaptainBuildProblemKind.DuplicateChoice, $"{name} is chosen more than once in {group.Group}."));
            }
        }

        return problems;
    }
}
