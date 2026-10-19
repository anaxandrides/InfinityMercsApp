using System.Text.Json;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Domain.Season.Injuries;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Presentation.Season;
using InfinityMercsApp.Presentation.Season.Game;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.Services.Stores;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Season.Game;
using InfinityMercsApp.ViewModels.Season.Game.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Game;

public sealed class GameModeViewModelTests : IDisposable
{
    private const string SeasonPath = "/seasons/s.json";

    private readonly TempRecords _records = new();
    private readonly INavigationService _navigation = Substitute.For<INavigationService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly IAppSettingsProvider _settings = Substitute.For<IAppSettingsProvider>();
    private readonly MissionDebrief _debrief = new();

    public GameModeViewModelTests() => _settings.GetShowUnitsInInches().Returns(true);

    public void Dispose() => _records.Dispose();

    // A lieutenant (entry 0), a regular trooper with Tactical Awareness skills, and an irregular impetuous one.
    private async Task<GameModeViewModel> OpenAsync(bool elite = false, ContractInducements? inducements = null)
    {
        var entries = new List<object>
        {
            new { entryIndex = 0, name = "Lt", isLieutenant = true, sourceFactionId = 101 },
            new { entryIndex = 1, name = "Bolt", sourceFactionId = 101 },
            new Dictionary<string, object>
            {
                ["entryIndex"] = 2,
                ["name"] = "Zero",
                ["sourceFactionId"] = 101,
                ["Custom Characteristics"] = new List<string> { "Irregular", "Impetuous" }
            }
        };
        var companyPath = _records.WriteCompany("c.json", JsonSerializer.Serialize(new { entries }));

        var viewModel = new GameModeViewModel(
            _navigation, _dialogs, _seasonFiles, new DeploymentRoster(Army101Data.Create(), new CompanyFileStore(), _seasonFiles, new StoreCatalog(Substitute.For<IStoreProvider>())), _debrief, _settings);
        var query = NavigationParameters.ForCompany(companyPath, SeasonPath);
        query[NavigationParameters.EliteDeployment] = elite;
        if (inducements is not null)
        {
            query[NavigationParameters.Inducements] = inducements;
        }

        viewModel.ApplyQueryAttributes(query);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static OrderPoolItem Pool(GameModeViewModel viewModel, OrderKind kind) => viewModel.Orders.Single(pool => pool.Kind == kind);

    [Fact]
    public async Task InitializeAsyncCommand_Deployment_OpensOnRoundOneWithOrders()
    {
        var viewModel = await OpenAsync();

        Assert.Equal(["Lt", "Bolt", "Zero"], viewModel.Units.Select(unit => unit.Name));
        Assert.Equal("Round 1", viewModel.RoundText);
        Assert.False(viewModel.CanEndGame);
        Assert.True(viewModel.HasNextRound);
        Assert.Equal(
            [1, 1, 1, 3],
            viewModel.Orders.Select(pool => pool.Available));
        Assert.True(viewModel.ShowUnitsInInches);
    }

    [Fact]
    public async Task HasTacticalToken_EliteDeployment_OnlyThen()
    {
        Assert.All((await OpenAsync()).Units, unit => Assert.False(unit.HasTacticalToken));

        var elite = await OpenAsync(elite: true);

        Assert.Equal([false, true, true], elite.Units.Select(unit => unit.HasTacticalToken));
    }

    [Fact]
    public async Task ToggleUnitCommand_SecondRow_ClosesTheFirst()
    {
        var viewModel = await OpenAsync();

        viewModel.ToggleUnitCommand.Execute(viewModel.Units[0]);
        viewModel.ToggleUnitCommand.Execute(viewModel.Units[1]);

        Assert.Equal([false, true, false], viewModel.Units.Select(unit => unit.IsExpanded));
        Assert.True(viewModel.Units[1].IsExpanded);

        viewModel.ToggleUnitCommand.Execute(viewModel.Units[1]);
        Assert.All(viewModel.Units, unit => Assert.False(unit.IsExpanded));
    }

    [Fact]
    public async Task TakeWoundCommand_Wounded_ChangesIconAndNextRoundsOrders()
    {
        var viewModel = await OpenAsync(elite: true);
        var bolt = viewModel.Units[1];

        bolt.TakeWoundCommand.Execute(null);

        Assert.Equal(WoundState.KnockedOut, bolt.WoundState);
        Assert.EndsWith("noun-knocked-out-face.svg", bolt.WoundIconPath);

        viewModel.NextRoundCommand.Execute(null);

        Assert.Equal("Round 2", viewModel.RoundText);
        Assert.Equal(2, Pool(viewModel, OrderKind.Regular).Available);
        Assert.True(bolt.IsTacticalTokenSpent);

        bolt.HealWoundCommand.Execute(null);
        Assert.Equal(WoundState.Healthy, bolt.WoundState);
    }

    [Fact]
    public async Task NextRoundCommand_OrdersAndTokensSpent_RefillsThem()
    {
        var viewModel = await OpenAsync(elite: true);
        Pool(viewModel, OrderKind.Regular).UseCommand.Execute(null);
        viewModel.Units[1].ToggleTacticalTokenCommand.Execute(null);

        viewModel.NextRoundCommand.Execute(null);

        Assert.Equal((3, 0), (Pool(viewModel, OrderKind.Regular).Available, Pool(viewModel, OrderKind.Regular).Used));
        Assert.False(viewModel.Units[1].IsTacticalTokenSpent);
    }

    [Fact]
    public async Task EndGameCommand_Rounds_CanEndFromThreeAndLastsFour()
    {
        var viewModel = await OpenAsync();

        viewModel.NextRoundCommand.Execute(null);
        Assert.False(viewModel.EndGameCommand.CanExecute(null));
        viewModel.NextRoundCommand.Execute(null);
        Assert.True(viewModel.CanEndGame);
        Assert.True(viewModel.EndGameCommand.CanExecute(null));
        viewModel.NextRoundCommand.Execute(null);

        Assert.Equal(GameModeViewModel.LastRound, viewModel.CurrentRound);
        Assert.False(viewModel.HasNextRound);
        Assert.False(viewModel.NextRoundCommand.CanExecute(null));
    }

    [Fact]
    public async Task ShowXpCommand_BoxesTicked_UpdatesTheButton()
    {
        var viewModel = await OpenAsync();
        var bolt = viewModel.Units[1];
        Assert.Equal("XP", bolt.XpButtonText);

        viewModel.ShowXpCommand.Execute(bolt);
        Assert.True(viewModel.IsShowingXp);
        var sheet = Assert.IsType<XpSheet>(viewModel.OpenXpSheet);
        sheet.Assist[0].IsChecked = true;

        Assert.Equal("XP (1)", bolt.XpButtonText);
        Assert.Equal("Total XP Earned: 1", sheet.TotalText);

        viewModel.CloseXpCommand.Execute(null);
        Assert.False(viewModel.IsShowingXp);
        Assert.Equal("XP (1)", bolt.XpButtonText);
    }

    [Fact]
    public async Task TakeAndHealWoundCommands_Limits_StopAtNoneAndTheMost()
    {
        var viewModel = await OpenAsync();
        var bolt = viewModel.Units[1];

        bolt.HealWoundCommand.Execute(null);
        Assert.Equal(0, bolt.Unit.WoundsReceived);

        for (var i = 0; i < bolt.Unit.MaxWounds + 3; i++)
        {
            bolt.TakeWoundCommand.Execute(null);
        }

        Assert.Equal(bolt.Unit.MaxWounds, bolt.Unit.WoundsReceived);
        Assert.False(bolt.IsConscious);
    }

    [Fact]
    public async Task ShowWeaponCommand_UnknownWeapon_DoesNotOpen()
    {
        var viewModel = await OpenAsync();
        var known = new GameWeaponCard("Combi Rifle", [new GameWeaponMode(new(), WeaponModeModifiers.None, null, true)], false);

        viewModel.ShowWeaponCommand.Execute(GameWeaponCard.Unmatched("Zap Gun"));
        Assert.False(viewModel.IsShowingWeapon);

        viewModel.ShowWeaponCommand.Execute(known);
        Assert.Same(known, viewModel.OpenWeaponCard);

        viewModel.CloseWeaponCommand.Execute(null);
        Assert.False(viewModel.IsShowingWeapon);
    }

    [Fact]
    public async Task EndGameCommand_NotConfirmed_StaysInTheGame()
    {
        var viewModel = await OpenAsync();
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        await viewModel.EndGameCommand.ExecuteAsync(null);

        await _navigation.DidNotReceiveWithAnyArgs().PushAsync(Arg.Any<string>(), Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task EndGameCommand_Confirmed_RecordsTheMissionAndGoesToTheOutcome()
    {
        var season = new SeasonFile();
        season.Rounds.Add(new SeasonRound { RoundIndex = 2 });
        _seasonFiles.LoadAsync(SeasonPath).Returns(season);
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var viewModel = await OpenAsync(elite: true);
        viewModel.NextRoundCommand.Execute(null);
        viewModel.NextRoundCommand.Execute(null);

        await viewModel.EndGameCommand.ExecuteAsync(null);

        var round = season.Rounds[^1];
        Assert.Equal(3, round.RoundIndex);
        Assert.Equal((3, true, 3), (round.MissionResults.UnitsDeployed, round.MissionResults.EliteDeploymentMet, round.MissionResults.MissionRound));
        await _seasonFiles.Received(1).SaveAsync(SeasonPath, season);
        Assert.Equal(["Lt", "Bolt", "Zero"], _debrief.Units.Select(unit => unit.Name));
        Assert.All(_debrief.Units, unit => Assert.Equal(3, unit.DeploymentXp));
        Assert.Empty(_debrief.PendingInjuries);
        await _navigation.Received(1).PushAsync("MissionOutcomePage", Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task EndGameCommand_DeadTrooper_GoesToInjuryChecks()
    {
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var viewModel = await OpenAsync();
        var bolt = viewModel.Units[1];
        bolt.TakeWoundCommand.Execute(null);
        bolt.TakeWoundCommand.Execute(null);
        viewModel.NextRoundCommand.Execute(null);
        viewModel.NextRoundCommand.Execute(null);

        await viewModel.EndGameCommand.ExecuteAsync(null);

        Assert.Equal(WoundState.Dead, bolt.WoundState);
        Assert.Equal("Bolt", Assert.Single(_debrief.PendingInjuries).Name);
        Assert.False(_debrief.Units.Single(unit => unit.Name == "Bolt").IsConsciousAtEnd);
        await _navigation.Received(1).PushAsync("InjuriesPage", Arg.Any<IDictionary<string, object>>());
    }

    [Fact]
    public async Task EndGameCommand_InjuredTrooperRested_GoesToTheInjuriesPage()
    {
        var season = new SeasonFile();
        season.TrooperFor(9).Injuries.Add(Injury.Arms);
        _seasonFiles.LoadAsync(SeasonPath).Returns(season);
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var viewModel = await OpenAsync();
        viewModel.NextRoundCommand.Execute(null);
        viewModel.NextRoundCommand.Execute(null);

        await viewModel.EndGameCommand.ExecuteAsync(null);

        Assert.Empty(_debrief.PendingInjuries);
        await _navigation.Received(1).PushAsync("InjuriesPage", Arg.Any<IDictionary<string, object>>());
    }

    // An Authorized Bounty Hunter (PanOceania 10049, the SMG loadout) hired for each index given.
    private static ContractInducements Hiring(params IEnumerable<int> entryIndices) => new()
    {
        Received = 100,
        Hired = entryIndices
            .Select(index => new HiredTroop
            {
                EntryIndex = index, Offer = "Authorized Bounty Hunter", FactionId = Army101Data.PanOceania, UnitId = 10049, GroupId = 1, OptionId = 5, Cost = 10
            })
            .ToList()
    };

    [Fact]
    public async Task InitializeAsyncCommand_HiredTroop_PlaysAfterTheCompany()
    {
        var viewModel = await OpenAsync(inducements: Hiring(-1));

        var hired = viewModel.Units[^1];
        Assert.Equal((true, "HIRED", "Authorized Bounty Hunter"), (hired.Unit.IsHired, hired.HiredRole, hired.Name));
        Assert.Contains("Submachine Gun", hired.Unit.RangedWeapons);
        Assert.Equal(4, viewModel.Orders.Single(pool => pool.Kind == OrderKind.Regular).Available);
    }

    // Three company troopers and four hires make seven: the last hire gives no order.
    [Fact]
    public async Task InitializeAsyncCommand_SeventhTrooperHired_GivesNoOrder()
    {
        var viewModel = await OpenAsync(inducements: Hiring(-1, -2, -3, -4));

        Assert.Equal([false, false, false, true], viewModel.Units.Where(unit => unit.Unit.IsHired).Select(unit => unit.IsOverTrooperLimit));
        Assert.Equal("HIRED · NO ORDER", viewModel.Units[^1].HiredRole);
        Assert.Equal(6, viewModel.Orders.Single(pool => pool.Kind == OrderKind.Regular).Available);
    }

    [Fact]
    public async Task HasTacticalToken_EliteDeploymentWithAHire_OnlyTheCompanysTroopers()
    {
        var viewModel = await OpenAsync(elite: true, inducements: Hiring(-1));

        Assert.True(viewModel.Units[1].HasTacticalToken);
        Assert.False(viewModel.Units[^1].HasTacticalToken);
    }

    [Fact]
    public async Task InitializeAsyncCommand_RentedGear_IsCarriedThisGame()
    {
        var inducements = new ContractInducements
        {
            Received = 20,
            CommandTokens = 1,
            Rentals = [new RentedGear { ItemName = "Spitfire", Slot = "Primary", EntryIndex = 1, Cost = 10 }]
        };

        var viewModel = await OpenAsync(inducements: inducements);

        Assert.Contains("Spitfire", viewModel.Units[1].Unit.RangedWeapons);
        Assert.Equal((1, true), (viewModel.CommandTokens.Available, viewModel.CommandTokens.HasTokens));
    }

    [Fact]
    public async Task EndGameCommand_HiredTroop_LeavesWithTheContract()
    {
        var season = new SeasonFile();
        _seasonFiles.LoadAsync(SeasonPath).Returns(season);
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var inducements = Hiring(-1);
        var viewModel = await OpenAsync(inducements: inducements);
        var hired = viewModel.Units[^1];
        hired.TakeWoundCommand.Execute(null);
        hired.TakeWoundCommand.Execute(null);
        viewModel.NextRoundCommand.Execute(null);
        viewModel.NextRoundCommand.Execute(null);

        await viewModel.EndGameCommand.ExecuteAsync(null);

        var result = season.Rounds[^1].MissionResults;
        Assert.Equal(3, result.UnitsDeployed);
        Assert.Same(inducements, result.Inducements);
        Assert.Equal(["Lt", "Bolt", "Zero"], _debrief.Units.Select(unit => unit.Name));
        Assert.Empty(_debrief.PendingInjuries);
    }

    [Fact]
    public void UseCommand_CommandTokens_SpendAndComeBack()
    {
        var tokens = new CommandTokenPool();
        tokens.Reset(2);

        tokens.UseCommand.Execute(null);
        tokens.UseCommand.Execute(null);

        Assert.False(tokens.UseCommand.CanExecute(null));
        tokens.RestoreCommand.Execute(null);
        Assert.Equal((1, 1), (tokens.Available, tokens.Used));
    }
}
