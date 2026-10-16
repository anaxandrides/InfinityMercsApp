using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Season.Services;
using InfinityMercsApp.Domain.Season.Troopers;
using InfinityMercsApp.ViewModels.Company.Viewer;
using InfinityMercsApp.ViewModels.Season.Overview;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Season.Overview;

public sealed class SeasonServicesViewModelTests
{
    private readonly SeasonTestKit _kit = new();
    private readonly SeasonServicesViewModel _services;
    private int _changes;

    public SeasonServicesViewModelTests()
    {
        // Three rounds: 60 CR to spend.
        _kit.Season.Rounds = [new SeasonRound { RoundIndex = 1 }, new SeasonRound { RoundIndex = 2 }, new SeasonRound { RoundIndex = 3 }];
        _kit.Dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), "Buy", "Cancel").Returns(true);
        _services = new SeasonServicesViewModel(_kit.SeasonFiles, _kit.Dialogs, () =>
        {
            _changes++;
            return Task.CompletedTask;
        });
    }

    private SeasonTrooper Bolt => _kit.Season.TrooperFor(1);

    private void Show(string trooper = "Bolt")
    {
        var units = new CompanyRoster(_kit.ArmyData).UnitsOf(SeasonTestKit.Company(), _kit.Season);
        _services.SelectedTrooper = null;
        _services.Show(SeasonTestKit.SeasonPath, _kit.Season, units);
        _services.SelectedTrooper = units.Single(unit => unit.Name == trooper);
    }

    private ServiceOption Option(SeasonService service) => _services.Options.Single(option => option.Service == service);

    private SeasonTransaction LastTransaction => _kit.Season.Rounds[2].Marketplace.Transactions.Last();

    [Fact]
    public void OffersEveryService()
    {
        Show();

        Assert.Equal(["Advanced Therapy", "Advanced Therapy (all injuries)", "Anger Management (Frenzy)", "Anger Management (Impetuous)", "Forced Retraining", "Regular Training"],
            _services.Options.Select(option => option.Name));
        Assert.Equal("4 CR", Option(SeasonService.AdvancedTherapy).CostText);
        Assert.True(Option(SeasonService.AdvancedTherapy).ChoosesInjury);
        Assert.True(Option(SeasonService.ForcedRetraining).ChoosesPerks);
    }

    [Fact]
    public async Task AdvancedTherapyWritesOffTheChosenInjury()
    {
        Bolt.Injuries.AddRange([Injury.Arms, Injury.BodyInjury]);
        Show();
        _services.SelectedInjury = _services.Injuries.Single(entry => entry.Injury == Injury.BodyInjury);

        await _services.BuyCommand.ExecuteAsync(Option(SeasonService.AdvancedTherapy));

        Assert.Equal([Injury.Arms], Bolt.Injuries);
        Assert.Equal(("Medical Services", "Advanced Therapy: Bolt (Body Injury)", 4), (LastTransaction.OriginStore, LastTransaction.ItemName, LastTransaction.CostCr));
        Assert.Equal(1, _changes);
    }

    [Fact]
    public async Task FullTherapyWritesOffEveryInjury()
    {
        Bolt.Injuries.AddRange([Injury.Arms, Injury.BodyInjury]);
        Show();

        await _services.BuyCommand.ExecuteAsync(Option(SeasonService.FullTherapy));

        Assert.Empty(Bolt.Injuries);
        Assert.Equal(8, LastTransaction.CostCr);
    }

    [Fact]
    public async Task TherapyNeedsAnInjury()
    {
        Show();

        Assert.Equal("No injury to write off.", Option(SeasonService.AdvancedTherapy).UnavailableReason);
        Assert.False(Option(SeasonService.FullTherapy).IsAvailable);

        await _services.BuyCommand.ExecuteAsync(Option(SeasonService.FullTherapy));
        await _kit.Dialogs.DidNotReceiveWithAnyArgs().ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task AngerManagementCalmsTheFrenzyOfBattleFury()
    {
        Bolt.Injuries.Add(Injury.BattleFury);
        Show();
        Assert.Equal("Isn't Impetuous.", Option(SeasonService.CalmImpetuous).UnavailableReason);

        await _services.BuyCommand.ExecuteAsync(Option(SeasonService.CalmFrenzy));

        Assert.Equal(["Frenzy"], Bolt.CharacteristicsLost);
        Show();
        Assert.Equal("Doesn't have Frenzy.", Option(SeasonService.CalmFrenzy).UnavailableReason);
    }

    [Fact]
    public async Task RegularTrainingMakesAnIrregularTrooperRegular()
    {
        Bolt.CharacteristicsGained.Add("Irregular");
        Show();

        await _services.BuyCommand.ExecuteAsync(Option(SeasonService.RegularTraining));

        Assert.Equal(["Regular"], Bolt.CharacteristicsGained);
        Assert.Equal(11, LastTransaction.CostCr);
        Show();
        Assert.Equal("Already regular.", Option(SeasonService.RegularTraining).UnavailableReason);
    }

    [Fact]
    public async Task ForcedRetrainingSwapsAPerkForOneOfTheSameTier()
    {
        _kit.Season.Rounds[0].MissionResults.UnitResults.Add(new SeasonMissionUnitResult { EntryIndex = 1, UnitName = "Bolt", XpGained = 5 });
        Bolt.Perks.Add("body-track-1-tier-1");
        Show();
        Assert.Equal(["Climbing Plus"], _services.RetrainablePerks.Select(perk => perk.Name));
        Assert.All(_services.Replacements, option => Assert.Equal(1, option.Pick.Tier));
        _services.SelectedReplacement = _services.Replacements.Single(option => option.Pick.NodeId == "body-track-6-tier-1");

        await _services.BuyCommand.ExecuteAsync(Option(SeasonService.ForcedRetraining));

        Assert.Equal(["body-track-6-tier-1"], Bolt.Perks);
        Assert.Equal("Forced Retraining: Bolt (Climbing Plus → Terrain (Total))", LastTransaction.ItemName);
    }

    [Fact]
    public void ForcedRetrainingNeedsARank()
    {
        Bolt.Perks.Add("body-track-1-tier-1");
        Show();

        Assert.Equal("Needs rank 1 or above.", Option(SeasonService.ForcedRetraining).UnavailableReason);
    }

    [Fact]
    public void AServiceTheCompanyCannotAffordSaysSo()
    {
        _kit.Season.Rounds = [new SeasonRound { RoundIndex = 1 }];
        Bolt.CharacteristicsGained.Add("Irregular");
        Show();

        Assert.Equal("Needs 11 CR; 10 CR available.", Option(SeasonService.RegularTraining).UnavailableReason);
    }

    [Fact]
    public async Task CancellingBuysNothing()
    {
        Bolt.Injuries.Add(Injury.Arms);
        _kit.Dialogs.ConfirmAsync(Arg.Any<string>(), Arg.Any<string>(), "Buy", "Cancel").Returns(false);
        Show();

        await _services.BuyCommand.ExecuteAsync(Option(SeasonService.AdvancedTherapy));

        Assert.Equal([Injury.Arms], Bolt.Injuries);
        Assert.Equal(0, _changes);
    }

    [Fact]
    public async Task BuyingBeforeTheFirstContractRecordsTheInitialPurchases()
    {
        _kit.Season.Rounds.Clear();
        _kit.Season.StartingFunds = new SeasonStartingFunds { Credits = 10 };
        Bolt.Injuries.Add(Injury.Arms);
        Show();
        _services.SelectedInjury = _services.Injuries.Single();

        await _services.BuyCommand.ExecuteAsync(Option(SeasonService.AdvancedTherapy));

        Assert.Equal(4, _kit.Season.InitialPurchases.Transactions.Single().CostCr);
        Assert.Empty(Bolt.Injuries);
        Assert.Equal(1, _changes);
    }

    [Fact]
    public async Task AFailedSaveSaysSo()
    {
        Bolt.Injuries.Add(Injury.Arms);
        Show();
        _kit.SeasonFiles.UpdateAsync(SeasonTestKit.SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(false);

        await _services.BuyCommand.ExecuteAsync(Option(SeasonService.AdvancedTherapy));

        await _kit.Dialogs.Received(1).ShowAlertAsync("Nothing Bought", SeasonMessages.NotSaved);
        Assert.Equal(0, _changes);
    }

    [Fact]
    public void ShowingAgainKeepsTheTrooper()
    {
        Show("Traktor Mul");

        _services.Show(SeasonTestKit.SeasonPath, _kit.Season, new CompanyRoster(_kit.ArmyData).UnitsOf(SeasonTestKit.Company(), _kit.Season));

        Assert.Equal("Traktor Mul", _services.SelectedTrooper?.Name);
    }
}
