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
        ? "Standard Company - Vanilla"
        : "Standard Company - Sectorial";

    public override bool HasRightSlot => Mode == ArmySourceSelectionMode.Sectorials;

    public override async Task<IReadOnlyList<FactionRecord>> LoadFactionsAsync(CompanySelectionViewModel screen, CancellationToken cancellationToken) =>
        WithoutGeneratedFactions(await screen.Services.FactionCatalog.LoadAsync(Mode, cancellationToken));

    /// <summary>Leaves out the factions generated for Airborne, Inspiring and TAG companies.</summary>
    protected static IReadOnlyList<FactionRecord> WithoutGeneratedFactions(IEnumerable<FactionRecord> factions) =>
        factions
            .Where(faction => faction.Id != AirborneGen.AirborneCompanyFactionId &&
                              faction.Id != InspiringGen.InspiringCompanyFactionId &&
                              faction.Id != TagGen.TagCompanyFactionId)
            .ToList();
}
