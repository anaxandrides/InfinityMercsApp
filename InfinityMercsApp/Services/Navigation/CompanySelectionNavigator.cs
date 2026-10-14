using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.CompanyFactions;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Views;
using InfinityMercsApp.Views.AirborneCompany;
using InfinityMercsApp.Views.CohesiveCompany;
using InfinityMercsApp.Views.InspiringCompany;
using InfinityMercsApp.Views.StandardCompany;

namespace InfinityMercsApp.Services.Navigation;

/// <summary>
/// Builds the company selection pages, which still take their dependencies directly, and pushes them
/// onto the Shell navigation stack.
/// </summary>
internal sealed class CompanySelectionNavigator : ICompanySelectionNavigator
{
    private readonly IMetadataProvider? _metadataProvider;
    private readonly IFactionProvider? _factionProvider;
    private readonly ISpecOpsProvider _specOpsProvider;
    private readonly ICohesiveCompanyFactionQueryProvider _cohesiveCompanyFactionQueryProvider;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider? _appSettingsProvider;
    private readonly IArmyDataService _armyDataService;

    public CompanySelectionNavigator(
        IMetadataProvider? metadataProvider,
        IFactionProvider? factionProvider,
        ISpecOpsProvider specOpsProvider,
        ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider,
        FactionLogoCacheService? factionLogoCacheService,
        IAppSettingsProvider? appSettingsProvider,
        IArmyDataService armyDataService)
    {
        _metadataProvider = metadataProvider;
        _factionProvider = factionProvider;
        _specOpsProvider = specOpsProvider;
        _cohesiveCompanyFactionQueryProvider = cohesiveCompanyFactionQueryProvider;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsProvider = appSettingsProvider;
        _armyDataService = armyDataService;
    }

    public Task OpenAsync(CompanyKind kind, ArmySourceSelectionMode mode = ArmySourceSelectionMode.Sectorials)
    {
        Page page = kind switch
        {
            CompanyKind.Standard => CreateStandard(mode),
            CompanyKind.Tag => CreateStandard(mode, "TAG Company"),
            CompanyKind.LoneWolf => CreateStandard(mode, "Lone Wolf Company"),
            CompanyKind.Cohesive => CreateCohesive(mode),
            CompanyKind.Inspiring => CreateInspiring(),
            CompanyKind.Airborne => CreateAirborne(),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        return Shell.Current.Navigation.PushAsync(page);
    }

    private StandardCompanySelectionPage CreateStandard(ArmySourceSelectionMode mode, string? companyTypeLabelOverride = null)
    {
        return new StandardCompanySelectionPage(
            mode,
            _metadataProvider,
            _factionProvider,
            _specOpsProvider,
            _cohesiveCompanyFactionQueryProvider,
            _factionLogoCacheService,
            _appSettingsProvider,
            _armyDataService,
            companyTypeLabelOverride: companyTypeLabelOverride);
    }

    private CohesiveCompanySelectionPage CreateCohesive(ArmySourceSelectionMode mode)
    {
        return new CohesiveCompanySelectionPage(
            mode,
            _metadataProvider,
            _factionProvider,
            _specOpsProvider,
            _cohesiveCompanyFactionQueryProvider,
            _factionLogoCacheService,
            _appSettingsProvider,
            _armyDataService);
    }

    private InspiringCompanySelectionPage CreateInspiring()
    {
        return new InspiringCompanySelectionPage(
            _metadataProvider,
            _factionProvider,
            _specOpsProvider,
            _cohesiveCompanyFactionQueryProvider,
            _factionLogoCacheService,
            _appSettingsProvider,
            _armyDataService);
    }

    private AirborneCompanySelectionPage CreateAirborne()
    {
        return new AirborneCompanySelectionPage(
            _metadataProvider,
            _factionProvider,
            _specOpsProvider,
            _cohesiveCompanyFactionQueryProvider,
            _factionLogoCacheService,
            _appSettingsProvider,
            _armyDataService);
    }
}

