using InfinityMercsApp.Services;
using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.ViewModels;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels;

public sealed class ArmoryViewModelTests
{
    private readonly IMetadataProvider _metadata = Substitute.For<IMetadataProvider>();

    private static Weapon W(string name, string? mode = null) => new() { Name = name, Mode = mode };

    private async Task<ArmoryViewModel> OpenAsync()
    {
        var viewModel = new ArmoryViewModel(Substitute.For<INavigationService>(), _metadata);
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);
        return viewModel;
    }

    [Fact]
    public void GroupByName_MergesModesOfTheSameWeapon()
    {
        var groups = ArmoryViewModel.GroupByName(
            [W("Rifle", "Burst"), W("adhesive launcher"), W("rifle ", "Anti-materiel"), W("Rifle")]);

        Assert.Equal(["adhesive launcher", "Rifle"], groups.Select(g => g.Name));
        Assert.Equal([null, "Anti-materiel", "Burst"], groups[1].Modes.Select(m => m.Mode));
    }

    [Fact]
    public async Task OpensWithEveryWeaponAndSearchNarrowsTheList()
    {
        _metadata.GetAllWeapons().Returns([W("Rifle"), W("Pistol")]);
        _metadata.SearchWeaponsByName("pis").Returns([W("Pistol")]);
        var viewModel = await OpenAsync();

        Assert.Equal(["Pistol", "Rifle"], viewModel.WeaponGroups.Select(g => g.Name));

        viewModel.SearchText = "pis";
        Assert.Equal(["Pistol"], viewModel.WeaponGroups.Select(g => g.Name));

        viewModel.SearchText = " ";
        Assert.Equal(2, viewModel.WeaponGroups.Count);
    }

    [Fact]
    public async Task ReappearingKeepsTheList()
    {
        _metadata.GetAllWeapons().Returns([W("Rifle")]);
        var viewModel = await OpenAsync();
        await viewModel.InitializeAsyncCommand.ExecuteAsync(null);

        _metadata.Received(1).GetAllWeapons();
    }

    [Fact]
    public async Task SelectingAWeaponShowsItsDetailUntilClosed()
    {
        _metadata.GetAllWeapons().Returns([W("Rifle")]);
        var viewModel = await OpenAsync();
        Assert.Equal("Select a weapon", viewModel.SelectedWeaponName);
        Assert.True(viewModel.NoWeaponSelected);

        viewModel.SelectedWeaponGroup = viewModel.WeaponGroups[0];

        Assert.Equal("Rifle", viewModel.SelectedWeaponName);
        Assert.True(viewModel.HasSelectedWeapon);
        Assert.True(viewModel.IsShowingDetail);

        viewModel.CloseDetailCommand.Execute(null);
        Assert.False(viewModel.IsShowingDetail);
        Assert.True(viewModel.HasSelectedWeapon);
    }
}
