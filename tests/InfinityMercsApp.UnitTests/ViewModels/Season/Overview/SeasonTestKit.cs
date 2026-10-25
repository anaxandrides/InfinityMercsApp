using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Rolls;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Providers.Metadata;
using InfinityMercsApp.Presentation.Company.Viewer;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Packaging;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Rolls;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.Services.Wiki;
using InfinityMercsApp.UnitTests.Presentation.Company.Viewer;
using InfinityMercsApp.ViewModels.Components;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;
using InfinityMercsApp.Services.Links;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

/// <summary>
/// Substitutes for everything the season page uses, a small company, the Base Market, two War Markets and a
/// season file whose latest round records what the page writes to it.
/// </summary>
internal sealed class SeasonTestKit
{
    public const string CompanyPath = "/companies/blue.json";
    public const string SeasonPath = "/seasons/blue.json";

    public INavigationService Navigation { get; } = Substitute.For<INavigationService>();
    public ICompanyFileService CompanyFiles { get; } = Substitute.For<ICompanyFileService>();
    public ISeasonFileService SeasonFiles { get; } = Substitute.For<ISeasonFileService>();
    public IStoreProvider Stores { get; } = Substitute.For<IStoreProvider>();
    public IArmyDataService ArmyData { get; } = Substitute.For<IArmyDataService>();
    public IMetadataProvider Metadata { get; } = Substitute.For<IMetadataProvider>();
    public IDialogService Dialogs { get; } = Substitute.For<IDialogService>();
    public ICompanySelectionNavigator CompanySelection { get; } = Substitute.For<ICompanySelectionNavigator>();
    public IDice Dice { get; } = Substitute.For<IDice>();
    public IAppPackageFiles PackageFiles { get; } = Substitute.For<IAppPackageFiles>();
    public IAppSettingsProvider Settings { get; } = Substitute.For<IAppSettingsProvider>();

    /// <summary>The Booty and Metachemistry roll prompt, over the kit's dialogs, dice and army data.</summary>
    public RecruitRollPrompt Rolls() => new(Dialogs, Dice, ArmyData);

    /// <summary>The season file the page reads; its latest round takes the page's updates.</summary>
    public SeasonFile Season { get; set; } = new() { Rounds = [new SeasonRound { RoundIndex = 1 }] };

    public static Store BaseMarket { get; } = new()
    {
        Name = "Base Market",
        Alignment = StoreAlignments.Neutral,
        Items = [new StoreItem { Name = "Knife", Category = "Sidearm", CostCr = 2 }]
    };

    public static Store NumberOne { get; } = new()
    {
        Name = "Number One",
        AssociatedFactions = ["PanOceania"],
        Alignment = StoreAlignments.Lawful,
        Items =
        [
            new StoreItem { Name = "Combi Rifle", Category = "Primary", CostCr = 12 },
            new StoreItem { Name = "Pistol", Category = "Sidearm", CostCr = 3 },
            new StoreItem { Name = "SWC +1", Category = "Exchange", CostCr = 20 },
            new StoreItem { Name = "Spitfire", Category = "Primary", CostCr = 20, CostSwc = 1 }
        ],
        TroopTypes = [new StoreTroopType { ArmorName = "Light Armour", Type = "LI", CostCr = 10 }],
        Augments = [new StoreAugment { Name = "WIP=12", CostCr = 15 }, new StoreAugment { Name = "Courage", CostCr = 6 }]
    };

    public static Store JadeTemu { get; } = new()
    {
        Name = "Jade Temu",
        AssociatedFactions = ["Yu Jing"],
        Alignment = StoreAlignments.Chaotic,
        Items = [new StoreItem { Name = "Monofilament Blade", Category = "Secondary", CostCr = 8 }]
    };

    public static Store Medical { get; } = new() { Name = ServiceStoreNames.MedicalServices, Items = [new StoreItem { Name = "Surgery", CostCr = 5 }] };

    public static Store Recruitment { get; } = new() { Name = ServiceStoreNames.AdditionalRecruitment };

    public SeasonTestKit()
    {
        SeasonFiles.LoadAsync(SeasonPath).Returns(_ => Season);
        SeasonFiles.SaveAsync(SeasonPath, Arg.Any<SeasonFile?>()).Returns(true);
        SeasonFiles.UpdateLatestRoundAsync(SeasonPath, Arg.Any<Action<SeasonRound>>()).Returns(call =>
        {
            if (Season.Rounds.OrderByDescending(round => round.RoundIndex).FirstOrDefault() is not { } latest)
            {
                return false;
            }

            call.Arg<Action<SeasonRound>>()(latest);
            return true;
        });
        SeasonFiles.UpdateAsync(SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(call =>
        {
            call.Arg<Action<SeasonFile>>()(Season);
            return true;
        });
        SeasonFiles.CreateAsync(Arg.Any<string>(), Arg.Any<string>(), CompanyPath).Returns(SeasonPath);

        Stores.GetAvailableStoresAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns([Recruitment, BaseMarket, NumberOne, Medical]);
        Stores.GetAllStoreNames().Returns(["Base Market", "Number One", "Jade Temu", "Arachne Req", ServiceStoreNames.MedicalServices]);
        foreach (var store in new List<Store> { BaseMarket, NumberOne, JadeTemu, Medical, Recruitment })
        {
            Stores.GetStoreByNameAsync(store.Name, Arg.Any<CancellationToken>()).Returns(store);
        }

        Settings.GetShowUnitsInInches().Returns(true);
        CompanyFiles.Exists(CompanyPath).Returns(true);
        CompanyFiles.LoadAsync(CompanyPath).Returns(Company());
    }

    /// <summary>A lieutenant, a trooper with a pistol, and a peripheral.</summary>
    public static SavedCompanyFile Company() => new()
    {
        CompanyName = "Blue Team",
        CompanyType = "Standard Company",
        CompanyIdentifier = "blue-1",
        SourceFactions = [new SavedCompanyFaction { FactionId = 101 }],
        Entries =
        [
            CompanyRosterTests.Entry(1, "Bolt", weapons: ["Combi Rifle"]),
            CompanyRosterTests.Entry(2, "Fusilier", isLieutenant: true, skills: ["Lieutenant"]),
            CompanyRosterTests.Entry(3, "Traktor Mul", isPeripheral: true, characteristics: ["Peripheral (Servant)"])
        ]
    };

    public MarketplaceDetailViewModel Popup() => new(Metadata, Substitute.For<IWikiDescriptionService>(), Substitute.For<ILinkLauncher>());

    public IArmyDataLoader ArmyLoader { get; } = Substitute.For<IArmyDataLoader>();

    public SeasonViewModel SeasonViewModel() => new(
        Navigation, CompanyFiles, SeasonFiles, Stores, new StoreCatalog(Stores), ArmyData, Dialogs, CompanySelection, Dice, PackageFiles,
        new CompanyRoster(ArmyData), new UnitDetailViewModel(ArmyData, Settings), Popup(),
        new PeripheralTrades(CompanyFiles, SeasonFiles, Dialogs, ArmyData),
        ArmyLoader);

    /// <summary>A season page opened on the company with its season file, as the page would be on appearing.</summary>
    public async Task<SeasonViewModel> OpenAsync(bool withSeasonFile = true)
    {
        var viewModel = SeasonViewModel();
        viewModel.ApplyQueryAttributes(withSeasonFile
            ? NavigationParameters.ForCompany(CompanyPath, SeasonPath)
            : NavigationParameters.ForCompany(CompanyPath));
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    public static SeasonTransaction Bought(string store, string item) => new() { OriginStore = store, ItemName = item };
}
