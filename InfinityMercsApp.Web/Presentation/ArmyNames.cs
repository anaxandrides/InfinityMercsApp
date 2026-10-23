using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Web.Presentation;

/// <summary>The armies a company comes from, as one line.</summary>
public static class ArmyNames
{
    public static string Of(SavedCompanyFile company) =>
        string.Join(" + ", company.SourceFactions.Select(faction => faction.FactionName).Where(name => !string.IsNullOrWhiteSpace(name)));
}
