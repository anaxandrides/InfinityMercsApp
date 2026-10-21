using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;

namespace InfinityMercsApp.UnitTests.Services.Navigation;

public sealed class CompanySelectionNavigatorTests
{
    private readonly CompanySelectionNavigator _navigator = new(new SelectionTestKit().Services);

    [Theory]
    [InlineData(CompanyKind.Standard, typeof(StandardCompanyRules))]
    [InlineData(CompanyKind.Tag, typeof(TagCompanyRules))]
    [InlineData(CompanyKind.ProxyPack, typeof(ProxyPackCompanyRules))]
    [InlineData(CompanyKind.Cohesive, typeof(CohesiveCompanyRules))]
    [InlineData(CompanyKind.Inspiring, typeof(InspiringCompanyRules))]
    [InlineData(CompanyKind.Airborne, typeof(AirborneCompanyRules))]
    public void ViewModelFor_EachKind_UsesItsOwnRules(CompanyKind kind, Type expected) =>
        Assert.IsType(expected, _navigator.ViewModelFor(kind, ArmySourceSelectionMode.Sectorials).Rules);

    [Fact]
    public void ViewModelFor_StandardCompany_PassesTheSourceModeOn() =>
        Assert.Equal(
            ArmySourceSelectionMode.VanillaFactions,
            _navigator.ViewModelFor(CompanyKind.Standard, ArmySourceSelectionMode.VanillaFactions).Mode);

    [Fact]
    public void ViewModelFor_GeneratedCompany_AlwaysUsesSectorials() =>
        Assert.Equal(
            ArmySourceSelectionMode.Sectorials,
            _navigator.ViewModelFor(CompanyKind.Airborne, ArmySourceSelectionMode.VanillaFactions).Mode);
}
