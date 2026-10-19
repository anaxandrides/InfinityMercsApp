using System.Text.Json;
using InfinityMercsApp.Domain.Models.Army.Stats;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Season.Game;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Presentation.Season;
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

public sealed class LoneWolfGameModeTests : IDisposable
{
    private const string SeasonPath = "/seasons/s.json";

    private readonly TempRecords _records = new();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly MissionDebrief _debrief = new();

    public void Dispose() => _records.Dispose();

    // A Proxy Pack: the Lone Wolf (entry 0) and its second Aspect (entry 1), both with VITA 2.
    private async Task<GameModeViewModel> OpenAsync(int? loneWolfStart = null, HashSet<int>? deployed = null)
    {
        var entries = new List<Dictionary<string, object>>
        {
            new()
            {
                ["entryIndex"] = 0, ["name"] = "Fusilier", ["customName"] = "Kestrel", ["isLieutenant"] = true, ["isLoneWolf"] = true,
                ["sourceFactionId"] = 101, ["Current VITA/STR"] = "2", ["Current WIP"] = "13"
            },
            new()
            {
                ["entryIndex"] = 1, ["name"] = "Fusilier", ["customName"] = "Kestrel (Aspect 2)", ["aspectOf"] = 0,
                ["sourceFactionId"] = 101, ["Current VITA/STR"] = "2", ["Current WIP"] = "13"
            }
        };
        var companyPath = _records.WriteCompany("c.json", JsonSerializer.Serialize(new { companyType = "Proxy Pack", entries }));
        var seasonFiles = Substitute.For<ISeasonFileService>();
        var viewModel = new GameModeViewModel(
            Substitute.For<INavigationService>(), _dialogs, seasonFiles,
            new DeploymentRoster(Army101Data.Create(), new CompanyFileStore(), seasonFiles, new StoreCatalog(Substitute.For<IStoreProvider>())),
            _debrief, Substitute.For<IAppSettingsProvider>());
        var query = NavigationParameters.ForCompany(companyPath, SeasonPath);
        query[NavigationParameters.EliteDeployment] = true;
        if (loneWolfStart is { } start)
        {
            query[NavigationParameters.LoneWolfStart] = start;
        }

        if (deployed is not null)
        {
            query[NavigationParameters.DeployedIndices] = deployed;
        }

        viewModel.ApplyQueryAttributes(query);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    private static GameUnitItem Row(GameModeViewModel viewModel, int entryIndex) =>
        viewModel.Units.Single(unit => unit.Unit.EntryIndex == entryIndex);

    [Fact]
    public async Task InitializeAsyncCommand_ProxyPack_AspectShowsTheAspectOfTheWolf()
    {
        var viewModel = await OpenAsync();
        var wolf = Row(viewModel, 0);
        var aspect = Row(viewModel, 1);

        Assert.Equal(("LONE WOLF", "ASPECT"), (wolf.LoneWolfRole, aspect.LoneWolfRole));
        Assert.Equal(AspectOfTheWolf.Statline("2"), aspect.Statline);
        Assert.Equal(AspectOfTheWolf.Skills, aspect.SkillItems);
        Assert.Empty(aspect.WeaponCards);
        Assert.True(aspect.CanReceiveLoneWolf);
        Assert.False(wolf.CanReceiveLoneWolf);
        Assert.Contains(wolf.Statline, stat => stat == new StatValue(Stat.Wip, "13"));
    }

    [Fact]
    public async Task InitializeAsyncCommand_StartPicked_PutsTheLoneWolfInThatLoadout()
    {
        var viewModel = await OpenAsync(loneWolfStart: 1);

        Assert.True(Row(viewModel, 1).HostsLoneWolf);
        Assert.True(Row(viewModel, 0).ShowsAspect);
    }

    [Fact]
    public async Task TransferConsciousnessCommand_StandingAspect_MovesTheLoneWolfThere()
    {
        var viewModel = await OpenAsync();
        var wolf = Row(viewModel, 0);
        var aspect = Row(viewModel, 1);

        viewModel.TransferConsciousnessCommand.Execute(aspect);

        Assert.True(aspect.HostsLoneWolf);
        Assert.False(wolf.HostsLoneWolf);
        Assert.True(wolf.ShowsAspect);
        Assert.True(aspect.OrderSource.IsLieutenant);
        Assert.False(wolf.OrderSource.IsLieutenant);
    }

    [Fact]
    public async Task TransferConsciousnessCommand_AspectDown_ChangesNothing()
    {
        var viewModel = await OpenAsync();
        var aspect = Row(viewModel, 1);
        aspect.TakeWoundCommand.Execute(null);
        aspect.TakeWoundCommand.Execute(null);

        viewModel.TransferConsciousnessCommand.Execute(aspect);

        Assert.True(Row(viewModel, 0).HostsLoneWolf);
    }

    [Fact]
    public async Task EndGameCommand_LoneWolfDiedInAnAspect_OnlyItChecksForInjury()
    {
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var viewModel = await OpenAsync();
        var wolf = Row(viewModel, 0);
        var aspect = Row(viewModel, 1);
        viewModel.TransferConsciousnessCommand.Execute(aspect);
        for (var wound = 0; wound < 3; wound++)
        {
            wolf.TakeWoundCommand.Execute(null);
            aspect.TakeWoundCommand.Execute(null);
        }

        viewModel.NextRoundCommand.Execute(null);
        viewModel.NextRoundCommand.Execute(null);
        await viewModel.EndGameCommand.ExecuteAsync(null);

        var check = Assert.Single(_debrief.PendingInjuries);
        Assert.Equal((0, "Kestrel"), (check.EntryIndex, check.Name));
        Assert.Equal(0, _debrief.Units.Single(unit => unit.EntryIndex == 1).AspectOf);
    }

    [Fact]
    public async Task EndGameCommand_OwnLoadoutLeftHome_ChecksUnderTheLoneWolfsName()
    {
        _dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var viewModel = await OpenAsync(loneWolfStart: 1, deployed: [1]);
        var aspect = Row(viewModel, 1);
        for (var wound = 0; wound < 3; wound++)
        {
            aspect.TakeWoundCommand.Execute(null);
        }

        viewModel.NextRoundCommand.Execute(null);
        viewModel.NextRoundCommand.Execute(null);
        await viewModel.EndGameCommand.ExecuteAsync(null);

        var check = Assert.Single(_debrief.PendingInjuries);
        Assert.Equal((0, "Kestrel"), (check.EntryIndex, check.Name));
    }
}
