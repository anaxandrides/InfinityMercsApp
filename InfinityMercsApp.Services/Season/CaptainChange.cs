using InfinityMercsApp.Domain.Lieutenants;
using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.Season;

/// <summary>
/// A downtime change of leadership: any trooper of the company can become the Captain. The old Captain keeps the
/// captain upgrades it was founded with, since they are saved on its own line.
/// </summary>
public sealed class CaptainChange(IArmyDataService armyData)
{
    /// <summary>The troopers who can become Captain: active ones that aren't peripherals or the Captain already.</summary>
    public static IReadOnlyList<SavedCompanyEntry> CandidatesOf(SavedCompanyFile company, SeasonFile? season) =>
        company.Entries
            .Where(entry => !entry.IsLieutenant && !entry.IsPeripheralUnit && entry.AspectOf is null && season?.IsRetired(entry.EntryIndex) != true)
            .ToList();

    /// <summary>
    /// Makes the trooper at <paramref name="entryIndex"/> the Captain with the Lieutenant skill; the old Captain loses
    /// the role and the skill. False, changing nothing, when that trooper can't become Captain.
    /// </summary>
    public bool Apply(SavedCompanyFile company, int entryIndex, SeasonFile? season)
    {
        if (CandidatesOf(company, season).FirstOrDefault(entry => entry.EntryIndex == entryIndex) is not { } next)
        {
            return false;
        }

        foreach (var old in company.Entries.Where(entry => entry.IsLieutenant))
        {
            var filters = FiltersOf(old);
            old.IsLieutenant = false;
            old.CurrentSkillCodes.RemoveAll(code =>
                SavedCodeNames.Resolve(filters, [code], ProfileEntryKind.Skills).Any(LieutenantRules.IsLieutenantSkill));
            old.CustomSkills.RemoveAll(LieutenantRules.IsLieutenantSkill);
        }

        next.IsLieutenant = true;
        var lieutenant = new SavedCodeEncoder(armyData).Encode(
            [LieutenantRules.SkillName], ProfileEntryKind.Skills, [SeasonTroopers.SourceFactionId(next)]);
        next.CurrentSkillCodes.AddRange(lieutenant.Codes);
        next.CustomSkills.AddRange(lieutenant.CustomNames);
        return true;
    }

    private ArmyFilterCatalog? FiltersOf(SavedCompanyEntry entry) =>
        armyData.GetFactionSnapshot(SeasonTroopers.SourceFactionId(entry))?.Filters;
}
