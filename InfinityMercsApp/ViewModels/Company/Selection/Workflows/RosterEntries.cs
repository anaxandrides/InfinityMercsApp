using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

/// <summary>A saved company's troopers as roster entries, so recruiting counts them against AVA and the company's rules.</summary>
public static class RosterEntries
{
    /// <summary>
    /// The company's troopers, lieutenant first, marked as already on the roster. Peripherals are left out: they
    /// come with the trooper they belong to.
    /// </summary>
    public static IReadOnlyList<MercsCompanyEntry> From(SavedCompanyFile company) =>
        company.Entries
            .Where(entry => !entry.IsPeripheralUnit)
            .OrderByDescending(entry => entry.IsLieutenant)
            .ThenBy(entry => entry.EntryIndex)
            .Select(Of)
            .ToList();

    private static MercsCompanyEntry Of(SavedCompanyEntry entry)
    {
        var baseName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName;
        var name = string.IsNullOrWhiteSpace(entry.CustomName) ? baseName : entry.CustomName.Trim();
        var logoFactionId = entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : entry.SourceFactionId;
        var logoUnitId = entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : entry.SourceUnitId;
        return new MercsCompanyEntry
        {
            Name = name,
            BaseUnitName = baseName,
            NameFormatted = CompanyProfileTextService.BuildNameFormatted(name),
            Subtitle = $"MOV {entry.CurrentMov} | CC {entry.CurrentCc} | BS {entry.CurrentBs} | PH {entry.CurrentPh} | WIP {entry.CurrentWip} | ARM {entry.CurrentArm} | BTS {entry.CurrentBts} | VITA {entry.CurrentVitaOrStr} | S {entry.CurrentS}",
            UnitTypeCode = entry.UnitTypeCode,
            CostDisplay = $"C {entry.Cost}",
            CostValue = entry.Cost,
            IsLieutenant = entry.IsLieutenant,
            ProfileKey = entry.ProfileKey,
            SourceUnitId = entry.SourceUnitId,
            SourceFactionId = entry.SourceFactionId,
            LogoSourceFactionId = logoFactionId,
            LogoSourceUnitId = logoUnitId,
            PackagedLogoPath = $"SVGCache/units/{logoFactionId}-{logoUnitId}.svg",
            ExperiencePoints = entry.ExperiencePoints,
            IsOnRoster = true
        };
    }
}
