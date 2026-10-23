using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.Web.UnitTests;

/// <summary>A small company and season to show.</summary>
internal static class TestFiles
{
    public static SavedCompanyFile Company(string name = "Blue Lions") => new()
    {
        CompanyName = name,
        CompanyType = "Standard Company - Vanilla",
        CompanyIdentifier = "c1",
        StartSeasonPoints = 75,
        SourceFactions = [new SavedCompanyFaction { FactionId = 101, FactionName = "PanOceania" }],
        Entries =
        [
            new SavedCompanyEntry { EntryIndex = 0, Name = "Fusilier", CustomName = "Rook", Cost = 10 },
            new SavedCompanyEntry { EntryIndex = 1, Name = "Bulleteer", Cost = 20, ExperiencePoints = 12, IsLieutenant = true, CurrentCc = "15" },
            new SavedCompanyEntry { EntryIndex = 2, Name = "Traktor Mul", IsPeripheralUnit = true, ParentEntryIndex = 1 }
        ]
    };

    public static SeasonFile Season(string companyName = "Blue Lions", string started = "2026-10-01") => new()
    {
        SeasonIdentifier = "s1",
        CompanyName = companyName,
        CompanyIdentifier = "c1",
        CreatedDate = started,
        Rounds =
        [
            new SeasonRound
            {
                RoundIndex = 2,
                MissionResults = new SeasonMissionResult { IsBye = true }
            },
            new SeasonRound
            {
                RoundIndex = 1,
                MissionResults = new SeasonMissionResult
                {
                    Won = true,
                    OpScored = 7,
                    UnitsDeployed = 5,
                    UnitResults = [new SeasonMissionUnitResult { XpGained = 4 }, new SeasonMissionUnitResult { XpGained = 3 }]
                }
            }
        ],
        Retired = [new RetiredTrooper { EntryIndex = 0, Name = "Rook", AfterRound = 1 }]
    };
}
