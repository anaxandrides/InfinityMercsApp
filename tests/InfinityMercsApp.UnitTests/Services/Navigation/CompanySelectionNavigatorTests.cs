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
    [InlineData(CompanyKind.LoneWolf, typeof(LoneWolfCompanySelectionViewModel))]
    [InlineData(CompanyKind.Cohesive, typeof(CohesiveCompanySelectionViewModel))]
    [InlineData(CompanyKind.Inspiring, typeof(InspiringCompanySelectionViewModel))]
    [InlineData(CompanyKind.Airborne, typeof(AirborneCompanySelectionViewModel))]
    public void EachKindGetsItsOwnViewModel(CompanyKind kind, Type expected) =>
        Assert.IsType(expected, _navigator.ViewModelFor(kind, ArmySourceSelectionMode.Sectorials));

    [Fact]
    public void TheSourceModeIsPassedOn() =>
        Assert.Equal(
            ArmySourceSelectionMode.VanillaFactions,
            _navigator.ViewModelFor(CompanyKind.Standard, ArmySourceSelectionMode.VanillaFactions).Mode);

    [Fact]
    public void GeneratedCompaniesAlwaysUseSectorials() =>
        Assert.Equal(
            ArmySourceSelectionMode.Sectorials,
            _navigator.ViewModelFor(CompanyKind.Airborne, ArmySourceSelectionMode.VanillaFactions).Mode);
}
