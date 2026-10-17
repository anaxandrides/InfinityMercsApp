using InfinityMercsApp.Domain.Season.RecruitRolls;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Rolls;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Rolls;

public sealed class RecruitRollPromptTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly IDice _dice = Substitute.For<IDice>();
    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();
    private readonly RecruitRollPrompt _prompt;

    public RecruitRollPromptTests()
    {
        _armyData.GetRecruitTable(RecruitRollTable.Booty).Returns([new RollRange(1, 10, "DA CC Weapon"), new RollRange(11, 20, "HMG")]);
        _dialogs.ChooseAsync(Arg.Is<string>(title => title.Contains("roll on")), "Later", Arg.Any<IReadOnlyList<string>>()).Returns(RecruitRollPrompt.RollChoice);
        _dice.RollD20().Returns(4, 15);
        _prompt = new RecruitRollPrompt(_dialogs, _dice, _armyData);
    }

    private Task<RecruitRoll?> RollAsync(bool canReroll) => _prompt.RollAsync("Ghulam", new OwedRoll(RecruitRollTable.Booty, canReroll), "LI", "Later");

    [Fact]
    public async Task ARollKeepsItsResult()
    {
        Assert.Equal(new RecruitRoll(RecruitRollTable.Booty, 4, "DA CC Weapon"), await RollAsync(canReroll: false));
        await _dialogs.Received(1).ShowAlertAsync("Booty rolled 4", "Ghulam gains DA CC Weapon for good.");
    }

    [Fact]
    public async Task ARerollKeepsTheSecondResult()
    {
        _dialogs.ChooseAsync(Arg.Is<string>(title => title.Contains("Keep it")), RecruitRollPrompt.KeepChoice, Arg.Any<IReadOnlyList<string>>())
            .Returns(RecruitRollPrompt.RerollChoice);

        Assert.Equal(new RecruitRoll(RecruitRollTable.Booty, 15, "HMG"), await RollAsync(canReroll: true));
    }

    [Fact]
    public async Task KeepingTheFirstRollSkipsTheReroll()
    {
        _dialogs.ChooseAsync(Arg.Is<string>(title => title.Contains("Keep it")), RecruitRollPrompt.KeepChoice, Arg.Any<IReadOnlyList<string>>())
            .Returns((string?)null);

        Assert.Equal(4, (await RollAsync(canReroll: true))?.Roll);
        _dice.Received(1).RollD20();
    }

    [Fact]
    public async Task ARealRollIsEntered()
    {
        _dialogs.ChooseAsync(Arg.Is<string>(title => title.Contains("roll on")), "Later", Arg.Any<IReadOnlyList<string>>()).Returns(RecruitRollPrompt.RealChoice);
        _dialogs.AskNumberAsync(Arg.Any<string>(), Arg.Any<string>(), 1, 20).Returns(18);

        Assert.Equal(new RecruitRoll(RecruitRollTable.Booty, 18, "HMG"), await RollAsync(canReroll: false));
        _dice.DidNotReceive().RollD20();
    }

    [Fact]
    public async Task WithoutTheTableNothingIsRolled()
    {
        Assert.Null(await _prompt.RollAsync("Ghulam", new OwedRoll(RecruitRollTable.Metachemistry, false), "LI", "Later"));
        await _dialogs.Received(1).ShowAlertAsync("No Metachemistry Table", Arg.Any<string>());
    }
}
