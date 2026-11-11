using InfinityMercsApp.Domain.Captain;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.SpecOps;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.SpecOps;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

/// <summary>A Spec-Ops or Team-Ops trooper once its upgrades (and a Spec-Ops's SpecBot) are chosen.</summary>
public static class UpgradedEntry
{
    /// <summary>
    /// <paramref name="entry"/> with <paramref name="upgrades"/> applied: the bonuses and any new MOV on its statline,
    /// the weapons, skills and equipment added, and each upgrade listed among its purchases. A
    /// <paramref name="peripheral"/> joins as its peripheral and adds its cost.
    /// </summary>
    public static MercsCompanyEntry From(
        MercsCompanyEntry entry,
        IReadOnlyList<TrooperUpgrade> upgrades,
        SpecBotOption? peripheral,
        bool showUnitsInInches)
    {
        var combined = SpecUpgradeRules.Combine(upgrades);
        var ranged = Merged(entry.SavedRangedWeapons, combined.Weapons.Where(name => !CompanyProfileText.IsMeleeWeaponName(name)));
        var melee = Merged(entry.SavedCcWeapons, combined.Weapons.Where(CompanyProfileText.IsMeleeWeaponName));
        var skills = Merged(entry.SavedSkills, combined.Skills);
        var equipment = Merged(entry.SavedEquipment, combined.Equipment);
        var moveFirstCm = combined.MoveFirstCm ?? entry.UnitMoveFirstCm;
        var moveSecondCm = combined.MoveSecondCm ?? entry.UnitMoveSecondCm;
        var moveDisplay = combined.SetsMove
            ? Movement.Format(Movement.From(moveFirstCm, moveSecondCm), showUnitsInInches)
            : entry.UnitMoveDisplay;
        var statline = combined.Bonuses.ApplyToStatline(entry.Subtitle);
        if (combined.SetsMove)
        {
            statline = Statline.WithMove(statline, moveDisplay);
        }

        var cost = entry.CostValue + (peripheral?.Cost ?? 0);
        var stats = peripheral?.Stats;
        var peripheralEquipment = stats?.Equipment ?? entry.SavedPeripheralEquipment;
        var peripheralSkills = stats?.Skills ?? entry.SavedPeripheralSkills;
        var hasPeripheral = stats is not null || entry.HasPeripheralStatBlock;

        return new MercsCompanyEntry
        {
            Name = entry.Name,
            BaseUnitName = entry.BaseUnitName,
            NameFormatted = entry.NameFormatted,
            CostDisplay = $"C {cost}",
            CostValue = cost,
            ProfileKey = entry.ProfileKey,
            ProfileGroupId = entry.ProfileGroupId,
            IsLieutenant = entry.IsLieutenant,
            IsLoneWolf = entry.IsLoneWolf,
            IsSpecOps = entry.IsSpecOps,
            CustomName = entry.CustomName,
            CanOwnServant = entry.CanOwnServant,
            ServantOwner = entry.ServantOwner,
            SourceUnitId = entry.SourceUnitId,
            SourceFactionId = entry.SourceFactionId,
            LogoSourceFactionId = entry.LogoSourceFactionId,
            LogoSourceUnitId = entry.LogoSourceUnitId,
            CachedLogoPath = entry.CachedLogoPath,
            PackagedLogoPath = entry.PackagedLogoPath,
            Subtitle = statline,
            UnitTypeCode = entry.UnitTypeCode,
            SavedEquipment = equipment,
            SavedSkills = skills,
            SavedCharacteristics = entry.SavedCharacteristics,
            SavedRangedWeapons = ranged,
            SavedCcWeapons = melee,
            UnitMoveFirstCm = moveFirstCm,
            UnitMoveSecondCm = moveSecondCm,
            UnitMoveDisplay = moveDisplay,
            HasPeripheralStatBlock = hasPeripheral,
            PeripheralNameHeading = stats?.NameHeading ?? entry.PeripheralNameHeading,
            PeripheralMov = stats is null ? entry.PeripheralMov : Movement.Format(Movement.From(stats.MoveFirstCm, stats.MoveSecondCm), showUnitsInInches),
            PeripheralCc = stats?.Cc ?? entry.PeripheralCc,
            PeripheralBs = stats?.Bs ?? entry.PeripheralBs,
            PeripheralPh = stats?.Ph ?? entry.PeripheralPh,
            PeripheralWip = stats?.Wip ?? entry.PeripheralWip,
            PeripheralArm = stats?.Arm ?? entry.PeripheralArm,
            PeripheralBts = stats?.Bts ?? entry.PeripheralBts,
            PeripheralVitalityHeader = stats?.VitalityHeader ?? entry.PeripheralVitalityHeader,
            PeripheralVitality = stats?.Vitality ?? entry.PeripheralVitality,
            PeripheralS = stats?.S ?? entry.PeripheralS,
            PeripheralAva = stats?.Ava ?? entry.PeripheralAva,
            PeripheralMoveFirstCm = stats is null ? entry.PeripheralMoveFirstCm : stats.MoveFirstCm,
            PeripheralMoveSecondCm = stats is null ? entry.PeripheralMoveSecondCm : stats.MoveSecondCm,
            SavedPeripheralEquipment = peripheralEquipment,
            SavedPeripheralSkills = peripheralSkills,
            SavedPeripheralCharacteristics = stats?.Characteristics ?? entry.SavedPeripheralCharacteristics,
            SavedPeripheralRangedWeapons = stats?.RangedWeapons ?? entry.SavedPeripheralRangedWeapons,
            SavedPeripheralCcWeapons = stats?.CcWeapons ?? entry.SavedPeripheralCcWeapons,
            EquipmentLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Equipment", equipment, Color.FromArgb("#06B6D4")),
            HasEquipmentLine = !string.Equals(equipment, "-", StringComparison.Ordinal),
            SkillsLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Skills", skills, Color.FromArgb("#F59E0B")),
            HasSkillsLine = !string.Equals(skills, "-", StringComparison.Ordinal),
            RangedLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Ranged Weapons", ranged, Color.FromArgb("#EF4444")),
            CcLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("CC Weapons", melee, Color.FromArgb("#22C55E")),
            PeripheralEquipmentLineFormatted = stats is null
                ? entry.PeripheralEquipmentLineFormatted
                : CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Equipment", peripheralEquipment, Color.FromArgb("#06B6D4")),
            HasPeripheralEquipmentLine = hasPeripheral && !string.Equals(peripheralEquipment, "-", StringComparison.Ordinal),
            PeripheralSkillsLineFormatted = stats is null
                ? entry.PeripheralSkillsLineFormatted
                : CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Skills", peripheralSkills, Color.FromArgb("#F59E0B")),
            HasPeripheralSkillsLine = hasPeripheral && !string.Equals(peripheralSkills, "-", StringComparison.Ordinal),
            ExperiencePoints = entry.ExperiencePoints,
            SpecOpsPurchases = [.. entry.SpecOpsPurchases, .. upgrades.Select(upgrade => upgrade.PurchaseLine)],
            Perks = entry.Perks
                .Select(perk => new CompanyTrooperPerkState { Id = perk.Id, Rank = perk.Rank })
                .ToList(),
            IsIrregular = entry.IsIrregular,
            NormallyIrregular = entry.NormallyIrregular,
            IsSelected = entry.IsSelected,
            IsDetailsExpanded = entry.IsDetailsExpanded
        };
    }

    // Adds the names a line lacks.
    private static string Merged(string existing, IEnumerable<string> additions)
    {
        var values = CompanyProfileText.SplitDisplayLine(existing);
        values.AddRange(additions.Where(name => !values.Contains(name, StringComparer.OrdinalIgnoreCase)).ToList());
        return CompanyProfileText.JoinOrDash(values);
    }
}
