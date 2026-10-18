using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Presentation.Company.Selection;
using InfinityMercsApp.Presentation.Units;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Profiles;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;

namespace InfinityMercsApp.ViewModels.Company.Selection.Items;

/// <summary>A trooper added to the company from a selection screen's picked unit and profile.</summary>
public static class NewTrooperEntry
{
    /// <summary>
    /// A profile of the picked unit as a new company trooper: the unit's shared equipment and skills merged with
    /// the profile's own, its statline, and its peripheral's. A controlled peripheral makes its controller an
    /// FT Master and is Irregular. A Transmutation (Auto) trooper recruited in one <paramref name="form"/> takes that
    /// form's statline, skills, equipment and characteristics, and loses Transmutation (Auto). A Servant recruited on
    /// its own goes to its <paramref name="servantOwner"/>, a Doctor or Engineer in the company.
    /// </summary>
    public static MercsCompanyEntry From(
        ArmyUnitSelectionItem selectedUnit,
        ViewerProfileItem profile,
        CompanyUnitDetail detail,
        IReadOnlyCollection<string> commonSkills,
        PeripheralMercsCompanyStats? peripheralStats,
        TrooperForm? form = null,
        MercsCompanyEntry? servantOwner = null)
    {
        string Move(int? firstCm, int? secondCm) => Movement.Format(Movement.From(firstCm, secondCm), detail.ShowUnitsInInches);

        var stats = form is null ? detail.Stats : UnitStatLine.From(form.Profile, detail.ShowUnitsInInches);
        var unitMoveFirstCm = form is null ? detail.UnitMoveFirstCm : form.Profile.Movement?.FirstCm;
        var unitMoveSecondCm = form is null ? detail.UnitMoveSecondCm : form.Profile.Movement?.SecondCm;
        var combinedEquipment = CompanyProfileText.MergeCommonAndUnique(detail.CommonEquipment.ToList(), profile.UniqueEquipment);
        var combinedSkills = CompanyProfileText.MergeCommonAndUnique(commonSkills, profile.UniqueSkills);
        var combinedCharacteristics = CompanyProfileText.SplitDisplayLine(profile.Characteristics).ToList();
        if (form is not null)
        {
            combinedSkills.RemoveAll(TransmutationForms.IsAuto);
            AddMissing(combinedSkills, form.Skills);
            AddMissing(combinedEquipment, form.Equipment);
            AddMissing(combinedCharacteristics, form.Characteristics);
        }

        var inferredPeripheralName = PeripheralNames.FirstOf(profile.Peripherals);
        var peripheralHasControlModifier = ProfileHasPeripheralControlModifier(profile, inferredPeripheralName);
        if (peripheralHasControlModifier && !string.IsNullOrWhiteSpace(inferredPeripheralName))
        {
            EnsureSkill(combinedSkills, PeripheralControl.FtMaster(inferredPeripheralName));
        }
        var combinedEquipmentText = CompanyProfileText.JoinOrDash(combinedEquipment);
        var combinedSkillsText = CompanyProfileText.JoinOrDash(combinedSkills);
        var combinedCharacteristicsText = CompanyProfileText.JoinOrDash(combinedCharacteristics);
        var hasInferredPeripheral = !string.IsNullOrWhiteSpace(inferredPeripheralName);
        var hasPeripheralData = peripheralStats is not null || hasInferredPeripheral;
        var peripheralNameHeading = peripheralStats?.NameHeading;
        if (string.IsNullOrWhiteSpace(peripheralNameHeading) && hasInferredPeripheral)
        {
            peripheralNameHeading = $"Peripheral: {inferredPeripheralName}";
        }

        var peripheralMov = peripheralStats is null
            ? (profile.HasPeripheralStatBlock ? profile.PeripheralMov : "-")
            : Move(peripheralStats.MoveFirstCm, peripheralStats.MoveSecondCm);
        var peripheralCc = peripheralStats?.Cc ?? (profile.HasPeripheralStatBlock ? profile.PeripheralCc : "-");
        var peripheralBs = peripheralStats?.Bs ?? (profile.HasPeripheralStatBlock ? profile.PeripheralBs : "-");
        var peripheralPh = peripheralStats?.Ph ?? (profile.HasPeripheralStatBlock ? profile.PeripheralPh : "-");
        var peripheralWip = peripheralStats?.Wip ?? (profile.HasPeripheralStatBlock ? profile.PeripheralWip : "-");
        var peripheralArm = peripheralStats?.Arm ?? (profile.HasPeripheralStatBlock ? profile.PeripheralArm : "-");
        var peripheralBts = peripheralStats?.Bts ?? (profile.HasPeripheralStatBlock ? profile.PeripheralBts : "-");
        var peripheralVitalityHeader = peripheralStats?.VitalityHeader ?? (profile.HasPeripheralStatBlock ? profile.PeripheralVitalityHeader : Stat.Vita.Key());
        var peripheralVitality = peripheralStats?.Vitality ?? (profile.HasPeripheralStatBlock ? profile.PeripheralVitality : "-");
        var peripheralS = peripheralStats?.S ?? (profile.HasPeripheralStatBlock ? profile.PeripheralS : "-");
        var peripheralAva = peripheralStats?.Ava ?? (profile.HasPeripheralStatBlock ? profile.PeripheralAva : "-");
        var peripheralEquipment = peripheralStats?.Equipment ?? "-";
        var peripheralSkillsText = peripheralStats?.Skills ?? "-";
        var peripheralCharacteristicsText = peripheralStats?.Characteristics ?? "-";
        var peripheralRangedWeapons = peripheralStats?.RangedWeapons ?? "-";
        var peripheralCcWeapons = peripheralStats?.CcWeapons ?? "-";
        if (profile.PeripheralIsIrregular || peripheralHasControlModifier)
        {
            var peripheralSkillLines = CompanyProfileText.SplitDisplayLine(peripheralSkillsText).ToList();
            if (!peripheralSkillLines.Any(x => string.Equals(x, "Irregular", StringComparison.OrdinalIgnoreCase)))
            {
                peripheralSkillLines.Add("Irregular");
            }

            peripheralSkillsText = CompanyProfileText.JoinOrDash(peripheralSkillLines);

            var peripheralCharacteristicLines = CompanyProfileText.SplitDisplayLine(peripheralCharacteristicsText).ToList();
            if (!peripheralCharacteristicLines.Any(x => string.Equals(x, "Irregular", StringComparison.OrdinalIgnoreCase)))
            {
                peripheralCharacteristicLines.Add("Irregular");
            }

            peripheralCharacteristicsText = CompanyProfileText.JoinOrDash(peripheralCharacteristicLines);
        }
        var currentUnitMove = Move(unitMoveFirstCm, unitMoveSecondCm);
        var statline =
            $"MOV {stats.Mov} | CC {stats.Cc} | BS {stats.Bs} | PH {stats.Ph} | WIP {stats.Wip} | ARM {stats.Arm} | BTS {stats.Bts} | {stats.VitalityHeader} {stats.Vitality} | S {stats.S}";

        return new MercsCompanyEntry
        {
            Name = profile.Name,
            BaseUnitName = selectedUnit.Name,
            NameFormatted = profile.NameFormatted ?? CompanyProfileFormatting.BuildNameFormatted(profile.Name),
            Subtitle = statline,
            UnitTypeCode = UnitTypeCodes.FromSubtitle(selectedUnit.Subtitle),
            CostDisplay = $"C {profile.Cost}",
            CostValue = ProfileValues.ParseCost(profile.Cost),
            IsLieutenant = profile.IsLieutenant,
            IsSpecOps = selectedUnit.IsSpecOps,
            ProfileKey = profile.ProfileKey,
            SourceUnitId = selectedUnit.Id,
            SourceFactionId = selectedUnit.SourceFactionId,
            LogoSourceFactionId = selectedUnit.LogoSourceFactionId > 0 ? selectedUnit.LogoSourceFactionId : selectedUnit.SourceFactionId,
            LogoSourceUnitId = selectedUnit.LogoSourceUnitId > 0 ? selectedUnit.LogoSourceUnitId : selectedUnit.Id,
            CachedLogoPath = selectedUnit.CachedLogoPath,
            PackagedLogoPath = selectedUnit.PackagedLogoPath,
            SavedEquipment = combinedEquipmentText,
            SavedSkills = combinedSkillsText,
            SavedCharacteristics = combinedCharacteristicsText,
            SavedRangedWeapons = profile.RangedWeapons,
            SavedCcWeapons = profile.MeleeWeapons,
            ExperiencePoints = 0,
            EquipmentLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Equipment", combinedEquipmentText, Color.FromArgb("#06B6D4")),
            HasEquipmentLine = combinedEquipment.Count > 0,
            SkillsLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Skills", combinedSkillsText, Color.FromArgb("#F59E0B")),
            HasSkillsLine = combinedSkills.Count > 0,
            CanOwnServant = PeripheralKinds.CanOwnServant(combinedSkills),
            ServantOwner = servantOwner,
            RangedLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Ranged Weapons", profile.RangedWeapons, Color.FromArgb("#EF4444")),
            CcLineFormatted = CompanyProfileFormatting.BuildMercsCompanyLineFormatted("CC Weapons", profile.MeleeWeapons, Color.FromArgb("#22C55E")),
            HasPeripheralStatBlock = hasPeripheralData,
            PeripheralNameHeading = peripheralNameHeading ?? string.Empty,
            PeripheralMov = peripheralMov,
            PeripheralCc = peripheralCc,
            PeripheralBs = peripheralBs,
            PeripheralPh = peripheralPh,
            PeripheralWip = peripheralWip,
            PeripheralArm = peripheralArm,
            PeripheralBts = peripheralBts,
            PeripheralVitalityHeader = peripheralVitalityHeader,
            PeripheralVitality = peripheralVitality,
            PeripheralS = peripheralS,
            PeripheralAva = peripheralAva,
            SavedPeripheralEquipment = peripheralEquipment,
            SavedPeripheralSkills = peripheralSkillsText,
            SavedPeripheralCharacteristics = peripheralCharacteristicsText,
            SavedPeripheralRangedWeapons = peripheralRangedWeapons,
            SavedPeripheralCcWeapons = peripheralCcWeapons,
            PeripheralEquipmentLineFormatted =
                CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Equipment", peripheralEquipment, Color.FromArgb("#06B6D4")),
            HasPeripheralEquipmentLine =
                hasPeripheralData && !string.IsNullOrWhiteSpace(peripheralEquipment) && peripheralEquipment != "-",
            PeripheralSkillsLineFormatted =
                CompanyProfileFormatting.BuildMercsCompanyLineFormatted("Skills", peripheralSkillsText, Color.FromArgb("#F59E0B")),
            HasPeripheralSkillsLine =
                hasPeripheralData && !string.IsNullOrWhiteSpace(peripheralSkillsText) && peripheralSkillsText != "-",
            UnitMoveFirstCm = unitMoveFirstCm,
            UnitMoveSecondCm = unitMoveSecondCm,
            UnitMoveDisplay = currentUnitMove,
            PeripheralMoveFirstCm = peripheralStats?.MoveFirstCm,
            PeripheralMoveSecondCm = peripheralStats?.MoveSecondCm
        };
    }

    private static void AddMissing(List<string> names, IEnumerable<string> more) =>
        names.AddRange(more.Where(name => !names.Contains(name, StringComparer.OrdinalIgnoreCase)).ToList());

    private static void EnsureSkill(ICollection<string> skills, string skill)
    {
        if (string.IsNullOrWhiteSpace(skill))
        {
            return;
        }

        if (skills.Any(x => string.Equals(x?.Trim(), skill, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        skills.Add(skill);
    }

    private static bool ProfileHasPeripheralControlModifier(ViewerProfileItem profile, string? peripheralName)
    {
        if (profile.PeripheralIsIrregular)
        {
            return true;
        }

        return PeripheralControl.Mentions(string.Join(" ",
            profile.Peripherals ?? string.Empty,
            profile.PeripheralNameHeading ?? string.Empty,
            profile.PeripheralSubtitle ?? string.Empty,
            peripheralName ?? string.Empty));
    }
}
