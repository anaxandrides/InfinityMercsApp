using InfinityMercsApp.Domain.Models.Company;

namespace InfinityMercsApp.Web.Presentation;

/// <summary>One trooper or peripheral on a company sheet.</summary>
/// <param name="Role">"Captain", "Lone Wolf", "Aspect", "Peripheral" or empty.</param>
/// <param name="Attributes">The current attributes in Infinity's order, e.g. "MOV 4-4 · CC 14 · BS 12 · …".</param>
public sealed record TrooperRow(string Name, string Unit, string Role, int Cost, int Experience, string Rank, string Attributes)
{
    public static TrooperRow From(SavedCompanyEntry entry) => new(
        string.IsNullOrWhiteSpace(entry.CustomName) ? entry.Name : entry.CustomName,
        string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName,
        RoleOf(entry),
        entry.Cost,
        entry.ExperiencePoints,
        entry.ExperienceRankName,
        string.Join(" · ", new List<string>
        {
            $"MOV {entry.CurrentMov}",
            $"CC {entry.CurrentCc}",
            $"BS {entry.CurrentBs}",
            $"PH {entry.CurrentPh}",
            $"WIP {entry.CurrentWip}",
            $"ARM {entry.CurrentArm}",
            $"BTS {entry.CurrentBts}",
            $"VITA {entry.CurrentVitaOrStr}",
            $"S {entry.CurrentS}"
        }));

    private static string RoleOf(SavedCompanyEntry entry) => entry switch
    {
        { IsLoneWolf: true } => "Lone Wolf",
        { IsLieutenant: true } => "Captain",
        { AspectOf: not null } => "Aspect",
        { IsPeripheralUnit: true } => "Peripheral",
        _ => string.Empty
    };
}
