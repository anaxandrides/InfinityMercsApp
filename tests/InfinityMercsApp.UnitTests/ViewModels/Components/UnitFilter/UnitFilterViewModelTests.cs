using System.Windows.Input;
using InfinityMercsApp.Domain.Models.Filtering;
using InfinityMercsApp.ViewModels.Components.UnitFilter;
using NSubstitute;

namespace InfinityMercsApp.UnitTests.ViewModels.Components.UnitFilter;

public sealed class UnitFilterViewModelTests
{
    private readonly ICommand _apply = Substitute.For<ICommand>();
    private readonly ICommand _close = Substitute.For<ICommand>();

    private static UnitFilterPopupOptions Options() => new()
    {
        Classification = ["Line Troops", "Elite Troops", " line troops ", ""],
        Skills = ["Mimetism", "Climbing Plus", "Courage"],
        MinPoints = 10,
        MaxPoints = 40
    };

    private UnitFilterViewModel Open(UnitFilterCriteria? existing = null, bool lieutenantOnly = false) =>
        new(new UnitFilterRequest(Options(), existing ?? UnitFilterCriteria.None, lieutenantOnly), _apply, _close);

    private static FilterCriterionItem Criterion(UnitFilterViewModel filter, UnitFilterField field) =>
        filter.CriteriaItems.Single(item => item.Field == field);

    private static SelectableFilterOption Option(UnitFilterViewModel filter, string value) =>
        filter.ActiveCriterionOptions.Single(option => option.Value == value);

    [Fact]
    public void Constructor_Options_OpensOnTheFirstCriterionSorted()
    {
        var filter = Open();

        Assert.Same(filter.CriteriaItems[0], filter.SelectedCriterion);
        Assert.True(filter.CriteriaItems[0].IsCurrent);
        Assert.Equal("Classification", filter.SelectedCriterionTitle);
        Assert.Equal(["Elite Troops", "Line Troops"], filter.ActiveCriterionOptions.Select(option => option.Value));
        Assert.Equal(("10", "40"), (filter.SelectedMinPoints, filter.SelectedMaxPoints));
        Assert.Equal(31, filter.PointsOptions.Count);
    }

    [Fact]
    public void Constructor_ExistingFilter_ShowsIt()
    {
        var existing = new UnitFilterCriteria
        {
            Terms = [new UnitFilterTerm(UnitFilterField.Skills, ["Mimetism", "Courage"], UnitFilterMatchMode.All)],
            MinPoints = 0,
            MaxPoints = 25
        };

        var filter = Open(existing, lieutenantOnly: true);

        var skills = Criterion(filter, UnitFilterField.Skills);
        Assert.True(skills.HasSelections);
        Assert.Equal("All: Courage, Mimetism", skills.Summary);
        Assert.Equal("Any", Criterion(filter, UnitFilterField.Weapons).Summary);
        Assert.Equal(("10", "25"), (filter.SelectedMinPoints, filter.SelectedMaxPoints));
        Assert.True(filter.SelectedLieutenantOnlyUnits);

        filter.SelectCriterionCommand.Execute(skills);

        Assert.Equal(UnitFilterViewModel.AllMode, filter.SelectedActiveMatchMode);
        Assert.Equal(["Courage", "Mimetism"], filter.ActiveCriterionOptions.Where(o => o.IsSelected).Select(o => o.Value));
    }

    [Fact]
    public void ToggleOptionCommand_Value_CommitsItAtOnce()
    {
        var filter = Open();
        var classification = filter.CriteriaItems[0];

        filter.ToggleOptionCommand.Execute(Option(filter, "Line Troops"));

        Assert.True(classification.HasSelections);
        Assert.Equal("Any: Line Troops", classification.Summary);

        filter.SelectedActiveMatchMode = UnitFilterViewModel.AllMode;

        Assert.Equal("All: Line Troops", classification.Summary);
    }

    [Fact]
    public void SelectCriterionCommand_Switched_KeepsTheChoices()
    {
        var filter = Open();
        Option(filter, "Elite Troops").IsSelected = true;

        filter.SelectCriterionCommand.Execute(Criterion(filter, UnitFilterField.Skills));

        Assert.False(filter.CriteriaItems[0].IsCurrent);
        Assert.Equal("Any: Elite Troops", filter.CriteriaItems[0].Summary);
        Assert.Equal(["Climbing Plus", "Courage", "Mimetism"], filter.ActiveCriterionOptions.Select(option => option.Value));
        Assert.Equal(UnitFilterViewModel.AnyMode, filter.SelectedActiveMatchMode);
    }

    [Fact]
    public void ClearCriterionCommand_Executed_EmptiesTheOpenCriterion()
    {
        var filter = Open();
        filter.ToggleOptionCommand.Execute(Option(filter, "Line Troops"));

        filter.ClearCriterionCommand.Execute(null);

        Assert.False(filter.CriteriaItems[0].HasSelections);
        Assert.All(filter.ActiveCriterionOptions, option => Assert.False(option.IsSelected));
    }

    [Fact]
    public void ResetCommand_Executed_ClearsEverything()
    {
        var existing = new UnitFilterCriteria
        {
            Terms = [new UnitFilterTerm(UnitFilterField.Classification, ["Line Troops"], UnitFilterMatchMode.All)],
            MaxPoints = 20,
            TeamsView = true
        };
        var filter = Open(existing, lieutenantOnly: true);

        filter.ResetCommand.Execute(null);

        Assert.All(filter.CriteriaItems, item => Assert.False(item.HasSelections));
        Assert.All(filter.ActiveCriterionOptions, option => Assert.False(option.IsSelected));
        Assert.Equal(UnitFilterViewModel.AnyMode, filter.SelectedActiveMatchMode);
        Assert.Equal(("10", "40"), (filter.SelectedMinPoints, filter.SelectedMaxPoints));
        Assert.False(filter.SelectedLieutenantOnlyUnits);
        Assert.Equal(UnitFilterCriteria.None.Terms, filter.BuildCriteria().Terms);
    }

    [Fact]
    public void ApplyCommand_Executed_HandsOverTheFilterThenCloses()
    {
        var filter = Open();
        filter.ToggleOptionCommand.Execute(Option(filter, "Line Troops"));
        filter.SelectCriterionCommand.Execute(Criterion(filter, UnitFilterField.Skills));
        Option(filter, "Mimetism").IsSelected = true;
        filter.SelectedMinPoints = "30";
        filter.SelectedMaxPoints = "15";
        filter.SelectedTeamsView = true;
        UnitFilterCriteria? applied = null;
        _apply.Execute(Arg.Do<object?>(criteria => applied = criteria as UnitFilterCriteria));

        filter.ApplyCommand.Execute(null);

        Assert.NotNull(applied);
        Assert.Equal(
            [new UnitFilterTerm(UnitFilterField.Classification, ["Line Troops"]), new UnitFilterTerm(UnitFilterField.Skills, ["Mimetism"])],
            applied.Terms,
            (left, right) => left.Field == right.Field && left.Values.SequenceEqual(right.Values) && left.MatchMode == right.MatchMode);
        Assert.Equal((15, 30), (applied.MinPoints, applied.MaxPoints));
        Assert.True(applied.TeamsView);
        Received.InOrder(() =>
        {
            _apply.Execute(Arg.Any<object?>());
            _close.Execute(null);
        });
    }

    [Fact]
    public void CloseCommand_Executed_ClosesWithoutApplying()
    {
        var filter = Open();

        filter.CloseCommand.Execute(null);

        _close.Received(1).Execute(null);
        _apply.DidNotReceive().Execute(Arg.Any<object?>());
    }
}
