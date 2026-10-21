using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Domain.Models.Army.Traits;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using InfinityMercsApp.ViewModels.UnitBrowser.Items;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;

namespace InfinityMercsApp.ViewModels.Company.Selection.Rules;

/// <summary>
/// A Proxy Pack: one Regular trooper from any single sectorial or vanilla army, the Lone Wolf. It is the company's
/// captain without needing the Lieutenant skill, gains Transfer Consciousness and Tactical Awareness, and is the
/// only trooper the company ever has.
/// </summary>
public sealed class ProxyPackCompanyRules(ArmySourceSelectionMode mode) : StandardCompanyRules(mode)
{
    public override string CompanyTypeLabel => ProxyPack.CompanyType;

    public override bool AllowsLieutenantFilter => false;

    public override bool AllowsTeamsFilter => false;

    /// <summary>Every faction but the Non-Aligned Armies, in the Infinity Army's order, with their logos cached.</summary>
    public override async Task<IReadOnlyList<FactionRecord>> LoadFactionsAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken)
    {
        var factions = screen.Services.ArmyData
            .GetMetadataFactions(includeDiscontinued: true, cancellationToken)
            .Where(faction => !CompanyFactionCatalog.IsNonAlignedArmyName(faction.Name))
            .ToList();
        if (screen.Services.Logos is { } logos)
        {
            await logos.CacheFactionLogosFromRecordsAsync(factions, cancellationToken);
        }

        return WithoutGeneratedFactions(CompanyFactionCatalog.CollapseContractedBackUpVariants(factions).OrderBy(faction => faction.Id));
    }

    /// <summary>Only regular troopers show, and an empty list stays empty.</summary>
    public override void AdjustUnitVisibility(
        CompanySelectionViewModel screen, CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken)
    {
        foreach (var unit in screen.UnitList.Units.Where(unit => unit.IsVisible && !IsRegular(screen, unit, context, cancellationToken)))
        {
            unit.IsVisible = false;
        }
    }

    /// <summary>The Lone Wolf is the only trooper.</summary>
    public override bool CanAddProfile(CompanySelectionViewModel screen, ArmyUnitSelectionItem unit) => screen.Roster.Entries.Count == 0;

    /// <summary>Once the Lone Wolf is picked, no other profile can be added.</summary>
    public override int AdjustProfileAvailability(CompanySelectionViewModel screen, int visibleProfiles)
    {
        if (screen.Roster.Entries.Count > 0)
        {
            screen.Preview.BlockAll();
        }

        return visibleProfiles;
    }

    /// <summary>The Lone Wolf gains Transfer Consciousness and Tactical Awareness.</summary>
    public override IReadOnlyCollection<string> CommonSkillsFor(
        ArmyUnitSelectionItem unit, ViewerProfileItem profile, IReadOnlyList<string> commonSkills) =>
        commonSkills.Concat(ProxyPack.LoneWolfSkills).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    /// <summary>The Lone Wolf is the captain, with or without the Lieutenant skill.</summary>
    public override void OnEntryBuilt(CompanySelectionViewModel screen, MercsCompanyEntry entry)
    {
        entry.IsLieutenant = true;
        entry.IsLoneWolf = true;
    }

    private static bool IsRegular(
        CompanySelectionViewModel screen, ArmyUnitSelectionItem unit, CompanyUnitVisibilityLookupContext context, CancellationToken cancellationToken) =>
        screen.Services.Visibility.ProfileGroupsOf(context, unit, cancellationToken) is { IsEmpty: false } groups &&
        UnitTraits.ReadOrderTraits(groups).HasRegular;
}
