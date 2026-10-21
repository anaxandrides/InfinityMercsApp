using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.ViewModels.Components.UnitFilter;

/// <summary>A criterion chip of the unit filter, e.g. Skills, with a summary of its chosen values.</summary>
public sealed partial class FilterCriterionItem(UnitFilterField field, string title) : ObservableObject
{
    public UnitFilterField Field { get; } = field;

    public string Title { get; } = title;

    /// <summary>"Any", or the match mode and chosen values, e.g. "All: Climbing Plus, Mimetism".</summary>
    [ObservableProperty]
    public partial string Summary { get; set; } = "Any";

    /// <summary>The criterion whose value chips are currently shown in the editor.</summary>
    [ObservableProperty]
    public partial bool IsCurrent { get; set; }

    /// <summary>True when this criterion has committed (active) values; drives the chip's active marker.</summary>
    [ObservableProperty]
    public partial bool HasSelections { get; set; }
}
