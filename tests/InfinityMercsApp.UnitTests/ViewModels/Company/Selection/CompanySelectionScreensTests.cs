using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class CompanySelectionScreensTests
{
    private readonly CompanySelectionScreens _screens = new(new SelectionTestKit().Services);

    [Theory]
    [InlineData(CompanyKind.Standard, typeof(StandardCompanyRules))]
    [InlineData(CompanyKind.Tag, typeof(TagCompanyRules))]
    [InlineData(CompanyKind.ProxyPack, typeof(ProxyPackCompanyRules))]
    [InlineData(CompanyKind.Cohesive, typeof(CohesiveCompanyRules))]
    [InlineData(CompanyKind.Inspiring, typeof(InspiringCompanyRules))]
    [InlineData(CompanyKind.Airborne, typeof(AirborneCompanyRules))]
    public void For_EachKind_UsesItsOwnRules(CompanyKind kind, Type expected) =>
        Assert.IsType(expected, _screens.For(kind, ArmySourceSelectionMode.Sectorials).Rules);

    [Fact]
    public void For_StandardCompany_PassesTheSourceModeOn() =>
        Assert.Equal(
            ArmySourceSelectionMode.VanillaFactions,
            _screens.For(CompanyKind.Standard, ArmySourceSelectionMode.VanillaFactions).Mode);

    [Fact]
    public void For_GeneratedCompany_AlwaysUsesSectorials() =>
        Assert.Equal(
            ArmySourceSelectionMode.Sectorials,
            _screens.For(CompanyKind.Airborne, ArmySourceSelectionMode.VanillaFactions).Mode);
}
