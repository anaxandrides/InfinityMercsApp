using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace InfinityMercsApp.ViewModels.Components.UnitFilter;

/// <summary>
/// The unit filter popup: criterion chips, the open criterion's value chips and match mode, the points
/// range and the toggles. Choices are kept per criterion as they are made; Apply hands the whole filter
/// to the screen that opened it.
/// </summary>
public sealed partial class UnitFilterViewModel : ObservableObject
{
    public const string AnyMode = "Any";
    public const string AllMode = "All";

    private readonly ICommand _apply;
    private readonly ICommand _close;
    private readonly IReadOnlyDictionary<UnitFilterField, IReadOnlyList<string>> _availableValues;
    private readonly Dictionary<UnitFilterField, UnitFilterTerm> _committedTerms = [];
    private readonly int _minPoints;
    private readonly int _maxPoints;
    private bool _isLoadingEditor;

    /// <param name="request">The values to choose from and the filter already applied.</param>
    /// <param name="apply">Runs with the chosen <see cref="UnitFilterCriteria"/>.</param>
    /// <param name="close">Runs when the popup should close, after applying or going back.</param>
    public UnitFilterViewModel(UnitFilterRequest request, ICommand apply, ICommand close)
    {
        Request = request;
        _apply = apply;
        _close = close;

        var options = request.Options;
        _availableValues = new Dictionary<UnitFilterField, IReadOnlyList<string>>
        {
            [UnitFilterField.Classification] = DistinctSorted(options.Classification),
            [UnitFilterField.Characteristics] = DistinctSorted(options.Characteristics),
            [UnitFilterField.Skills] = DistinctSorted(options.Skills),
            [UnitFilterField.Equipment] = DistinctSorted(options.Equipment),
            [UnitFilterField.Weapons] = DistinctSorted(options.Weapons),
            [UnitFilterField.Ammo] = DistinctSorted(options.Ammo)
        };

        _minPoints = Math.Max(0, options.MinPoints);
        _maxPoints = Math.Max(_minPoints, options.MaxPoints);
        PointsOptions = Enumerable.Range(_minPoints, _maxPoints - _minPoints + 1)
            .Select(points => points.ToString(CultureInfo.InvariantCulture))
            .ToList();

        var existing = request.ExistingCriteria;
        foreach (var term in existing.ToQuery().Terms)
        {
            _committedTerms[term.Field] = term;
        }

        UpdateAllCriterionSummaries();
        SelectedMinPoints = ClampedPoints(existing.MinPoints ?? _minPoints);
        SelectedMaxPoints = ClampedPoints(existing.MaxPoints ?? _maxPoints);
        SelectedLieutenantOnlyUnits = request.LieutenantOnlyUnits;
        SelectedTeamsView = request.TeamsView;
        SelectedCriterion = CriteriaItems[0];
    }

    public UnitFilterRequest Request { get; }

    public IReadOnlyList<FilterCriterionItem> CriteriaItems { get; } =
    [
        new(UnitFilterField.Classification, "Classification"),
        new(UnitFilterField.Characteristics, "Characteristics"),
        new(UnitFilterField.Skills, "Skills"),
        new(UnitFilterField.Equipment, "Equipment"),
        new(UnitFilterField.Weapons, "Weapons"),
        new(UnitFilterField.Ammo, "Ammo")
    ];

    /// <summary>The open criterion's values, each selected or not.</summary>
    public ObservableCollection<SelectableFilterOption> ActiveCriterionOptions { get; } = [];

    public IReadOnlyList<string> PointsOptions { get; }

    public IReadOnlyList<string> MatchModeOptions { get; } = [AnyMode, AllMode];

    public bool LieutenantOnlyUnitsEnabled => Request.LieutenantOnlyUnitsEnabled;

    public bool TeamsViewEnabled => Request.TeamsViewEnabled;

    /// <summary>The criterion whose value chips are shown.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedCriterionTitle))]
    public partial FilterCriterionItem? SelectedCriterion { get; set; }

    public string SelectedCriterionTitle => SelectedCriterion?.Title ?? "Select a criterion";

    /// <summary>Whether the open criterion matches units with any or all of its values.</summary>
    [ObservableProperty]
    public partial string SelectedActiveMatchMode { get; set; } = AnyMode;

    [ObservableProperty]
    public partial string? SelectedMinPoints { get; set; }

    [ObservableProperty]
    public partial string? SelectedMaxPoints { get; set; }

    [ObservableProperty]
    public partial bool SelectedLieutenantOnlyUnits { get; set; }

    [ObservableProperty]
    public partial bool SelectedTeamsView { get; set; }

    // Leaving a criterion keeps what was chosen in it.
    partial void OnSelectedCriterionChanging(FilterCriterionItem? oldValue, FilterCriterionItem? newValue)
    {
        if (oldValue is not null)
        {
            CommitCriterionFromEditor(oldValue);
            oldValue.IsCurrent = false;
        }
    }

    partial void OnSelectedCriterionChanged(FilterCriterionItem? value)
    {
        if (value is not null)
        {
            value.IsCurrent = true;
        }

        LoadSelectedCriterionEditor();
    }

