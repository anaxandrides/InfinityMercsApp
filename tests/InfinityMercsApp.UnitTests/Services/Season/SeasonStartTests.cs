using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class SeasonStartTests
{
    private static SavedCompanyFile Company(int startingCredits, int spent, params List<SavedCompanyEntry> entries) =>
        new() { StartSeasonPoints = startingCredits, CurrentPoints = spent, Entries = entries };

    private static SavedCompanyEntry Captain(string swc) =>
        new() { EntryIndex = 0, IsLieutenant = true, CarriesCaptainUpgrades = true, ProfileKey = $"Joan|JOAN OF ARC|47|{swc}|lt:1" };

    [Fact]
    public void FundsFor_CreditsLeftFromRecruiting_CarryOver()
    {
        Assert.Equal(33, SeasonStart.FundsFor(Company(90, 57, Captain("0"))).Credits);
    }

    [Fact]
    public void FundsFor_OverspentRecruiting_LeavesNothing()
    {
        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 80, Captain("0"))).Credits);
    }

    [Fact]
    public void FundsFor_CaptainGivesSwc_StartsTheCompanysSwc()
    {
        Assert.Equal(1, SeasonStart.FundsFor(Company(75, 75, Captain("+1"))).Swc);
    }

    [Fact]
    public void FundsFor_NoSwcCaptain_StartsWithNoSwc()
    {
        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 75, Captain("0"))).Swc);
        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 75)).Swc);
    }

    [Fact]
    public void FundsFor_TrooperGivesSwc_CountsOnlyTheCaptains()
    {
        var trooper = new SavedCompanyEntry { EntryIndex = 1, ProfileKey = "Harper|DR. HARPER FTO|32|+1|lt:0" };

        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 75, Captain("0"), trooper)).Swc);
    }

    [Fact]
    public void FundsFor_OddProfileKey_GivesNoSwc()
    {
        var captain = new SavedCompanyEntry { IsLieutenant = true, CarriesCaptainUpgrades = true, ProfileKey = "Joan" };

        Assert.Equal(0, SeasonStart.FundsFor(Company(75, 75, captain)).Swc);
    }
}
