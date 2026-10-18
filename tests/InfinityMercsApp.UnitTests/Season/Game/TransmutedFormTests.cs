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
        var form = BattleRavaged.Carrying(new CarriedGear(["Forward Observer", "courage"], ["Medikit"], []));

        Assert.Equal(["Courage", "Forward Observer"], form.Skills);
        Assert.Equal(["Multispectral Visor (L1)", "Medikit"], form.Equipment);
    }

    [Fact]
    public void Carrying_StatAugments_SetTheirStatButNotVita()
    {
        var form = BattleRavaged.Carrying(new CarriedGear([], [], [new StatOverride(Stat.Bs, 14), new StatOverride(Stat.Vita, 3)]));

        Assert.Equal([new(Stat.Bs, "14"), new(Stat.Arm, "2"), new(Stat.Vita, "2")], form.Statline);
    }

    [Fact]
    public void Carrying_Nothing_LeavesTheFormAsPrinted() =>
        Assert.Equal(BattleRavaged.Statline, BattleRavaged.Carrying(CarriedGear.None).Statline);
}
