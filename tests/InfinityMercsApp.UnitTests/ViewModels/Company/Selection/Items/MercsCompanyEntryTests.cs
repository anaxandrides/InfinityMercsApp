using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection.Items;

public sealed class MercsCompanyEntryTests
{
    [Fact]
    public void RefreshMoves_InchesChosen_ReformatsTheTrooperAndItsPeripheral()
    {
        var entry = new MercsCompanyEntry
        {
            Subtitle = "MOV 10-5 | CC 14",
            UnitMoveFirstCm = 10,
            UnitMoveSecondCm = 5,
            HasPeripheralStatBlock = true,
            PeripheralMoveFirstCm = 15,
            PeripheralMoveSecondCm = 10
        };

        entry.RefreshMoves(inInches: true);

        var move = new Movement(10, 5).Format(inInches: true);
        Assert.Equal(move, entry.UnitMoveDisplay);
        Assert.Equal($"MOV {move} | CC 14", entry.Subtitle);
        Assert.Equal(new Movement(15, 10).Format(inInches: true), entry.PeripheralMov);
    }
}
