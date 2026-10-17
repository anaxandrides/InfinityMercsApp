using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.ViewModels.Company.Selection.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

/// <summary>A TAG company trooper once its Spec Ops upgrades are chosen.</summary>
public static class TagFinalizedEntry
{
    /// <summary>The experience a TAG starts with, spent on its Spec Ops.</summary>
    public const int SpecOpsExperience = 20;

    /// <summary>
    /// <paramref name="entry"/> with its chosen Spec Ops: the upgrades merged into its weapons, skills and equipment,
    /// the bonuses on its statline, its new name, the Spec Ops experience, and a list of what was bought.
    /// </summary>
    public static MercsCompanyEntry From(MercsCompanyEntry entry, SavedImprovedCaptainStats stats)
    {
        var mergedRanged = MergeSelections(entry.SavedRangedWeapons, [stats.WeaponChoice1, stats.WeaponChoice2, stats.WeaponChoice3], includeMelee: false);
        var mergedCc = MergeSelections(entry.SavedCcWeapons, [stats.WeaponChoice1, stats.WeaponChoice2, stats.WeaponChoice3], includeMelee: true);
        var mergedSkills = MergeSelections(entry.SavedSkills, [stats.SkillChoice1, stats.SkillChoice2, stats.SkillChoice3], includeMelee: false);
        var mergedEquipment = MergeSelections(entry.SavedEquipment, [stats.EquipmentChoice1, stats.EquipmentChoice2, stats.EquipmentChoice3], includeMelee: false);
        var updatedName = string.IsNullOrWhiteSpace(stats.CaptainName) ? entry.Name : stats.CaptainName.Trim();
        var updatedStatline = stats.Bonuses.ApplyToStatline(entry.Subtitle);

        return new MercsCompanyEntry
        {
            Name = updatedName,
            BaseUnitName = entry.BaseUnitName,
            NameFormatted = CompanyProfileFormatting.BuildNameFormatted(updatedName),
            CostDisplay = entry.CostDisplay,
            CostValue = entry.CostValue,
            ProfileKey = entry.ProfileKey,
            IsLieutenant = entry.IsLieutenant,
            SourceUnitId = entry.SourceUnitId,
            SourceFactionId = entry.SourceFactionId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            CachedLogoPath = entry.CachedLogoPath,
            PackagedLogoPath = entry.PackagedLogoPath,
            Subtitle = updatedStatline,
            UnitTypeCode = entry.UnitTypeCode,
            SavedEquipment = mergedEquipment,
            SavedSkills = mergedSkills,
            SavedCharacteristics = entry.SavedCharacteristics,
            SavedRangedWeapons = mergedRanged,
            SavedCcWeapons = mergedCc,
            UnitMoveFirstCm = entry.UnitMoveFirstCm,
            UnitMoveSecondCm = entry.UnitMoveSecondCm,
            UnitMoveDisplay = entry.UnitMoveDisplay,
            HasPeripheralStatBlock = entry.HasPeripheralStatBlock,
            PeripheralNameHeading = entry.PeripheralNameHeading,
            PeripheralMov = entry.PeripheralMov,
            PeripheralCc = entry.PeripheralCc,
            PeripheralBs = entry.PeripheralBs,
            PeripheralPh = entry.PeripheralPh,
            PeripheralWip = entry.PeripheralWip,
            PeripheralArm = entry.PeripheralArm,
            PeripheralBts = entry.PeripheralBts,
            PeripheralVitalityHeader = entry.PeripheralVitalityHeader,
            PeripheralVitality = entry.PeripheralVitality,
            PeripheralS = entry.PeripheralS,
            PeripheralAva = entry.PeripheralAva,
            PeripheralMoveFirstCm = entry.PeripheralMoveFirstCm,
            PeripheralMoveSecondCm = entry.PeripheralMoveSecondCm,
            SavedPeripheralEquipment = entry.SavedPeripheralEquipment,
            SavedPeripheralSkills = entry.SavedPeripheralSkills,
            SavedPeripheralCharacteristics = entry.SavedPeripheralCharacteristics,
            EquipmentLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Equipment", mergedEquipment, Color.FromArgb("#B5C0CE")),
            HasEquipmentLine = !string.Equals(mergedEquipment, "-", StringComparison.Ordinal),
            SkillsLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Skills", mergedSkills, Color.FromArgb("#F59E0B")),
            HasSkillsLine = !string.Equals(mergedSkills, "-", StringComparison.Ordinal),
            RangedLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Ranged Weapons", mergedRanged, Color.FromArgb("#F87171")),
            CcLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("CC Weapons", mergedCc, Color.FromArgb("#34D399")),
            PeripheralEquipmentLineFormatted = entry.PeripheralEquipmentLineFormatted,
            HasPeripheralEquipmentLine = entry.HasPeripheralEquipmentLine,
            PeripheralSkillsLineFormatted = entry.PeripheralSkillsLineFormatted,
            HasPeripheralSkillsLine = entry.HasPeripheralSkillsLine,
            ExperiencePoints = SpecOpsExperience,
            SpecOpsPurchases = stats.SpecOpsPurchases(StatKeys.VitalityFor(entry.UnitTypeCode)),
            Perks = entry.Perks
                .Select(x => new CompanyTrooperPerkState { Id = x.Id, Rank = x.Rank })
                .ToList(),
            IsIrregular = entry.IsIrregular,
            NormallyIrregular = entry.NormallyIrregular,
            IsSelected = entry.IsSelected,
            IsDetailsExpanded = entry.IsDetailsExpanded
        };
    }

    // Adds the chosen items to a profile section; melee choices only go with the CC weapons.
    private static string MergeSelections(
        string existing,
        IEnumerable<string?> additions,
        bool includeMelee)
    {
        var values = CompanyProfileText.SplitDisplayLine(existing);
        foreach (var addition in additions)
        {
            if (string.IsNullOrWhiteSpace(addition))
            {
                continue;
            }

            var trimmed = addition.Trim();
            var isMeleeChoice = trimmed.Contains("cc weapon", StringComparison.OrdinalIgnoreCase) ||
                                trimmed.Contains("melee", StringComparison.OrdinalIgnoreCase);
            if (includeMelee != isMeleeChoice)
            {
                continue;
            }

            if (values.Any(x => string.Equals(x, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            values.Add(trimmed);
        }

        return CompanyProfileText.JoinOrDash(values);
    }
}
