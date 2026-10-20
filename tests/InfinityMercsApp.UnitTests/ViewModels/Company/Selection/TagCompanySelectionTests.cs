using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Rules;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using NSubstitute;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class TagCompanySelectionTests
{
    private readonly SelectionTestKit _kit = new();

    private Task<CompanySelectionViewModel> OpenAsync() =>
        SelectionTestKit.OpenAsync(new CompanySelectionViewModel(_kit.Services, new TagCompanyRules(ArmySourceSelectionMode.Sectorials)));

    [Fact]
    public async Task InitializeAsync_TagCompany_FillsTheRightSlot()
    {
        var viewModel = await OpenAsync();

        Assert.Equal(TagCompanyRules.TagCompanyName, viewModel.Rules.CompanyTypeLabel);
        Assert.Equal(TagGen.TagCompanyFactionId, viewModel.Slots.Right?.Id);
        Assert.Equal(TagCompanyRules.TagCompanyLogoPath, viewModel.Slots.RightLogoSource);
    }

    [Fact]
    public async Task SelectFactionCommand_RightSlotTapped_GoesInTheLeftSlot()
    {
        var viewModel = await OpenAsync();

        viewModel.SelectSlotCommand.Execute(1);
        Assert.Equal(0, viewModel.Slots.ActiveIndex);

        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);

        Assert.Equal(SelectionTestKit.Sectorial, viewModel.Slots.Left?.Id);
        Assert.Equal(TagGen.TagCompanyFactionId, viewModel.Slots.Right?.Id);
    }

    [Fact]
    public void TagFinalizedEntryFrom_SpecOpsChosen_GainsThem()
    {
        var entry = new InfinityMercsApp.ViewModels.Company.Selection.Items.MercsCompanyEntry
        {
            Name = "Squalo",
            Subtitle = "MOV 15-10 | CC 16 | BS 13 | PH 18 | WIP 13 | ARM 7 | BTS 6 | STR 3 | S 7",
            SavedRangedWeapons = "Spitfire",
            SavedCcWeapons = "-",
            SavedSkills = "-",
            SavedEquipment = "-"
        };
        var stats = new SavedImprovedCaptainStats
        {
            IsEnabled = true,
            CaptainName = " Brutus ",
            ArmBonus = 1,
            WeaponChoice1 = "Heavy Machine Gun",
            WeaponChoice2 = "CC Weapon",
            SkillChoice1 = "Courage"
        };

        var finalized = TagFinalizedEntry.From(entry, stats);

        Assert.Equal("Brutus", finalized.Name);
        Assert.Equal(TagFinalizedEntry.SpecOpsExperience, finalized.ExperiencePoints);
        Assert.Equal(string.Join(Environment.NewLine, "Spitfire", "Heavy Machine Gun"), finalized.SavedRangedWeapons);
        Assert.Equal("CC Weapon", finalized.SavedCcWeapons);
        Assert.Equal("Courage", finalized.SavedSkills);
        Assert.Contains("ARM 8", finalized.Subtitle);
        Assert.Contains("Spec-Ops: +1 ARM", finalized.SpecOpsPurchases);
        Assert.Contains("Spec-Ops: Courage", finalized.SpecOpsPurchases);
    }

    [Fact]
    public async Task AddProfileToMercsCompanyCommand_TagSpecOpsCancelled_RemovesTheTag()
    {
        _kit.CaptainDialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>()).Returns((SavedImprovedCaptainStats?)null);
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Slots.Factions[0]);
        var tag = viewModel.UnitList.Units.FirstOrDefault(unit => unit.IsVisible && (unit.Subtitle ?? string.Empty).StartsWith("TAG", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(tag);
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(tag);
        var profile = viewModel.Preview.Profiles.First(candidate => candidate.IsVisible && !candidate.IsLieutenant && !candidate.IsLieutenantBlocked);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        await _kit.CaptainDialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context => context.PopupTitle == "TAG Spec Ops"));
        Assert.Empty(viewModel.Roster.Entries);
    }
}
