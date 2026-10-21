using InfinityMercsApp.Services.Dialogs;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class CompanyRosterViewModelTests
{
    private readonly IDialogService _dialogs = Substitute.For<IDialogService>();

    private CompanyRosterViewModel Roster() => new(_dialogs);

    [Fact]
    public void Add_Lieutenant_GoesFirst()
    {
        var roster = Roster();
        roster.Add(new MercsCompanyEntry { Name = "Fusilier", CostValue = 10 }, servantOwner: null);

        roster.Add(new MercsCompanyEntry { Name = "Captain", CostValue = 20, IsLieutenant = true }, servantOwner: null);

        Assert.Equal(["Captain", "Fusilier"], roster.Entries.Select(entry => entry.Name));
        Assert.Equal(30, roster.Cost);
        Assert.True(roster.HasLieutenant);
    }

    [Fact]
    public void Remove_ServantOwner_TakesItsServantToo()
    {
        var roster = Roster();
        var doctor = new MercsCompanyEntry { Name = "Doctor", CanOwnServant = true };
        roster.Add(doctor, servantOwner: null);
        roster.Add(new MercsCompanyEntry { Name = "Fusilier" }, servantOwner: null);
        roster.Add(new MercsCompanyEntry { Name = "Servant", ServantOwner = doctor }, doctor);

        Assert.Equal(["Doctor", "Servant", "Fusilier"], roster.Entries.Select(entry => entry.Name));
        Assert.True(roster.Remove(doctor));

        Assert.Equal(["Fusilier"], roster.Entries.Select(entry => entry.Name));
    }

    [Fact]
    public void Remove_OnTheCompanysRoster_StaysPut()
    {
        var roster = Roster();
        roster.ShowCompany([new MercsCompanyEntry { Name = "Veteran", IsOnRoster = true }]);
        roster.Add(new MercsCompanyEntry { Name = "Recruit" }, servantOwner: null);

        Assert.False(roster.Remove(roster.Entries[0]));
        roster.RemoveRecruits();

        Assert.Equal(["Veteran"], roster.Entries.Select(entry => entry.Name));
        Assert.Empty(roster.Recruits);
    }

    [Fact]
    public void CompanyName_EnteredAfterTheError_ClearsIt()
    {
        var roster = Roster();
        roster.ShowCompanyNameValidationError = true;

        roster.CompanyName = "Blue Lions";

        Assert.False(roster.ShowCompanyNameValidationError);
    }

    [Fact]
    public async Task ChooseServantOwnerAsync_NoDoctorOrEngineer_SaysSo()
    {
        var roster = Roster();

        Assert.Null(await roster.ChooseServantOwnerAsync("Bot"));

        await _dialogs.Received(1).ShowAlertAsync("Needs a Doctor or Engineer", Arg.Any<string>());
    }

    [Fact]
    public async Task ChooseServantOwnerAsync_TwoOwnersOfOneName_NumbersThem()
    {
        var roster = Roster();
        roster.Add(new MercsCompanyEntry { Name = "Doctor", CanOwnServant = true }, servantOwner: null);
        roster.Add(new MercsCompanyEntry { Name = "Doctor", CanOwnServant = true }, servantOwner: null);
        _dialogs.ChooseAsync("Who does Bot belong to?", "Cancel", Arg.Any<IReadOnlyList<string>>()).Returns("Doctor (2)");

        var owner = await roster.ChooseServantOwnerAsync("Bot");

        Assert.Same(roster.Entries[1], owner);
    }
}
