using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using TemplateCaptain = InfinityMercsApp.Views.Common.Captain;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.Services.CompanySelection.SpecOps;

internal static class CompanySpecOpsWorkflowService
{
    private static readonly IReadOnlyList<string> DisallowedTagSpecOpsOptions =
    [
        "Forward Deployment",
        "Strategic Deployment",
        "Infiltration",
        "Impersonation",
        "Parachutist",
        "Combat Drop",
        "Engineer"
    ];

    public static async Task<SavedImprovedCaptainStats?> ShowTagSpecOpsConfigurationAsync(
        CompanySpecOpsWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var sourceFactionId = ResolveSourceFactionIdForTagWorkflow(request);
        var candidateFactionIds = BuildFactionCandidateChain(sourceFactionId, request.TryGetParentFactionId).ToList();
        var inferredVanillaFactionId = InferVanillaFactionIdFromReinforcement(sourceFactionId);
        if (inferredVanillaFactionId.HasValue && !candidateFactionIds.Contains(inferredVanillaFactionId.Value))
        {
            candidateFactionIds.Add(inferredVanillaFactionId.Value);
        }
        var mergedWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mergedSkills = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var mergedEquipment = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var optionFactionId = sourceFactionId;

        foreach (var candidateFactionId in candidateFactionIds)
        {
            var candidateOptions = CaptainPopupInputBuilder.LoadUpgradeOptions(
                request.ArmyDataService,
                request.SpecOpsProvider,
                candidateFactionId,
                request.ShowUnitsInInches,
                cancellationToken);

            if (!candidateOptions.IsEmpty &&
                mergedWeapons.Count == 0 &&
                mergedSkills.Count == 0 &&
                mergedEquipment.Count == 0)
            {
                optionFactionId = candidateFactionId;
            }

            foreach (var weapon in candidateOptions.Weapons)
            {
                mergedWeapons.Add(weapon);
            }

            foreach (var skill in candidateOptions.Skills)
            {
                mergedSkills.Add(skill);
            }

            foreach (var equipment in candidateOptions.Equipment)
            {
                mergedEquipment.Add(equipment);
            }
        }

        var options = FilterDisallowedTagSpecOpsOptions(new CaptainUpgradeOptionSet
        {
            Weapons = mergedWeapons.ToList(),
            Skills = mergedSkills.ToList(),
            Equipment = mergedEquipment.ToList()
        });

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
            EquipmentOptions = options.Equipment,
            PopupTitle = request.IsLieutenant ? "TAG Lieutenant Spec Ops" : "TAG Spec Ops",
            ConfirmButtonText = "Finalize Tag",
            DefaultUnitCustomName = request.IsLieutenant ? "Lieutenant" : "TAG",
            BaseExperienceOverride = Math.Max(0, request.BaseExperience)
        };

        return await TemplateCaptain.ConfigureCaptainPopupPage.ShowAsync(request.Navigation, context);
    }

    private static int ResolveNonTagSourceFactionId(int resolvedSourceFactionId, int? firstSourceFactionId)
    {
        if (resolvedSourceFactionId > 0 && resolvedSourceFactionId != TagGen.TagCompanyFactionId)
        {
            return resolvedSourceFactionId;
        }

        if (firstSourceFactionId.GetValueOrDefault() > 0 &&
            firstSourceFactionId.GetValueOrDefault() != TagGen.TagCompanyFactionId)
        {
            return firstSourceFactionId.GetValueOrDefault();
        }

        return resolvedSourceFactionId;
    }

    private static int ResolveSourceFactionIdForTagWorkflow(CompanySpecOpsWorkflowRequest request)
    {
        if (request.PreferredOtherFactionId.GetValueOrDefault() > 0 &&
            request.PreferredOtherFactionId.GetValueOrDefault() != TagGen.TagCompanyFactionId)
        {
            return request.PreferredOtherFactionId.GetValueOrDefault();
        }

        var rawSourceFactionId = CaptainPopupInputBuilder.ResolveSourceFactionId(
            request.FallbackSourceFactionId,
            request.FirstSourceFactionId);
        return ResolveNonTagSourceFactionId(
            rawSourceFactionId,
            request.FirstSourceFactionId);
    }

    private static IReadOnlyList<int> BuildFactionCandidateChain(
        int sourceFactionId,
        Func<int, int?> tryGetParentFactionId)
    {
        var candidates = new List<int>();
        var seen = new HashSet<int>();
        var current = sourceFactionId;
        var guard = 0;

        while (current > 0 && seen.Add(current) && guard++ < 16)
        {
            candidates.Add(current);
            var parent = tryGetParentFactionId(current);
            if (!parent.HasValue || parent.Value <= 0 || parent.Value == current)
            {
                break;
            }

            current = parent.Value;
        }

        return candidates;
    }

    private static int? InferVanillaFactionIdFromReinforcement(int factionId)
    {
        if (factionId <= 0)
        {
            return null;
        }

        // Reinforcement factions are encoded as x99; map to x01 for the parent vanilla faction.
        // Examples: 199 -> 101 (PanO), 299 -> 201 (Yu Jing), 1099 -> 1001 (O-12).
        if (factionId % 100 != 99)
        {
            return null;
        }

        var vanillaFactionId = (factionId / 100) * 100 + 1;
        return vanillaFactionId > 0 ? vanillaFactionId : null;
    }

    private static CaptainUpgradeOptionSet FilterDisallowedTagSpecOpsOptions(
        CaptainUpgradeOptionSet source)
    {
        static bool IsDisallowed(string optionLabel)
        {
            if (string.IsNullOrWhiteSpace(optionLabel))
            {
                return false;
            }

            foreach (var disallowed in DisallowedTagSpecOpsOptions)
            {
                if (optionLabel.Contains(disallowed, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        return new CaptainUpgradeOptionSet
        {
            Weapons = source.Weapons.Where(option => !IsDisallowed(option)).ToList(),
            Skills = source.Skills.Where(option => !IsDisallowed(option)).ToList(),
            Equipment = source.Equipment.Where(option => !IsDisallowed(option)).ToList()
        };
    }

    private static string ResolveOptionFactionName(
        CompanySpecOpsWorkflowRequest request,
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
