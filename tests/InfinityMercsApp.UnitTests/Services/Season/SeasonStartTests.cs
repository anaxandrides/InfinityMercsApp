using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class SeasonStartTests
{
    private static SavedCompanyFile Company(int startingCredits, int spent, params List<SavedCompanyEntry> entries) =>
        new() { StartSeasonPoints = startingCredits, CurrentPoints = spent, Entries = entries };

    private static SavedCompanyEntry Captain(string swc) =>
        new() { EntryIndex = 0, IsLieutenant = true, ProfileKey = $"Joan|JOAN OF ARC|47|{swc}|lt:1" };

    [Fact]
    public void TheCreditsLeftFromRecruitingCarryOver()
    {
        Assert.Equal(33, SeasonStart.FundsFor(Company(90, 57, Captain("0"))).Credits);
    }

    [Fact]
    public void OverspentRecruitingLeavesNothing()
    {
        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 80, Captain("0"))).Credits);
    }

    [Fact]
    public void ACaptainWhoGivesSwcStartsTheCompanysSwc()
    {
        Assert.Equal(1, SeasonStart.FundsFor(Company(75, 75, Captain("+1"))).Swc);
    }

    [Fact]
    public void OtherwiseTheCompanyStartsWithNoSwc()
    {
        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 75, Captain("0"))).Swc);
        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 75)).Swc);
    }

    [Fact]
    public void OnlyTheCaptainsSwcCounts()
    {
        var trooper = new SavedCompanyEntry { EntryIndex = 1, ProfileKey = "Harper|DR. HARPER FTO|32|+1|lt:0" };

        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 75, Captain("0"), trooper)).Swc);
    }

    [Fact]
    public void AnOddProfileKeyGivesNoSwc()
    {
        var captain = new SavedCompanyEntry { IsLieutenant = true, ProfileKey = "Joan" };

        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 75, captain)).Swc);
    }
}
