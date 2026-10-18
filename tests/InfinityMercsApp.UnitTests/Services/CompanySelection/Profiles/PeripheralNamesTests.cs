using InfinityMercsApp.Services.CompanySelection.Profiles;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Profiles;

public sealed class PeripheralNamesTests
{
    [Theory]
    [InlineData("Peripheral: Traktor Mul (2), Bit", "Traktor Mul")]
    [InlineData("Q-Drone (Synchronized) (1)", "Q-Drone")]
    [InlineData("-", "")]
    [InlineData(null, "")]
    public void FirstOf_PeripheralsText_ReturnsTheFirstBaseName(string? text, string expected) =>
        Assert.Equal(expected, PeripheralNames.FirstOf(text));

    [Fact]
    public void TotalCount_CountedAndPlainNames_AddsCountsAndOnes() =>
        Assert.Equal(2 + 1 + 0, PeripheralNames.TotalCount(["Traktor Mul (2)", "Bit", "-", " "]));

    [Fact]
    public void SingleCounted_OneCountedPeripheral_ReturnsItsNameAndCount() =>
        Assert.Equal(new CountedPeripheral("Traktor Mul", 3), PeripheralNames.SingleCounted(["Traktor Mul (3)"]));

    [Theory]
    [InlineData("Traktor Mul")]
    [InlineData("Traktor Mul (0)")]
    [InlineData("-")]
    public void SingleCounted_NoUsableCount_ReturnsNull(string name) =>
        Assert.Null(PeripheralNames.SingleCounted([name]));

    [Fact]
    public void SingleCounted_SeveralPeripherals_ReturnsNull() =>
        Assert.Null(PeripheralNames.SingleCounted(["Bit (1)", "Traktor Mul (2)"]));

    [Fact]
    public void DedupeKey_PrefixAndBrackets_DropsThem() =>
        Assert.Equal("Traktor Mul", PeripheralNames.DedupeKey("Peripheral: Traktor Mul (Control) (2)"));
}
