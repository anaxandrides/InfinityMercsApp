using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Support;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class CaptainChangeTests
{
    private const int LieutenantSkillId = 119;

    private readonly CaptainChange _change = new(Army101Data.Create());

    private static SavedCompanyFile Company() => new()
    {
        Entries =
        [
            new SavedCompanyEntry
            {
                EntryIndex = 0, Name = "Captain", IsLieutenant = true, CarriesCaptainUpgrades = true, SourceFactionId = Army101Data.PanOceania,
                CurrentSkillCodes = [new CompanySavedCodeRef { Id = LieutenantSkillId }]
            },
            new SavedCompanyEntry { EntryIndex = 1, Name = "Fusilier", SourceFactionId = Army101Data.PanOceania },
            new SavedCompanyEntry { EntryIndex = 2, Name = "Drone", IsPeripheralUnit = true, SourceFactionId = Army101Data.PanOceania }
        ]
    };

    [Fact]
    public void Apply_Trooper_PassesTheRoleAndLieutenantSkill()
    {
        var company = Company();

        Assert.True(_change.Apply(company, 1, season: null));

        Assert.True(company.Entries[1].IsLieutenant);
        Assert.Equal([LieutenantSkillId], company.Entries[1].CurrentSkillCodes.Select(code => code.Id));
        Assert.False(company.Entries[0].IsLieutenant);
        Assert.Empty(company.Entries[0].CurrentSkillCodes);
        Assert.True(company.Entries[0].CarriesCaptainUpgrades);
        Assert.False(company.Entries[1].CarriesCaptainUpgrades);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(9)]
    public void Apply_CaptainPeripheralOrMissing_ChangesNothing(int entryIndex)
    {
        var company = Company();

        Assert.False(_change.Apply(company, entryIndex, season: null));

        Assert.True(company.Entries[0].IsLieutenant);
    }

    [Fact]
    public void CandidatesOf_RetiredTrooper_IsLeftOut()
    {
        var season = new SeasonFile { Retired = [new RetiredTrooper { EntryIndex = 1 }] };

        Assert.Empty(CaptainChange.CandidatesOf(Company(), season));
    }
}
