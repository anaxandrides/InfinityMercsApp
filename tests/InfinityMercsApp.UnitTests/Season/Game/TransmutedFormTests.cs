using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Season.Game;

namespace InfinityMercsApp.UnitTests.Season.Game;

public sealed class TransmutedFormTests
{
    private static readonly TransmutedForm BattleRavaged = new(
        "Battle-Ravaged",
        Threshold: 1,
        [new(Stat.Bs, "11"), new(Stat.Arm, "2"), new(Stat.Vita, "2")],
        ["Courage"],
        ["Multispectral Visor (L1)"]);

    [Fact]
    public void Carrying_Gear_AddsItsSkillsAndEquipment()
    {
        var form = BattleRavaged.Carrying(new CarriedGear(["Forward Observer", "courage"], ["Medikit"]));

        Assert.Equal(["Courage", "Forward Observer"], form.Skills);
        Assert.Equal(["Multispectral Visor (L1)", "Medikit"], form.Equipment);
    }

    [Fact]
    public void Carrying_Gear_LeavesTheStatlineAsPrinted() =>
        Assert.Equal(BattleRavaged.Statline, BattleRavaged.Carrying(new CarriedGear(["Forward Observer"], ["Medikit"])).Statline);
}
