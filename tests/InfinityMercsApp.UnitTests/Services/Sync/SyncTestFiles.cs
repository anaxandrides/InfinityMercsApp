using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.UnitTests.Services.Sync;

/// <summary>Companies and seasons that pass the API's checks.</summary>
internal static class SyncTestFiles
{
    public static SavedCompanyFile Company(string identifier, string name = "Iron Wolves") => new()
    {
        CompanyName = name,
        CompanyType = "Standard Company",
        CompanyIdentifier = identifier,
        SourceFactions = [new SavedCompanyFaction { FactionId = 101, FactionName = "PanOceania" }],
        Entries = [new SavedCompanyEntry { EntryIndex = 0, IsLieutenant = true }]
    };

    public static SeasonFile Season(string identifier, string companyIdentifier, string companyName = "Iron Wolves") => new()
    {
        SeasonIdentifier = identifier,
        CompanyName = companyName,
        CompanyIdentifier = companyIdentifier
    };
}
