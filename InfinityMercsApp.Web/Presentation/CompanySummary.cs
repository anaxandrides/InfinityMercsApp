using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Web.Presentation;

/// <summary>A company as the list of the user's companies shows it.</summary>
public sealed record CompanySummary(string Id, string Name, string Type, string Armies, int Troopers)
{
    public static CompanySummary From(string id, SavedCompanyFile company) => new(
        id,
        company.CompanyName,
        company.CompanyType,
        ArmyNames.Of(company),
        company.Entries.Count(entry => !entry.IsPeripheralUnit && entry.AspectOf is null));
}
