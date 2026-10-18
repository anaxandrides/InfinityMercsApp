using InfinityMercsApp.Domain.Models.Army.Stats;

namespace InfinityMercsApp.Services.CompanySelection.Units;

internal static partial class CompanyUnitDetailsShared
{
    internal static CompanyUnitStatProjection BuildUnitStatProjection(
        UnitProfile profile,
        bool showUnitsInInches)
    {
        var moveFirstCm = profile.Movement?.FirstCm;
        var moveSecondCm = profile.Movement?.SecondCm;
        var (vitalityStat, vitalityValue) = profile.Vitality;
        var vitalityHeader = vitalityStat.Key();
        return new CompanyUnitStatProjection(
            moveFirstCm,
            moveSecondCm,
            Movement.Format(profile.Movement, showUnitsInInches),
            UnitProfile.StatText(profile.Cc),
            UnitProfile.StatText(profile.Bs),
            UnitProfile.StatText(profile.Ph),
            UnitProfile.StatText(profile.Wip),
            UnitProfile.StatText(profile.Arm),
            UnitProfile.StatText(profile.Bts),
            UnitProfile.StatText(profile.S),
            profile.AvaText,
            vitalityHeader,
            vitalityValue);
    }
}
