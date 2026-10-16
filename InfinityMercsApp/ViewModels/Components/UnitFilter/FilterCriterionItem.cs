using CommunityToolkit.Mvvm.ComponentModel;

namespace InfinityMercsApp.ViewModels.Components.UnitFilter;

/// <summary>A criterion chip of the unit filter, e.g. Skills, with a summary of its chosen values.</summary>
public sealed partial class FilterCriterionItem(UnitFilterField field, string title) : ObservableObject
{
    public UnitFilterField Field { get; } = field;

    public string Title { get; } = title;

    /// <summary>"Any", or the match mode and chosen values, e.g. "All: Climbing Plus, Mimetism".</summary>
    [ObservableProperty]
    private string _summary = "Any";

    /// <summary>The criterion whose value chips are currently shown in the editor.</summary>
    [ObservableProperty]
    private bool _isCurrent;

    /// <summary>True when this criterion has committed (active) values; drives the chip's active marker.</summary>
    [ObservableProperty]
    private bool _hasSelections;
}
