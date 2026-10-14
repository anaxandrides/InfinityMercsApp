using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Services.CompanyProfiles;

internal static class CompanyPeripheralStatBlockService
{
    /// <summary>
    /// Builds the stat block shown for a unit's peripheral from its statline and the army filters.
    /// </summary>
    public static PeripheralMercsCompanyStats? Build(
        string peripheralName,
        UnitProfile peripheralProfile,
        ArmyFilterCatalog filters,
        bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(peripheralName))
        {
            return null;
        }

        var names = new ProfileEntryNames(filters.Extras, showUnitsInInches);
        var equipmentNames = names.OrderedUnique(peripheralProfile.Equip, filters.Names(ArmyFilterSection.Equip));
        var skillNames = CompanyProfileTextService.BuildConfigurationSkillNames(
            names.OrderedUnique(peripheralProfile.Skills, filters.Names(ArmyFilterSection.Skills)));
        var characteristicNames = names.OrderedUnique(peripheralProfile.Chars, filters.Names(ArmyFilterSection.Chars));
        var weaponNames = names.OrderedUnique(peripheralProfile.Weapons, filters.Names(ArmyFilterSection.Weapons));
        var moveFirstCm = peripheralProfile.Movement?.FirstCm;
        var moveSecondCm = peripheralProfile.Movement?.SecondCm;
        var (vitalityStat, vitalityValue) = peripheralProfile.Vitality;
        var vitalityHeader = vitalityStat.Key();

        return new PeripheralMercsCompanyStats
        {
            NameHeading = $"Peripheral: {peripheralName}",
            MoveFirstCm = moveFirstCm,
            MoveSecondCm = moveSecondCm,
            Mov = peripheralProfile.Move,
            Cc = UnitProfile.StatText(peripheralProfile.Cc),
            Bs = UnitProfile.StatText(peripheralProfile.Bs),
            Ph = UnitProfile.StatText(peripheralProfile.Ph),
            Wip = UnitProfile.StatText(peripheralProfile.Wip),
            Arm = UnitProfile.StatText(peripheralProfile.Arm),
            Bts = UnitProfile.StatText(peripheralProfile.Bts),
            VitalityHeader = vitalityHeader,
            Vitality = vitalityValue,
            S = UnitProfile.StatText(peripheralProfile.S),
            Ava = peripheralProfile.AvaText,
            Equipment = CompanyProfileTextService.JoinOrDash(equipmentNames),
            Skills = CompanyProfileTextService.JoinOrDash(skillNames),
            Characteristics = CompanyProfileTextService.JoinOrDash(characteristicNames),
            RangedWeapons = CompanyProfileTextService.JoinOrDash(weaponNames.Where(x => !CompanyProfileTextService.IsMeleeWeaponName(x))),
            CcWeapons = CompanyProfileTextService.JoinOrDash(weaponNames.Where(CompanyProfileTextService.IsMeleeWeaponName))
        };
    }
}
