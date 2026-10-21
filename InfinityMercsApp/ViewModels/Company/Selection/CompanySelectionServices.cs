using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.CompanySelection.Teams;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>Everything a company selection screen talks to, gathered so each kind of company passes one thing on.</summary>
public sealed record CompanySelectionServices(
    INavigationService Navigation,
    IDialogService Dialogs,
    IArmyDataService ArmyData,
    ISpecOpsProvider SpecOps,
    IAppPackageFiles PackageFiles,
    CompanyFactionCatalog FactionCatalog,
    ThemeFactions ThemeFactions,
    SelectionUnitCatalog UnitCatalog,
    UnitVisibility Visibility,
    UnitFilterOptionsCatalog FilterOptions,
    UnitDetailsLoader UnitDetails,
    CoreFireteamEligibility CoreFireteams,
    CaptainSetup Captains,
    CompanyFounder Founder,
    CompanyRecruiter Recruiter,
    IAppSettingsProvider? Settings = null,
    FactionLogoCacheService? Logos = null);
