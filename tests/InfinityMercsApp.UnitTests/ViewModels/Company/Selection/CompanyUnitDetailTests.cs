using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.Presentation.Themes;
using InfinityMercsApp.Presentation.Units;
namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class CompanyUnitDetailTests
{
    private static string Move(int? first, int? second) => $"{first}/{second}";

    private static PeripheralMercsCompanyStats Peripheral() => new()
    {
        NameHeading = "Traktor Mul",
        MoveFirstCm = 15,
        MoveSecondCm = 5,
        Cc = "8",
        Arm = "3",
        Equipment = "-",
        Skills = "Peripheral (Servant)"
    };

    [Fact]
    public void ANewDetailShowsNoUnit()
    {
        var detail = new CompanyUnitDetail();

        Assert.Equal(CompanyUnitDetail.NoUnitHeading, detail.UnitNameHeading);
        Assert.Equal(UnitStatLine.Empty, detail.Stats);
        Assert.Equal(UnitHeaderIcons.None, detail.Icons);
        Assert.Null(detail.Peripheral);
        Assert.Equal(CompanyHeaderColors.Default, detail.HeaderColors);
    }

    [Fact]
    public void APeripheralGetsItsOwnStatline()
    {
        var detail = new CompanyUnitDetail();

        detail.ApplyPeripheral(Peripheral(), Move);

        var peripheral = Assert.IsType<PeripheralStatBlock>(detail.Peripheral);
        Assert.Equal("Traktor Mul", peripheral.NameHeading);
        Assert.Equal(("15/5", "8", "3"), (peripheral.Stats.Mov, peripheral.Stats.Cc, peripheral.Stats.Arm));
        Assert.False(peripheral.HasEquipment);
        Assert.True(peripheral.HasSkills);
    }

    [Fact]
    public void ResettingThePeripheralHidesIt()
    {
        var detail = new CompanyUnitDetail();
        detail.ApplyPeripheral(Peripheral(), Move);
        Assert.True(detail.HasPeripheral);

        detail.ResetPeripheral();

        Assert.Null(detail.Peripheral);
        Assert.False(detail.HasPeripheral);
    }

    // The season and viewer detail has no peripheral block, so the display must not show an empty one.
    [Fact]
    public void ASavedTroopersDetailHasNoPeripheralBlock() =>
        Assert.False(new UnitDetailViewModel(Army101Data.Create()).HasPeripheral);

    [Fact]
    public void ChangingUnitsReformatsBothMoves()
    {
        var detail = new CompanyUnitDetail();
        detail.ApplyPeripheral(Peripheral(), Move);

        detail.RefreshMoves((first, second) => $"{first}-{second} cm");

        Assert.Equal("15-5 cm", detail.Peripheral?.Stats.Mov);
        Assert.Equal("- cm", detail.Stats.Mov);
    }

    [Fact]
    public void IrregularWinsOverRegular()
    {
        var detail = new CompanyUnitDetail();

        detail.SetOrderTraits(hasRegular: true, hasIrregular: true, hasImpetuous: true, hasTacticalAwareness: false);
        detail.SetTechTraits(hasCube: true, hasCube2: false, hasHackable: true);

        Assert.Equal(new UnitHeaderIcons { Irregular = true, Impetuous = true, Cube = true, Hackable = true }, detail.Icons);
    }

    [Fact]
    public void ResettingClearsTheIconsAndStats()
    {
        var detail = new CompanyUnitDetail();
        detail.SetOrderTraits(true, false, false, true);
        detail.ApplyPeripheral(Peripheral(), Move);

        detail.Reset();

        Assert.Equal(UnitHeaderIcons.None, detail.Icons);
        Assert.Equal(UnitStatLine.Empty, detail.Stats);
        Assert.Null(detail.Peripheral);
    }
}
