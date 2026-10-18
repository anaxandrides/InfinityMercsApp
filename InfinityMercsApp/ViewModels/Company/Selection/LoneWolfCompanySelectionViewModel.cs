using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// A Lone Wolf company: regular troopers from any faction, vanilla or sectorial, with no lieutenant-only
/// filter or fireteams.
/// </summary>
public sealed class LoneWolfCompanySelectionViewModel(CompanySelectionServices services, ArmySourceSelectionMode mode)
    : StandardCompanySelectionViewModel(services, mode)
{
    public override string CompanyTypeLabel => "Lone Wolf Company";

    protected override bool AllowsLieutenantFilter => false;

    protected override bool AllowsTeamsFilter => false;

    /// <summary>Every faction but the Non-Aligned Armies, with their logos cached.</summary>
    protected override async Task<IReadOnlyList<FactionRecord>> LoadFactionRecordsAsync(CancellationToken cancellationToken)
    {
        var factions = Services.ArmyData
            .GetMetadataFactions(includeDiscontinued: true, cancellationToken)
            .Where(faction => !CompanyFactionCatalog.IsNonAlignedArmyName(faction.Name))
            .ToList();
        if (Services.Logos is { } logos)
        {
            await logos.CacheFactionLogosFromRecordsAsync(factions, cancellationToken);
        }

        return WithoutGeneratedFactions(CompanyFactionCatalog.CollapseContractedBackUpVariants(factions).OrderBy(faction => faction.Name));
    }

    /// <summary>Only regular troopers show, and an empty list stays empty.</summary>
    protected override void AdjustUnitVisibility(CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken)
    {
        foreach (var unit in Units.Where(unit => unit.IsVisible && !IsRegular(unit, context, cancellationToken)))
        {
            unit.IsVisible = false;
        }
    }

    private bool IsRegular(ArmyUnitSelectionItem unit, CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken)
    {
        var profileGroups = Services.ArmyData.GetUnit(unit.SourceFactionId, unit.Id, cancellationToken)?.ProfileGroups;
        if (context.SpecopsByFactionId.TryGetValue(unit.SourceFactionId, out var specOpsById) &&
            specOpsById.TryGetValue(unit.Id, out var specOps) &&
            (unit.IsSpecOps || profileGroups is not { IsEmpty: false }))
        {
            profileGroups = specOps.ProfileGroups;
        }

        return profileGroups is { IsEmpty: false } groups && UnitTraits.ReadOrderTraits(groups).HasRegular;
    }
}
