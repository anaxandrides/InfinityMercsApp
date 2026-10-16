using InfinityMercsApp.Domain.Models.Army.Filters;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using InfinityMercsApp.Services.CompanyProfiles;

namespace InfinityMercsApp.ViewModels.Company.Selection.Workflows;

internal static class CompanyProfilePopulationWorkflowService
{
    public static IReadOnlyList<ViewerProfileItem> BuildProfiles(CompanyProfilePopulationRequest request)
    {
        if (request.ProfileGroups.IsEmpty)
        {
            return [];
        }

        var filters = request.Filters;
        var unit = request.ProfileGroups;
        var buildRequest = new CompanyProfileBuildRequest
        {
            ProfileGroups = unit,
            ForceLieutenant = request.ForceLieutenant,
            ShowTacticalAwarenessIcon = request.ShowTacticalAwarenessIcon,
            WeaponsLookup = filters.Names(ArmyFilterSection.Weapons),
            EquipLookup = filters.Names(ArmyFilterSection.Equip),
            SkillsLookup = filters.Names(ArmyFilterSection.Skills),
            CharsLookup = filters.Names(ArmyFilterSection.Chars),
            PeripheralLookup = filters.Names(ArmyFilterSection.Peripheral),
            EntryNames = new ProfileEntryNames(filters.Extras, request.ShowUnitsInInches),
            ShouldIncludeOption = request.ShouldIncludeOption,
            ReadAdjustedOptionCost = (group, option) =>
            {
                var adjustedCost = unit.GetAdjustedCost(group, option);
                return request.AdjustOptionDisplayCost is null
                    ? adjustedCost
                    : request.AdjustOptionDisplayCost(group, option, adjustedCost);
            },
            BuildPeripheralStatBlock = (peripheralName, peripheralProfile) =>
                CompanyPeripheralStatBlockService.Build(peripheralName, peripheralProfile, filters, request.ShowUnitsInInches),
            FormatMoveValue = request.FormatMoveValue
        };

        return new CompanyProfileCoordinator().BuildProfiles(buildRequest).ToList();
    }
}
