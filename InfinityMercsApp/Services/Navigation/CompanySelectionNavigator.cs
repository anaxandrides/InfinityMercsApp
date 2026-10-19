using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using InfinityMercsApp.Views.Company.Selection;

namespace InfinityMercsApp.Services.Navigation;

/// <summary>Opens the company selection screen with the ViewModel for the kind of company being built.</summary>
internal sealed class CompanySelectionNavigator(CompanySelectionServices services) : ICompanySelectionNavigator
{
    public Task OpenAsync(CompanyKind kind, ArmySourceSelectionMode mode = ArmySourceSelectionMode.Sectorials)
    {
        var navigation = Shell.Current?.Navigation
            ?? throw new InvalidOperationException("No active Shell is available to open the company selection.");
        return navigation.PushAsync(new CompanySelectionPage(ViewModelFor(kind, mode)));
    }

    public async Task<bool> OpenRecruitmentAsync(CompanyRecruitment recruitment)
    {
        if (RecruitmentViewModelFor(recruitment) is not { } viewModel)
        {
            return false;
        }

        var navigation = Shell.Current?.Navigation
            ?? throw new InvalidOperationException("No active Shell is available to open recruitment.");
        await navigation.PushAsync(new CompanySelectionPage(viewModel));
        return true;
    }

    /// <summary>The company's kind of selection screen, prepared to recruit; null for a company type with none.</summary>
    internal CompanySelectionViewModel? RecruitmentViewModelFor(CompanyRecruitment recruitment)
    {
        if (RecruitableCompany.For(recruitment.Company.CompanyType) is not { } company)
        {
            return null;
        }

        var viewModel = ViewModelFor(company.Kind, company.Mode);
        viewModel.PrepareRecruitment(recruitment);
        return viewModel;
    }

    /// <summary>The selection screen for a kind of company.</summary>
    internal CompanySelectionViewModel ViewModelFor(CompanyKind kind, ArmySourceSelectionMode mode) => new(services, RulesFor(kind, mode));

    /// <summary>The rules for a kind of company; Inspiring and Airborne companies always use sectorials.</summary>
    internal static CompanyRules RulesFor(CompanyKind kind, ArmySourceSelectionMode mode) => kind switch
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
