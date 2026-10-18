using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.CompanySelection.Recruitment;
using InfinityMercsApp.Services.CompanySelection.StartCompany;
using InfinityMercsApp.Services.Records;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.UnitTests.Support;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.Services.CompanySelection.Recruitment;

public sealed class CompanyRecruiterTests
{
    private const string CompanyPath = "/records/c.json";
    private const string SeasonPath = "/records/s.json";

    private readonly ICompanyFileService _companyFiles = Substitute.For<ICompanyFileService>();
    private readonly ISeasonFileService _seasonFiles = Substitute.For<ISeasonFileService>();
    private readonly SeasonFile _season = new() { Rounds = [new SeasonRound { RoundIndex = 1 }] };
    private readonly SavedCompanyFile _company = new()
    {
        CompanyName = "Blue Team",
        Entries =
        [
            new SavedCompanyEntry { EntryIndex = 0, Name = "Captain", IsLieutenant = true, SourceFactionId = 101, Cost = 20 },
            new SavedCompanyEntry { EntryIndex = 3, Name = "Fusilier", SourceFactionId = 101, Cost = 10 },
            new SavedCompanyEntry { EntryIndex = 4, Name = "Peripheral", IsPeripheralUnit = true, SourceFactionId = 101 }
        ]
    };

    private SavedCompanyFile? _saved;

    public CompanyRecruiterTests()
    {
        _companyFiles.LoadAsync(CompanyPath).Returns(_company);
        _companyFiles.SaveAsync(CompanyPath, Arg.Do<SavedCompanyFile>(company => _saved = company)).Returns(Task.CompletedTask);
        _seasonFiles.UpdateAsync(SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(call =>
        {
            call.Arg<Action<SeasonFile>>()(_season);
            return true;
        });
    }

    private static MercsCompanyEntry Recruit(string name, int cost) => new()
    {
        Name = name,
        BaseUnitName = name,
        Subtitle = "MOV 10-10 | CC 13 | BS 12 | PH 10 | WIP 12 | ARM 1 | BTS 0 | VITA 1 | S 2",
        CostValue = cost,
        SourceFactionId = Army101Data.PanOceania,
        SourceUnitId = Army101Data.UnitNamed("FUSILIERS").Id
    };

    private Task<RecruitmentOutcome> HireAsync(params IEnumerable<MercsCompanyEntry> recruits) =>
        new CompanyRecruiter(new SavedEntryWriter(Army101Data.Create()), _companyFiles, _seasonFiles).HireAsync(
            new CompanyRecruitment(CompanyPath, SeasonPath, _company, Credits: 50, RetiredEntries: []),
            recruits.ToList());

    [Fact]
    public async Task RecruitsJoinAfterTheLastTrooper()
    {
        var outcome = await HireAsync(Recruit("Alpha", 10), Recruit("Bravo", 12));

        Assert.Equal(RecruitmentOutcome.Hired, outcome);
        Assert.NotNull(_saved);
        var hired = _saved.Entries.Skip(3).ToList();
        Assert.Equal([5, 6], hired.Select(entry => entry.EntryIndex));
        Assert.Equal(["Alpha", "Bravo"], hired.Select(entry => entry.Name));
        Assert.Equal([10, 12], hired.Select(entry => entry.Cost));
        Assert.All(hired, entry => Assert.False(entry.IsLieutenant));
    }

    [Fact]
    public async Task ATagsSpecOpsBuysAreSavedWithIt()
    {
        var tag = new MercsCompanyEntry
        {
            Name = "Squalo",
            CostValue = 40,
            SourceFactionId = Army101Data.PanOceania,
            SpecOpsPurchases = ["Spec-Ops: +1 ARM", "Spec-Ops: Courage"]
        };

        await HireAsync(tag);

        Assert.Equal(["Spec-Ops: +1 ARM", "Spec-Ops: Courage"], _saved?.Entries[^1].SpecOpsPurchases);
    }

    // A Drummer's Drumbot is a REM in the army data, so it keeps STR rather than being a typeless peripheral.
    [Fact]
    public async Task APeripheralIsSavedWithItsOwnTroopType()
    {
        var drummer = new MercsCompanyEntry
        {
            Name = "Drummer",
            CostValue = 20,
            SourceFactionId = Army101Data.PanOceania,
            SourceUnitId = Army101Data.UnitNamed("DRUMMERS, Mobile Support Section").Id,
            HasPeripheralStatBlock = true,
            PeripheralNameHeading = "DRUMBOT"
        };

        await HireAsync(drummer);

        Assert.Equal("REM", _saved?.Entries.Single(entry => entry.IsPeripheralUnit && entry.Name.Contains("Drumbot", StringComparison.OrdinalIgnoreCase)).UnitTypeCode);
    }

    [Fact]
    public async Task TheirCostIsChargedThroughAdditionalRecruitment()
    {
        await HireAsync(Recruit("Alpha", 10));

        var charge = Assert.Single(_season.Rounds[0].Marketplace.Transactions);
        Assert.Equal(ServiceStoreNames.AdditionalRecruitment, charge.OriginStore);
        Assert.Equal("Recruit: Alpha", charge.ItemName);
        Assert.Equal(10, charge.CostCr);
    }

    [Fact]
    public async Task BeforeTheFirstContractTheyAreInitialPurchases()
    {
        _season.Rounds.Clear();

        await HireAsync(Recruit("Alpha", 10));

        Assert.Single(_season.InitialPurchases.Transactions);
    }

    [Fact]
    public async Task NobodyToHireChangesNothing()
    {
        Assert.Equal(RecruitmentOutcome.NothingToHire, await HireAsync());

        await _companyFiles.DidNotReceiveWithAnyArgs().SaveAsync(Arg.Any<string>(), Arg.Any<SavedCompanyFile>());
    }

    [Fact]
    public async Task WhenTheSeasonIsNotSavedTheCompanyIsPutBack()
    {
        _seasonFiles.UpdateAsync(SeasonPath, Arg.Any<Action<SeasonFile>>()).Returns(false);

        var outcome = await HireAsync(Recruit("Alpha", 10));

        Assert.Equal(RecruitmentOutcome.SeasonNotSaved, outcome);
        await _companyFiles.Received(2).SaveAsync(CompanyPath, Arg.Any<SavedCompanyFile>());
        Assert.NotNull(_saved);
        Assert.Equal([0, 3, 4], _saved.Entries.Select(entry => entry.EntryIndex));
    }

    [Fact]
    public async Task WhenTheCompanyIsNotSavedNothingIsCharged()
    {
        _companyFiles.SaveAsync(CompanyPath, Arg.Any<SavedCompanyFile>()).Returns(Task.FromException(new IOException("disk full")));

        var outcome = await HireAsync(Recruit("Alpha", 10));

        Assert.Equal(RecruitmentOutcome.CompanyNotSaved, outcome);
        await _seasonFiles.DidNotReceiveWithAnyArgs().UpdateAsync(Arg.Any<string?>(), Arg.Any<Action<SeasonFile>>());
    }
}
