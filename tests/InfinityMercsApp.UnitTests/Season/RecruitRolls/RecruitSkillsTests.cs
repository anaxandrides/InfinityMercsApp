using InfinityMercsApp.Domain.Season.RecruitRolls;

namespace InfinityMercsApp.UnitTests.Season.RecruitRolls;

public sealed class RecruitSkillsTests
{
    [Fact]
    public void ATrooperOwesARollPerTableItsSkillsName()
    {
        var owed = RecruitSkills.Owed(["Courage", "Booty (ReRoll)", "MetaChemistry"], []);

        Assert.Equal([new OwedRoll(RecruitRollTable.Booty, true), new OwedRoll(RecruitRollTable.Metachemistry, false)], owed);
    }

    [Fact]
    public void ATableRolledOnIsNotOwedAgain()
    {
        Assert.Empty(RecruitSkills.Owed(["Booty"], [new RecruitRoll(RecruitRollTable.Booty, 7, "DA CC Weapon")]));
    }

    [Theory]
    [InlineData("Booty", RecruitRollTable.Booty)]
    [InlineData("booty (Re-Roll)", RecruitRollTable.Booty)]
    [InlineData("Metachemistry", RecruitRollTable.Metachemistry)]
    [InlineData("Mimetism (-3)", null)]
    public void ASkillNamesItsTable(string skill, RecruitRollTable? table) => Assert.Equal(table, RecruitSkills.TableOf(skill));

    [Fact]
    public void ARollDescribesTheSideForTheTroopersType()
    {
        var roll = new RecruitRoll(RecruitRollTable.Booty, 20, "TAG: BS Attack (+1B) / Other Troop Types: HMG");

        Assert.Equal("Booty: HMG", roll.Describe("LI"));
        Assert.Equal("Booty: BS Attack (+1B)", roll.Describe("TAG"));
    }
}
