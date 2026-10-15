using InfinityMercsApp.Services.CompanySelection.Models.Saved;

namespace InfinityMercsApp.Services.CompanySelection.Captain;

internal static class CompanyCaptainWorkflowService
{
    public static async Task<SavedImprovedCaptainStats?> ShowCaptainConfigurationAsync(
        CompanyCaptainWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var sourceFactionId = CaptainPopupInputBuilder.ResolveSourceFactionId(
            request.FallbackSourceFactionId,
            request.FirstSourceFactionId);
        var optionFactionId = CaptainPopupInputBuilder.ResolveOptionFactionId(
            sourceFactionId,
            request.TryGetParentFactionId(sourceFactionId));
        var options = CaptainPopupInputBuilder.LoadUpgradeOptions(
            request.ArmyDataService,
            request.SpecOpsProvider,
            optionFactionId,
            request.ShowUnitsInInches,
            cancellationToken);

        if (options.IsEmpty && optionFactionId != sourceFactionId)
        {
            options = CaptainPopupInputBuilder.LoadUpgradeOptions(
                request.ArmyDataService,
                request.SpecOpsProvider,
                sourceFactionId,
                request.ShowUnitsInInches,
                cancellationToken);
            optionFactionId = sourceFactionId;
        }

        var context = new CaptainUpgradePopupContext
        {
            Unit = new CaptainUnitPopupInfo
            {
                Name = request.UnitName,
                Cost = request.UnitCost,
                Statline = request.UnitStatline,
                RangedWeapons = request.UnitRangedWeapons,
                CcWeapons = request.UnitCcWeapons,
                Skills = request.UnitSkills,
                Equipment = request.UnitEquipment,
                CachedLogoPath = request.UnitCachedLogoPath,
                PackagedLogoPath = request.UnitPackagedLogoPath
            },
            OptionFactionId = optionFactionId,
            OptionFactionName = ResolveOptionFactionName(request, sourceFactionId, optionFactionId),
            WeaponOptions = options.Weapons,
            SkillOptions = options.Skills,
            EquipmentOptions = options.Equipment
        };

        return await request.CaptainDialog.ShowAsync(context);
    }

    private static string ResolveOptionFactionName(
        CompanyCaptainWorkflowRequest request,
        int sourceFactionId,
        int optionFactionId)
    {
        var sourceName = sourceFactionId > 0 ? request.TryGetFactionName(sourceFactionId) : null;
        var optionName = optionFactionId > 0 ? request.TryGetFactionName(optionFactionId) : null;
        var metadataSourceName = sourceFactionId > 0 ? request.TryGetMetadataFactionName(sourceFactionId) : null;
        var metadataOptionName = optionFactionId > 0 ? request.TryGetMetadataFactionName(optionFactionId) : null;
        return CaptainPopupInputBuilder.ResolveOptionFactionName(
            sourceFactionId,
            optionFactionId,
            sourceName,
            optionName,
            metadataSourceName,
            metadataOptionName);
    }
}
