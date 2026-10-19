using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Services.Season.Peripherals;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.Season;

public sealed class PeripheralTradesTests
{
    private const string CompanyPath = "/companies/c.json";
    private const string SeasonPath = "/seasons/s.json";

    private readonly ICompanyFileService _companyFiles = Substitute.For<ICompanyFileService>();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();
    private readonly IArmyDataService _armyData = Substitute.For<IArmyDataService>();
    private readonly SavedCompanyFile _company = new()
    {
        Entries =
        [
            new SavedCompanyEntry { EntryIndex = 1, Name = "Bolt" },
            new SavedCompanyEntry { EntryIndex = 7, Name = "Chaksa Peripheral A", IsPeripheralUnit = true, FromControllerAugment = true, Cost = 7 }
        ]
    };

    public PeripheralTradesTests()
    {
        _companyFiles.LoadAsync(CompanyPath).Returns(_company);
        _seasonFiles.LoadAsync(SeasonPath).Returns(new SeasonFile { StartingFunds = new SeasonStartingFunds { Credits = 20 } });
        _dialogs.ConfirmAsync("Sell Peripheral", Arg.Any<string>(), "Sell", "Cancel").Returns(true);
    }

    private PeripheralTrades Trades() => new(_companyFiles, _seasonFiles, _dialogs, _armyData);

    [Fact]
    public async Task SellAsync_SaleNotRecorded_PutsThePeripheralBack()
    {
        _seasonFiles.UpdateAsync(SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(false);

        Assert.Null(await Trades().SellAsync(CompanyPath, SeasonPath, 7));

        Assert.Contains(_company.Entries, entry => entry.EntryIndex == 7);
        await _companyFiles.Received(2).SaveAsync(CompanyPath, _company);
        await _dialogs.Received(1).ShowAlertAsync("Peripheral Not Sold", SeasonMessages.NotSaved);
    }

    [Fact]
    public async Task SellAsync_Declined_ChangesNothing()
    {
        _dialogs.ConfirmAsync("Sell Peripheral", Arg.Any<string>(), "Sell", "Cancel").Returns(false);

        Assert.Null(await Trades().SellAsync(CompanyPath, SeasonPath, 7));

        await _companyFiles.DidNotReceiveWithAnyArgs().SaveAsync(Arg.Any<string>(), Arg.Any<SavedCompanyFile>());
    }

    [Fact]
    public async Task ChangeAsync_PurchaseNotRecorded_TakesTheNewPeripheralOut()
    {
        TohaaArmyData.AddTo(_armyData);
        _dialogs.ChooseAsync("Peripheral for Bolt", "Cancel", Arg.Any<IReadOnlyList<string>>())
            .Returns("Chaksa Peripheral B: Combi Rifle (Gorgos Squad), 4 CR");
        _seasonFiles.UpdateAsync(SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(false);

        Assert.Null(await Trades().ChangeAsync(CompanyPath, SeasonPath, new PeripheralChange([], 1, "Bolt")));

        Assert.Equal([1, 7], _company.Entries.Select(entry => entry.EntryIndex));
        await _dialogs.Received(1).ShowAlertAsync("Peripheral Not Bought", SeasonMessages.NotSaved);
    }

    [Fact]
    public async Task ChangeAsync_UnreadableCompany_SaysSo()
    {
        _companyFiles.LoadAsync(CompanyPath).Returns<SavedCompanyFile?>(_ => throw new IOException("locked"));

        Assert.Null(await Trades().ChangeAsync(CompanyPath, SeasonPath, new PeripheralChange([], 1, "Bolt")));

        await _dialogs.Received(1).ShowAlertAsync("Company Not Read", Arg.Any<string>());
    }
}
