using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.ViewModels.Company.Selection;

/// <summary>Everything a company selection screen talks to, gathered so each kind of company passes one thing on.</summary>
public sealed record CompanySelectionServices(
    INavigationService Navigation,
    IDialogService Dialogs,
    ICaptainConfigurationDialog CaptainDialog,
    IArmyDataService ArmyData,
    ISpecOpsProvider SpecOps,
    IAppPackageFiles PackageFiles,
    ICompanyFileService CompanyFiles,
    ISeasonFileService SeasonFiles,
    RecordPaths Paths,
    CompanyFactionCatalog FactionCatalog,
    UnitDetailsLoader UnitDetails,
    CaptainSetup Captains,
    CompanyFounder Founder,
    CompanyRecruiter Recruiter,
    IAppSettingsProvider? Settings = null,
    FactionLogoCacheService? Logos = null);
