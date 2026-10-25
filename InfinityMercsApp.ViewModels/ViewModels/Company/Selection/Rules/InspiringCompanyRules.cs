using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.InspiringCompanyFactionGenerator;

namespace InfinityMercsApp.ViewModels.Company.Selection.Rules;

/// <summary>An Inspiring Leader company: the lieutenant gains Inspiring Leadership.</summary>
public sealed class InspiringCompanyRules() : GeneratedFactionCompanyRules(InspiringGen.InspiringCompanyFactionId, "Inspiring Company", LogoPath)
{
    public const string InspiringLeadershipSkill = "Inspiring Leadership";
    public const string LogoPath = "SVGCache/MercsIcons/noun-leadership-7195245.svg";

    public override string CompanyTypeLabel => CompanyTypes.InspiringLeader;

    public override IReadOnlyCollection<string> CommonSkillsFor(
        ArmyUnitSelectionItem unit, ViewerProfileItem profile, IReadOnlyList<string> commonSkills)
    {
        if (!profile.IsLieutenant || commonSkills.Any(skill => string.Equals(skill?.Trim(), InspiringLeadershipSkill, StringComparison.OrdinalIgnoreCase)))
        {
            return commonSkills.ToList();
        }

        var skills = commonSkills.Where(skill => !string.IsNullOrWhiteSpace(skill)).Select(skill => skill.Trim()).ToList();
        skills.Add(InspiringLeadershipSkill);
        return skills;
    }
}
