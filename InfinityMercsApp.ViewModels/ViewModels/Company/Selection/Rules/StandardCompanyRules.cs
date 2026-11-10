using InfinityMercsApp.Domain.Company;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.Navigation;
using AirborneGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.AirborneCompanyFactionGenerator;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using InspiringGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.InspiringCompanyFactionGenerator;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.ViewModels.Company.Selection.Rules;

/// <summary>A Standard company: units from one vanilla faction, or from two sectorials.</summary>
public class StandardCompanyRules(ArmySourceSelectionMode mode) : CompanyRules(mode)
{
    public override string Heading => Mode == ArmySourceSelectionMode.VanillaFactions ? "Choose your faction:" : "Choose your sectorials";

    public override string CompanyTypeLabel => Mode == ArmySourceSelectionMode.VanillaFactions
        ? CompanyTypes.StandardVanilla
        : CompanyTypes.StandardSectorial;

    public override bool HasRightSlot => Mode == ArmySourceSelectionMode.Sectorials;

    public override bool OffersSpecOps => true;

    public override async Task<IReadOnlyList<FactionRecord>> LoadFactionsAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken) =>
        WithoutGeneratedFactions(await screen.Services.FactionCatalog.LoadAsync(Mode, cancellationToken));

    /// <summary>
    /// Every sectorial and vanilla army but the Non-Aligned Armies, in the Infinity Army's order, with their logos
    /// cached: for companies of any single army (a Proxy Pack, a Team-Ops Company).
    /// </summary>
    protected static async Task<IReadOnlyList<FactionRecord>> EveryArmyAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken)
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

    /// <summary>Leaves out the factions generated for Airborne, Inspiring and TAG companies.</summary>
    protected static IReadOnlyList<FactionRecord> WithoutGeneratedFactions(IEnumerable<FactionRecord> factions) =>
        factions
            .Where(faction => faction.Id != AirborneGen.AirborneCompanyFactionId &&
                              faction.Id != InspiringGen.InspiringCompanyFactionId &&
                              faction.Id != TagGen.TagCompanyFactionId)
            .ToList();
}
