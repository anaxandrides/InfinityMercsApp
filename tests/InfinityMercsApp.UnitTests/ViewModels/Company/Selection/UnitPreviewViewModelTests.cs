using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services.ArmyData;
using InfinityMercsApp.Services.CompanySelection.Factions;
using InfinityMercsApp.Services.CompanySelection.Units;
using InfinityMercsApp.Services.Navigation;
using InfinityMercsApp.ViewModels.Company.Selection;
using InfinityMercsApp.ViewModels.Company.Selection.Items;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Company.Selection;

public sealed class UnitPreviewViewModelTests
{
    private readonly UnitPreviewViewModel _preview;

    public UnitPreviewViewModelTests()
    {
        var armyData = Substitute.For<IArmyDataService>();
        _preview = new UnitPreviewViewModel(
            new UnitDetailsLoader(armyData, Substitute.For<ISpecOpsProvider>()), new ThemeFactions(armyData), ArmySourceSelectionMode.Sectorials);
    }

    private static ArmyUnitSelectionItem Unit(int id) => new() { Id = id, Name = $"Unit {id}" };

    [Fact]
    public void Select_AnotherUnit_MovesTheSelectionAndAsksForItsDetails()
    {
        var first = Unit(1);
        var second = Unit(2);
        _preview.Select(first, restrictToFto: false);

        Assert.True(_preview.Select(second, restrictToFto: false));

        Assert.False(first.IsSelected);
        Assert.True(second.IsSelected);
        Assert.Same(second, _preview.SelectedUnit);
    }

    [Fact]
    public void Select_SameUnit_AsksForDetailsOnlyWhenTheRestrictionChanges()
    {
        var unit = Unit(1);
        _preview.Select(unit, restrictToFto: false);

        Assert.False(_preview.Select(unit, restrictToFto: false));
        Assert.True(_preview.Select(unit, restrictToFto: true));
        Assert.True(_preview.RestrictsToFto);
    }

    [Fact]
    public void Clear_UnitSelected_EmptiesTheDetail()
    {
        var unit = Unit(1);
        _preview.Select(unit, restrictToFto: false);
        _preview.Detail.UnitNameHeading = unit.Name;

        _preview.Clear();

        Assert.Null(_preview.SelectedUnit);
        Assert.False(unit.IsSelected);
        Assert.Equal(CompanyUnitDetail.NoUnitHeading, _preview.Detail.UnitNameHeading);
        Assert.Equal(UnitPreviewViewModel.NoUnitStatus, _preview.ProfilesStatus);
    }

    [Fact]
    public void Load_NothingSelected_HasNoProfiles()
    {
        Assert.False(_preview.Load(new UnitPreviewRequest(false, [], 0)));
        Assert.Empty(_preview.Profiles);
    }
}
