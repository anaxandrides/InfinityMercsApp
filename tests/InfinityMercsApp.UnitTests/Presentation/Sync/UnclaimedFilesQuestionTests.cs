using InfinityMercsApp.Presentation.Sync;
using InfinityMercsApp.Services.Sync;

namespace InfinityMercsApp.UnitTests.Presentation.Sync;

public sealed class UnclaimedFilesQuestionTests
{
    [Fact]
    public void For_CompaniesAndSeasons_CountsBoth() =>
        Assert.StartsWith(
            "This device has 3 companies and 1 season that aren't in your account.",
            UnclaimedFilesQuestion.For(new UnclaimedFiles(3, 1)).Question);

    [Fact]
    public void For_OneCompany_SpeaksOfIt() =>
        Assert.Equal(
            "This device has 1 company that isn't in your account. Add it to your account, so it's kept online?",
            UnclaimedFilesQuestion.For(new UnclaimedFiles(1, 0)).Question);

    [Theory]
    [InlineData(UnclaimedFilesQuestion.AddToAccount, ClaimChoice.AddToAccount)]
    [InlineData(UnclaimedFilesQuestion.KeepOnDevice, ClaimChoice.KeepOnDevice)]
    [InlineData(UnclaimedFilesQuestion.NotNow, null)]
    public void ChoiceFor_Answer_IsItsChoice(string answer, ClaimChoice? expected) =>
        Assert.Equal(expected, UnclaimedFilesQuestion.ChoiceFor(answer));
}