    // A match-mode change re-commits the open criterion so its summary prefix (Any/All) stays in sync,
    // but not while the editor is loading.
    partial void OnSelectedActiveMatchModeChanged(string value)
    {
        if (!_isLoadingEditor && SelectedCriterion is { } criterion)
        {
            CommitCriterionFromEditor(criterion);
        }
    }

    [RelayCommand]
    private void SelectCriterion(FilterCriterionItem criterion) => SelectedCriterion = criterion;

    /// <summary>Selections are live: toggling a value commits it so the criterion's marker and summary stay in sync.</summary>
    [RelayCommand]
    private void ToggleOption(SelectableFilterOption option)
    {
        option.IsSelected = !option.IsSelected;
        CommitSelectedCriterion();
    }

    [RelayCommand]
    private void ClearCriterion()
    {
        foreach (var option in ActiveCriterionOptions)
        {
            option.IsSelected = false;
        }

        CommitSelectedCriterion();
    }

    /// <summary>Clears every criterion and toggle and opens the whole points range.</summary>
    [RelayCommand]
    private void Reset()
    {
        _committedTerms.Clear();
        UpdateAllCriterionSummaries();
        SelectedMinPoints = ClampedPoints(_minPoints);
        SelectedMaxPoints = ClampedPoints(_maxPoints);
        SelectedLieutenantOnlyUnits = false;
        SelectedTeamsView = false;
        LoadSelectedCriterionEditor();
    }

    [RelayCommand]
    private void Apply()
    {
        _apply.Execute(BuildCriteria());
        _close.Execute(null);
    }

    [RelayCommand]
    private void Close() => _close.Execute(null);

    /// <summary>The filter as chosen: each criterion with values, and the points range in order.</summary>
    public UnitFilterCriteria BuildCriteria()
    {
        CommitSelectedCriterion();

        var minPoints = int.TryParse(SelectedMinPoints, out var parsedMin) ? parsedMin : 0;
        var maxPoints = int.TryParse(SelectedMaxPoints, out var parsedMax) ? parsedMax : minPoints;
        if (minPoints > maxPoints)
        {
            (minPoints, maxPoints) = (maxPoints, minPoints);
        }

        return new UnitFilterCriteria
        {
            Terms = _committedTerms.Values.OrderBy(term => term.Field).ToList(),
            MinPoints = minPoints,
            MaxPoints = maxPoints,
            LieutenantOnlyUnits = SelectedLieutenantOnlyUnits,
            TeamsView = SelectedTeamsView
        };
    }

    private void CommitSelectedCriterion()
    {
        if (SelectedCriterion is { } criterion)
        {
            CommitCriterionFromEditor(criterion);
        }
    }

    private void LoadSelectedCriterionEditor()
    {
        _isLoadingEditor = true;
        try
        {
            ActiveCriterionOptions.Clear();
            if (SelectedCriterion is null)
            {
                SelectedActiveMatchMode = AnyMode;
                return;
            }

            var field = SelectedCriterion.Field;
            _committedTerms.TryGetValue(field, out var term);
            var selectedValues = term?.Values.ToHashSet(StringComparer.OrdinalIgnoreCase)
                                 ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var value in _availableValues[field])
            {
                ActiveCriterionOptions.Add(new SelectableFilterOption(value) { IsSelected = selectedValues.Contains(value) });
            }

            SelectedActiveMatchMode = term?.MatchMode == UnitFilterMatchMode.All ? AllMode : AnyMode;
        }
        finally
        {
            _isLoadingEditor = false;
        }
    }

    private void CommitCriterionFromEditor(FilterCriterionItem criterion)
    {
        var values = ActiveCriterionOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Value)
            .ToList();

        if (values.Count == 0)
        {
            _committedTerms.Remove(criterion.Field);
        }
        else
        {
            var matchMode = string.Equals(SelectedActiveMatchMode, AllMode, StringComparison.OrdinalIgnoreCase)
                ? UnitFilterMatchMode.All
                : UnitFilterMatchMode.Any;
            _committedTerms[criterion.Field] = new UnitFilterTerm(criterion.Field, values, matchMode);
        }

        UpdateCriterionSummary(criterion);
    }

    private void UpdateAllCriterionSummaries()
    {
        foreach (var criterion in CriteriaItems)
        {
            UpdateCriterionSummary(criterion);
        }
    }

    private void UpdateCriterionSummary(FilterCriterionItem criterion)
    {
        criterion.HasSelections = _committedTerms.TryGetValue(criterion.Field, out var term);
        criterion.Summary = term is null || term.Values.Count == 0
            ? AnyMode
            : $"{(term.MatchMode == UnitFilterMatchMode.All ? AllMode : AnyMode)}: {string.Join(", ", term.Values.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))}";
    }

    private string ClampedPoints(int points) =>
        Math.Clamp(points, _minPoints, _maxPoints).ToString(CultureInfo.InvariantCulture);

    private static IReadOnlyList<string> DistinctSorted(IEnumerable<string> values) =>
        values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
