using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>Makes the selection screen for a kind of company, new or recruiting, for whichever app shows it.</summary>
public sealed class CompanySelectionScreens(CompanySelectionServices services)
{
    /// <summary>The selection screen for a kind of company.</summary>
    public CompanySelectionViewModel For(CompanyKind kind, ArmySourceSelectionMode mode) => new(services, RulesFor(kind, mode));

    /// <summary>The company's kind of selection screen, prepared to recruit; null for a company type with none.</summary>
    public CompanySelectionViewModel? ForRecruitment(CompanyRecruitment recruitment)
    {
        if (RecruitableCompany.For(recruitment.Company.CompanyType) is not { } company)
        {
            return null;
        }

        var viewModel = For(company.Kind, company.Mode);
        viewModel.PrepareRecruitment(recruitment);
        return viewModel;
    }

    /// <summary>The rules for a kind of company; Inspiring and Airborne companies always use sectorials.</summary>
    public static CompanyRules RulesFor(CompanyKind kind, ArmySourceSelectionMode mode) => kind switch
    {
        CompanyKind.Standard => new StandardCompanyRules(mode),
        CompanyKind.Tag => new TagCompanyRules(mode),
        CompanyKind.ProxyPack => new ProxyPackCompanyRules(mode),
        CompanyKind.Cohesive => new CohesiveCompanyRules(mode),
        CompanyKind.Inspiring => new InspiringCompanyRules(),
        CompanyKind.Airborne => new AirborneCompanyRules(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
