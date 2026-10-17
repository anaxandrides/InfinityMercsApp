using InfinityMercsApp.Services.CompanySelection.Captain;
using InfinityMercsApp.Services.CompanySelection.Models.Saved;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using NSubstitute;
using TagGen = InfinityMercsApp.Infrastructure.Providers.CompanyFactions.TagCompanyFactionGenerator;
using InfinityMercsApp.ViewModels.Company.Selection.Workflows;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class TagCompanySelectionViewModelTests
{
    private readonly SelectionTestKit _kit = new();

    private Task<TagCompanySelectionViewModel> OpenAsync() =>
        SelectionTestKit.OpenAsync(new TagCompanySelectionViewModel(_kit.Services, ArmySourceSelectionMode.Sectorials));

    [Fact]
    public async Task TheTagCompanyFillsTheRightSlot()
    {
        var viewModel = await OpenAsync();

        Assert.Equal(TagCompanySelectionViewModel.TagCompanyName, viewModel.CompanyTypeLabel);
        Assert.Equal(TagGen.TagCompanyFactionId, viewModel.RightSlotFaction?.Id);
        Assert.Equal(TagCompanySelectionViewModel.TagCompanyLogoPath, viewModel.RightSlotLogoSource);
    }

    [Fact]
    public async Task FactionsAlwaysGoInTheLeftSlot()
    {
        var viewModel = await OpenAsync();

        viewModel.SelectSlotCommand.Execute(1);
        Assert.Equal(0, viewModel.ActiveSlotIndex);

        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);

        Assert.Equal(SelectionTestKit.Sectorial, viewModel.LeftSlotFaction?.Id);
        Assert.Equal(TagGen.TagCompanyFactionId, viewModel.RightSlotFaction?.Id);
    }

    [Fact]
    public void AFinalizedTagGainsItsSpecOps()
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
    public async Task BackingOutOfATagsSpecOpsRemovesIt()
    {
        _kit.CaptainDialog.ShowAsync(Arg.Any<CaptainUpgradePopupContext>()).Returns((SavedImprovedCaptainStats?)null);
        var viewModel = await OpenAsync();
        await viewModel.SelectFactionCommand.ExecuteAsync(viewModel.Factions[0]);
        var tag = viewModel.Units.FirstOrDefault(unit => unit.IsVisible && (unit.Subtitle ?? string.Empty).StartsWith("TAG", StringComparison.OrdinalIgnoreCase));
        Assert.NotNull(tag);
        await viewModel.SelectUnitFromListCommand.ExecuteAsync(tag);
        var profile = viewModel.Profiles.First(candidate => candidate.IsVisible && !candidate.IsLieutenant && !candidate.IsLieutenantBlocked);

        await viewModel.AddProfileToMercsCompanyCommand.ExecuteAsync(profile);

        await _kit.CaptainDialog.Received(1).ShowAsync(Arg.Is<CaptainUpgradePopupContext>(context => context.PopupTitle == "TAG Spec Ops"));
        Assert.Empty(viewModel.MercsCompanyEntries);
    }
}
