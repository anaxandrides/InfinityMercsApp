using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using AirborneGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.AirborneCompanyFactionGenerator;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>An Airborne company: the captain jumps in with Parachutist and Network Support (Controlled Jump).</summary>
public sealed class AirborneCompanySelectionViewModel(CompanySelectionServices services) : GeneratedFactionCompanySelectionViewModel(services)
{
    public const string ParachutistSkill = "Parachutist";
    public const string NetworkSupportSkill = "Network Support (Controlled Jump)";
    public const string LogoPath = "SVGCache/MercsIcons/noun-airborne-8005870.svg";

    public override string CompanyTypeLabel => "Airborne Company";

    protected override int CompanyFactionId => AirborneGen.AirborneCompanyFactionId;

    protected override string CompanyFactionName => "Airborne Company";

    protected override string CompanyLogoPath => LogoPath;

    /// <summary>The captain (from the left slot) gains the airborne skills it doesn't already have.</summary>
    protected override IReadOnlyCollection<string> CommonSkillsFor(
        ArmyUnitSelectionItem unit, ViewerProfileItem profile, IReadOnlyList<string> commonSkills)
    {
        if (!IsLeftSlotUnit(unit))
        {
            return commonSkills.ToList();
        }

        var existing = new HashSet<string>(commonSkills, StringComparer.OrdinalIgnoreCase);
        existing.UnionWith(CompanyProfileText.SplitDisplayLine(profile.UniqueSkills));
        var skills = commonSkills.ToList();
        skills.AddRange(new List<string> { ParachutistSkill, NetworkSupportSkill }.Where(skill => !existing.Contains(skill)));
        return skills;
    }
}
