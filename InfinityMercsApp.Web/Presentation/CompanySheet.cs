using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Web.Presentation;

/// <summary>A company's page: what it is, and every trooper with its attributes, Captain first.</summary>
public sealed record CompanySheet(string Id, string Name, string Type, string Armies, int StartingPoints, IReadOnlyList<TrooperRow> Troopers)
{
    public static CompanySheet From(string id, SavedCompanyFile company) => new(
        id,
        company.CompanyName,
        company.CompanyType,
        ArmyNames.Of(company),
        company.StartSeasonPoints,
        company.Entries
            .OrderByDescending(entry => entry.IsLieutenant)
            .ThenBy(entry => entry.EntryIndex)
            .Select(TrooperRow.From)
            .ToList());
}
