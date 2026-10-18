using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.UnitTests.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection;

namespace InfinityMercsApp.UnitTests.Services.Navigation;

public sealed class CompanySelectionNavigatorTests
{
    private readonly CompanySelectionNavigator _navigator = new(new SelectionTestKit().Services);

    [Theory]
    [InlineData(CompanyKind.Standard, typeof(StandardCompanySelectionViewModel))]
    [InlineData(CompanyKind.Tag, typeof(TagCompanySelectionViewModel))]
    [InlineData(CompanyKind.ProxyPack, typeof(ProxyPackCompanySelectionViewModel))]
    [InlineData(CompanyKind.Cohesive, typeof(CohesiveCompanySelectionViewModel))]
    [InlineData(CompanyKind.Inspiring, typeof(InspiringCompanySelectionViewModel))]
    [InlineData(CompanyKind.Airborne, typeof(AirborneCompanySelectionViewModel))]
    public void ViewModelFor_EachKind_ReturnsItsOwnViewModel(CompanyKind kind, Type expected) =>
        Assert.IsType(expected, _navigator.ViewModelFor(kind, ArmySourceSelectionMode.Sectorials));

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
