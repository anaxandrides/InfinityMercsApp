using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.Services.CompanyProfiles;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.CompanySelection.Units;

internal static partial class CompanyUnitDetailsShared
{
    internal static string BuildPeripheralSubtitle(
        string mov,
        string cc,
        string bs,
        string ph,
        string wip,
        string arm,
        string bts,
        string vitalityHeader,
        string vitality,
        string s,
        string ava)
    {
        return $"MOV {mov} | CC {cc} | BS {bs} | PH {ph} | WIP {wip} | ARM {arm} | BTS {bts} | {vitalityHeader} {vitality} | S {s} | AVA {ava}";
    }

    internal static string BuildPeripheralSubtitle(PeripheralMercsCompanyStats? stats) =>
        stats is null
            ? "-"
            : BuildPeripheralSubtitle(
                stats.Mov,
                stats.Cc,
                stats.Bs,
                stats.Ph,
                stats.Wip,
                stats.Arm,
                stats.Bts,
                stats.VitalityHeader,
                stats.Vitality,
                stats.S,
                stats.Ava);

    /// <summary>
    /// Builds the stat block for a peripheral statline, resolving names against the army filters JSON.
    /// </summary>
    internal static PeripheralMercsCompanyStats? BuildPeripheralStatBlock(
        string peripheralName,
        UnitProfile peripheralProfile,
        ArmyFilterCatalog? filters,
        bool showUnitsInInches)
    {
        return CompanyPeripheralStatBlockService.Build(
            peripheralName,
            peripheralProfile,
            (filters ?? ArmyFilterCatalog.Empty),
            showUnitsInInches);
    }

    /// <summary>
    /// Shows the stat block of the first peripheral named by a visible profile row.
    /// </summary>
    internal static void UpdatePeripheralStatBlockFromVisibleProfiles<TProfile>(
        UnitProfileGroups? selectedUnitProfileGroups,
        IEnumerable<TProfile> profiles,
        Func<TProfile, bool> isVisible,
        Func<TProfile, bool> hasPeripherals,
        Func<TProfile, string?> readPeripherals,
        Func<string?, string> extractFirstPeripheralName,
        Action resetPeripheralStatsOnly,
        Action<UnitProfile, string> populatePeripheralStats)
    {
        resetPeripheralStatsOnly();

        if (selectedUnitProfileGroups is not { IsEmpty: false })
        {
            return;
        }

        var firstPeripheralPeripherals = profiles
            .Where(x => isVisible(x) && hasPeripherals(x))
            .Select(readPeripherals)
            .FirstOrDefault();
        if (firstPeripheralPeripherals is null)
        {
            return;
        }

        var peripheralName = extractFirstPeripheralName(firstPeripheralPeripherals);
        if (string.IsNullOrWhiteSpace(peripheralName))
        {
            return;
        }

        var peripheralProfile = (selectedUnitProfileGroups ?? UnitProfileGroups.Empty)
            .FindPeripheralProfile(peripheralName);
        if (peripheralProfile is not null)
        {
            populatePeripheralStats(peripheralProfile, peripheralName);
        }
    }
}
