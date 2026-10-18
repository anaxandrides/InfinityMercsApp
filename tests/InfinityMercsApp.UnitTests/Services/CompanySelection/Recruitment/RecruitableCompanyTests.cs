using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.Navigation;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Recruitment;

public sealed class RecruitableCompanyTests
{
    /// <summary>Each company type the selection screens save, and the screen it recruits through.</summary>
    public static TheoryData<string, CompanyKind, ArmySourceSelectionMode> CompanyTypes => new()
    {
        { "Standard Company - Vanilla", CompanyKind.Standard, ArmySourceSelectionMode.VanillaFactions },
        { "Standard Company - Sectorial", CompanyKind.Standard, ArmySourceSelectionMode.Sectorials },
        { "TAG Company", CompanyKind.Tag, ArmySourceSelectionMode.Sectorials },
        { "Lone Wolf Company", CompanyKind.LoneWolf, ArmySourceSelectionMode.VanillaFactions },
        { "Cohesive Company", CompanyKind.Cohesive, ArmySourceSelectionMode.Sectorials },
        { "Inspiring Leader", CompanyKind.Inspiring, ArmySourceSelectionMode.Sectorials },
        { " Airborne Company ", CompanyKind.Airborne, ArmySourceSelectionMode.Sectorials }
    };

    [Theory]
    [MemberData(nameof(CompanyTypes))]
    public void For_EachSavedType_RecruitsThroughItsOwnScreen(string companyType, CompanyKind kind, ArmySourceSelectionMode mode) =>
        Assert.Equal(new RecruitableCompany(kind, mode), RecruitableCompany.For(companyType));

    [Theory]
    [InlineData("Standard Company")]
    [InlineData("")]
    [InlineData(null)]
    public void For_UnknownType_ReturnsNull(string? companyType) => Assert.Null(RecruitableCompany.For(companyType));
}
