using InfinityMercsApp.Domain.Models.Army.Profiles;

namespace InfinityMercsApp.Views.Common;

internal static partial class CompanyUnitDetailsShared
{
    /// <summary>
    /// Shows the first profile's statline (or clears the stats when the unit has no profiles).
    /// </summary>
    internal static void PopulateUnitStatsFromFirstProfile(
        UnitProfileGroups profileGroups,
        Action resetUnitStatsOnly,
        Action<UnitProfile> populateUnitStats)
    {
        resetUnitStatsOnly();

        var firstProfile = profileGroups.Groups.SelectMany(group => group.Profiles).FirstOrDefault();
        if (firstProfile is not null)
        {
            populateUnitStats(firstProfile);
        }
    }

    internal static CompanyUnitStatProjection BuildUnitStatProjection(
        UnitProfile profile,
        Func<int?, int?, string> formatMoveValue)
    {
        var (moveFirstCm, moveSecondCm) = profile.MoveFirstSecondCm;
        var (vitalityHeader, vitalityValue) = profile.Vitality;
        return new CompanyUnitStatProjection(
            moveFirstCm,
            moveSecondCm,
            formatMoveValue(moveFirstCm, moveSecondCm),
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
