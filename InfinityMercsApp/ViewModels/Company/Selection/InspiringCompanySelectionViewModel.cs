using InfinityMercsApp.Services.CompanySelection.Models.Selection;
using InfinityMercsApp.ViewModels.UnitBrowser;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.InspiringCompanyFactionGenerator;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>An Inspiring Leader company: the lieutenant gains Inspiring Leadership.</summary>
public sealed class InspiringCompanySelectionViewModel(CompanySelectionServices services) : GeneratedFactionCompanySelectionViewModel(services)
{
    public const string InspiringLeadershipSkill = "Inspiring Leadership";
    public const string LogoPath = "SVGCache/MercsIcons/noun-leadership-7195245.svg";

    public override string CompanyTypeLabel => "Inspiring Leader";

    protected override int CompanyFactionId => InspiringGen.InspiringCompanyFactionId;

    protected override string CompanyFactionName => "Inspiring Company";

    protected override string CompanyLogoPath => LogoPath;

    protected override IReadOnlyCollection<string> CommonSkillsFor(
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
