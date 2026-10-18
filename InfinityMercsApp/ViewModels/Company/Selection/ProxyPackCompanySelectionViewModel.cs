using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>
/// A Proxy Pack: one Regular trooper from any single sectorial or vanilla army, the Lone Wolf. It is the company's
/// captain without needing the Lieutenant skill, gains Transfer Consciousness and Tactical Awareness, and is the
/// only trooper the company ever has.
/// </summary>
public sealed class ProxyPackCompanySelectionViewModel(CompanySelectionServices services, ArmySourceSelectionMode mode)
    : StandardCompanySelectionViewModel(services, mode)
{
    public override string CompanyTypeLabel => ProxyPack.CompanyType;

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

    /// <summary>The Lone Wolf is the only trooper.</summary>
    protected override bool CanAddProfile(ArmyUnitSelectionItem unit) => MercsCompanyEntries.Count == 0;

    /// <summary>Once the Lone Wolf is picked, no other profile can be added.</summary>
    protected override int AdjustLieutenantVisualStates(int visibleProfiles)
    {
        if (MercsCompanyEntries.Count > 0)
        {
            foreach (var profile in Profiles)
            {
                profile.IsLieutenantBlocked = true;
            }
        }

        return visibleProfiles;
    }

    /// <summary>The Lone Wolf gains Transfer Consciousness and Tactical Awareness.</summary>
    protected override IReadOnlyCollection<string> CommonSkillsFor(
        ArmyUnitSelectionItem unit, ViewerProfileItem profile, IReadOnlyList<string> commonSkills) =>
        commonSkills.Concat(ProxyPack.LoneWolfSkills).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>The Lone Wolf is the captain, with or without the Lieutenant skill.</summary>
    protected override void OnEntryBuilt(MercsCompanyEntry entry) => entry.IsLieutenant = true;

    private bool IsRegular(ArmyUnitSelectionItem unit, CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken) =>
        Services.Visibility.ProfileGroupsOf(context, unit, cancellationToken) is { IsEmpty: false } groups &&
        UnitTraits.ReadOrderTraits(groups).HasRegular;
}
