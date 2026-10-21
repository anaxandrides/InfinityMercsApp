using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Services.CompanyProfiles;

namespace InfinityMercsApp.Services.CompanySelection.Models.Saved;

public class PeripheralMercsCompanyStats
{
    public string NameHeading { get; init; } = string.Empty;
    public int? MoveFirstCm { get; init; }
    public int? MoveSecondCm { get; init; }
    public string Mov { get; init; } = "-";
    public string Cc { get; init; } = "-";
    public string Bs { get; init; } = "-";
    public string Ph { get; init; } = "-";
    public string Wip { get; init; } = "-";
    public string Arm { get; init; } = "-";
    public string Bts { get; init; } = "-";
    public string VitalityHeader { get; init; } = Stat.Vita.Key();
    public string Vitality { get; init; } = "-";
    public string S { get; init; } = "-";
    public string Ava { get; init; } = "-";
    public string Equipment { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string Characteristics { get; init; } = "-";
    public string RangedWeapons { get; init; } = "-";
    public string CcWeapons { get; init; } = "-";

    /// <summary>A unit's peripheral as its stat block shows it, its names read from the army filters; null without a name.</summary>
    public static PeripheralMercsCompanyStats? From(
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
        var skillNames = CompanyProfileText.BuildConfigurationSkillNames(
            names.OrderedUnique(peripheralProfile.Skills, filters.Names(ArmyFilterSection.Skills)));
        var characteristicNames = names.OrderedUnique(peripheralProfile.Chars, filters.Names(ArmyFilterSection.Chars));
        var weaponNames = names.OrderedUnique(peripheralProfile.Weapons, filters.Names(ArmyFilterSection.Weapons));
        var moveFirstCm = peripheralProfile.FirstMoveCm;
        var moveSecondCm = peripheralProfile.SecondMoveCm;
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
            Equipment = CompanyProfileText.JoinOrDash(equipmentNames),
            Skills = CompanyProfileText.JoinOrDash(skillNames),
            Characteristics = CompanyProfileText.JoinOrDash(characteristicNames),
            RangedWeapons = CompanyProfileText.JoinOrDash(weaponNames.Where(x => !CompanyProfileText.IsMeleeWeaponName(x))),
            CcWeapons = CompanyProfileText.JoinOrDash(weaponNames.Where(CompanyProfileText.IsMeleeWeaponName))
        };
    }

    /// <summary>The peripheral's statline on one line, e.g. "MOV 15-10 | CC 8 | … | AVA 2".</summary>
    public string Subtitle =>
        $"MOV {Mov} | CC {Cc} | BS {Bs} | PH {Ph} | WIP {Wip} | ARM {Arm} | BTS {Bts} | {VitalityHeader} {Vitality} | S {S} | AVA {Ava}";
}
