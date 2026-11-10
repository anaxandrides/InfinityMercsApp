using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Domain.SpecOps;

/// <summary>Tells Spec-Ops and Team-Ops units apart by the skill their profiles carry.</summary>
public static class SpecialOperatives
{
    /// <summary>
    /// <see cref="SpecialOperativeKind.TeamOps"/> when a profile has Infinity Team-Ops, else
    /// <see cref="SpecialOperativeKind.SpecOps"/> when one has Infinity Spec-Ops, else none.
    /// </summary>
    public static SpecialOperativeKind KindOf(UnitProfileGroups groups, IReadOnlyDictionary<int, string> skillNames)
    {
        var skills = groups.Groups
            .SelectMany(group => group.Profiles)
            .SelectMany(profile => profile.Skills)
            .Select(skill => skill.Id is int id && skillNames.TryGetValue(id, out var name) ? name.Trim() : string.Empty)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return skills.Contains(SpecUpgradeRules.TeamOpsSkill) ? SpecialOperativeKind.TeamOps
            : skills.Contains(SpecUpgradeRules.SpecOpsSkill) ? SpecialOperativeKind.SpecOps
            : SpecialOperativeKind.None;
    }
}
