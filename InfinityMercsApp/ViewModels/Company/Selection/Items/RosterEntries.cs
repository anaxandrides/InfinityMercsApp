using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Season.Peripherals;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

/// <summary>A saved company's troopers as roster entries, so recruiting counts them against AVA and the company's rules.</summary>
public static class RosterEntries
{
    /// <summary>
    /// The company's troopers, lieutenant first, marked as already on the roster. Peripherals are left out: they
    /// come with the trooper they belong to. A Servant recruited on its own stays, after its owner, since it cost
    /// CR and counts toward AVA. Retired troopers stay, named as retired, since AVA counts every trooper the
    /// company has had.
    /// </summary>
    public static IReadOnlyList<MercsCompanyEntry> From(
        SavedCompanyFile company,
        IReadOnlyCollection<int> retiredEntries,
        CompanyServants servants)
    {
        var troopers = company.Entries
            .Where(entry => !entry.IsPeripheralUnit)
            .OrderByDescending(entry => entry.IsLieutenant)
            .ThenBy(entry => entry.EntryIndex)
            .Select(entry => Of(entry, retiredEntries.Contains(entry.EntryIndex), servants.Owners.Contains(entry.EntryIndex), null))
            .ToList();
        var result = new List<MercsCompanyEntry>(troopers.Count);
        foreach (var trooper in troopers)
        {
            result.Add(trooper);
            result.AddRange(company.Entries
                .Where(entry => IsRecruitedServant(entry, servants) && entry.ParentEntryIndex == trooper.SavedEntryIndex)
                .Select(entry => Of(entry, retiredEntries.Contains(entry.EntryIndex), false, trooper)));
        }

        return result;
    }

    // A Servant bought on its own has its own cost; one that came with its trooper costs nothing.
    private static bool IsRecruitedServant(SavedCompanyEntry entry, CompanyServants servants) =>
        entry.IsPeripheralUnit && entry.Cost > 0 && servants.Servants.Contains(entry.EntryIndex);

    private static MercsCompanyEntry Of(SavedCompanyEntry entry, bool isRetired, bool canOwnServant, MercsCompanyEntry? servantOwner)
    {
        var baseName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName;
        var customName = string.IsNullOrWhiteSpace(entry.CustomName) ? baseName : entry.CustomName.Trim();
        var name = isRetired ? $"{customName} (Retired)" : customName;
        var logoFactionId = entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : entry.SourceFactionId;
        var logoUnitId = entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : entry.SourceUnitId;
        return new MercsCompanyEntry
        {
            Name = name,
            BaseUnitName = baseName,
            NameFormatted = CompanyProfileFormatting.BuildNameFormatted(name),
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
            IsOnRoster = true,
            SavedEntryIndex = entry.EntryIndex,
            CanOwnServant = canOwnServant,
            ServantOwner = servantOwner
        };
    }
}
