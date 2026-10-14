using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace InfinityMercsApp.Views.Controls;

public sealed class UnitFilterPopupOptions
{
    public List<string> Classification { get; init; } = [];
    public List<string> Characteristics { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<string> Equipment { get; init; } = [];
    public List<string> Weapons { get; init; } = [];
    public List<string> Ammo { get; init; } = [];
    public int MinPoints { get; init; }
    public int MaxPoints { get; init; } = 200;
}

/// <summary>What the filter popup opens with: the values to choose from and the filter already applied.</summary>
public sealed record UnitFilterRequest(
    UnitFilterPopupOptions Options,
    UnitFilterCriteria ExistingCriteria,
    bool LieutenantOnlyUnits,
    bool TeamsView = false,
    bool TeamsViewEnabled = true,
    bool LieutenantOnlyUnitsEnabled = true);

public sealed class SelectableFilterOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public SelectableFilterOption(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed class FilterCriterionItem : INotifyPropertyChanged
{
    private string _summary = "Any";
    private bool _isCurrent;
    private bool _hasSelections;

    public FilterCriterionItem(UnitFilterField field, string title)
    {
        Field = field;
        Title = title;
    }

    public UnitFilterField Field { get; }

    public string Title { get; }

    public string Summary
    {
        get => _summary;
        set
        {
            if (string.Equals(_summary, value, StringComparison.Ordinal))
            {
                return;
            }

            _summary = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary)));
        }
    }

    /// <summary>The criterion whose value chips are currently shown in the editor.</summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (_isCurrent == value)
            {
                return;
            }

            _isCurrent = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCurrent)));
        }
    }

    /// <summary>True when this criterion has committed (active) values; drives the chip's active marker.</summary>
    public bool HasSelections
    {
        get => _hasSelections;
        set
        {
            if (_hasSelections == value)
            {
                return;
            }

            _hasSelections = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasSelections)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class UnitFilterPopupView : ContentView
{
    private const string AnyMode = "Any";
    private const string AllMode = "All";

    /// <summary>Setting a request reloads the popup with its options and existing filter, as if newly opened.</summary>
    public static readonly BindableProperty RequestProperty =
        BindableProperty.Create(
            nameof(Request),
            typeof(UnitFilterRequest),
            typeof(UnitFilterPopupView),
            null,
            propertyChanged: (bindable, _, newValue) =>
            {
                if (newValue is UnitFilterRequest request)
                {
                    ((UnitFilterPopupView)bindable).Load(request);
                }
            });

    private Dictionary<UnitFilterField, List<string>> _availableValues = [];
    private readonly Dictionary<UnitFilterField, UnitFilterTerm> _committedTerms = [];
    private FilterCriterionItem? _selectedCriterion;
    private string _selectedActiveMatchMode = AnyMode;
    private bool _isLoadingEditor;

    public event EventHandler<UnitFilterCriteria>? FilterArmyApplied;
    public event EventHandler? CloseRequested;

    public ObservableCollection<FilterCriterionItem> CriteriaItems { get; }
    public ObservableCollection<SelectableFilterOption> ActiveCriterionOptions { get; } = [];
    public ObservableCollection<string> PointsOptions { get; private set; } = [];
    public ObservableCollection<string> MatchModeOptions { get; } = [AnyMode, AllMode];

    public FilterCriterionItem? SelectedCriterion
    {
        get => _selectedCriterion;
        set
        {
            if (_selectedCriterion == value)
            {
                return;
            }

            // Persist current editor state when switching criteria.
            if (_selectedCriterion is not null)
            {
                CommitCriterionFromEditor(_selectedCriterion);
                _selectedCriterion.IsCurrent = false;
            }

            _selectedCriterion = value;
            if (_selectedCriterion is not null)
            {
                _selectedCriterion.IsCurrent = true;
            }

            OnPropertyChanged(nameof(SelectedCriterion));
            OnPropertyChanged(nameof(SelectedCriterionTitle));
            LoadSelectedCriterionEditor();
        }
    }

    public string SelectedCriterionTitle => SelectedCriterion?.Title ?? "Select a criterion";

    public string SelectedActiveMatchMode
    {
        get => _selectedActiveMatchMode;
        set
        {
            if (string.Equals(_selectedActiveMatchMode, value, StringComparison.Ordinal))
            {
                return;
            }

            _selectedActiveMatchMode = value;
            OnPropertyChanged(nameof(SelectedActiveMatchMode));

            // A match-mode change re-commits the open criterion so its summary
            // prefix (Any/All) stays in sync, but not while the editor is loading.
            if (!_isLoadingEditor && _selectedCriterion is not null)
            {
                CommitCriterionFromEditor(_selectedCriterion);
            }
        }
    }

    public string? SelectedMinPoints { get; set; }
    public string? SelectedMaxPoints { get; set; }
    public bool SelectedLieutenantOnlyUnits { get; set; }
    public bool LieutenantOnlyUnitsEnabled { get; set; } = true;
    public bool SelectedTeamsView { get; set; }
    public bool TeamsViewEnabled { get; set; }

    public UnitFilterPopupView()
        : this(new UnitFilterPopupOptions(), UnitFilterCriteria.None, lieutenantOnlyUnits: false, teamsView: false)
    {
    }

    public UnitFilterPopupView(
        UnitFilterPopupOptions options,
        UnitFilterCriteria? existingCriteria = null,
        bool lieutenantOnlyUnits = false,
        bool teamsView = false,
        bool teamsViewEnabled = true,
        bool lieutenantOnlyUnitsEnabled = true)
    {
        InitializeComponent();

        CriteriaItems = new ObservableCollection<FilterCriterionItem>
        {
            new(UnitFilterField.Classification, "Classification"),
            new(UnitFilterField.Characteristics, "Characteristics"),
            new(UnitFilterField.Skills, "Skills"),
            new(UnitFilterField.Equipment, "Equipment"),
            new(UnitFilterField.Weapons, "Weapons"),
            new(UnitFilterField.Ammo, "Ammo")
        };

        BindingContext = this;
        Load(new UnitFilterRequest(
            options,
            existingCriteria ?? UnitFilterCriteria.None,
            lieutenantOnlyUnits,
            teamsView,
            teamsViewEnabled,
            lieutenantOnlyUnitsEnabled));
    }

    public UnitFilterRequest? Request
    {
        get => (UnitFilterRequest?)GetValue(RequestProperty);
        set => SetValue(RequestProperty, value);
    }

    // Starts afresh from the request: its option values, points range and existing filter, first criterion open.
    private void Load(UnitFilterRequest request)
    {
        var options = request.Options;
        _availableValues = new Dictionary<UnitFilterField, List<string>>
        {
            [UnitFilterField.Classification] = NormalizeValues(options.Classification),
            [UnitFilterField.Characteristics] = NormalizeValues(options.Characteristics),
            [UnitFilterField.Skills] = NormalizeValues(options.Skills),
            [UnitFilterField.Equipment] = NormalizeValues(options.Equipment),
            [UnitFilterField.Weapons] = NormalizeValues(options.Weapons),
            [UnitFilterField.Ammo] = NormalizeValues(options.Ammo)
        };

        var minPoints = Math.Max(0, options.MinPoints);
        var maxPoints = Math.Max(minPoints, options.MaxPoints);
        PointsOptions = BuildPointsOptions(minPoints, maxPoints);
        LieutenantOnlyUnitsEnabled = request.LieutenantOnlyUnitsEnabled;
        TeamsViewEnabled = request.TeamsViewEnabled;
        OnPropertyChanged(nameof(PointsOptions));
        OnPropertyChanged(nameof(LieutenantOnlyUnitsEnabled));
        OnPropertyChanged(nameof(TeamsViewEnabled));

        if (_selectedCriterion is not null)
        {
            _selectedCriterion.IsCurrent = false;
            _selectedCriterion = null;
        }

        ApplySelections(request.ExistingCriteria, minPoints, maxPoints, request.LieutenantOnlyUnits, request.TeamsView);
    }

    private static List<string> NormalizeValues(IEnumerable<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ObservableCollection<string> BuildPointsOptions(int minValue, int maxValue)
    {
        var values = new ObservableCollection<string>();
        for (var i = minValue; i <= maxValue; i++)
        {
            values.Add(i.ToString());
        }

        return values;
    }

    private void ResetSelections(int minPoints, int maxPoints)
    {
        _committedTerms.Clear();
        UpdateAllCriterionSummaries();

        SelectedActiveMatchMode = AnyMode;
        SelectedMinPoints = minPoints.ToString();
        SelectedMaxPoints = maxPoints.ToString();
        SelectedLieutenantOnlyUnits = false;
        SelectedTeamsView = false;
        OnPropertyChanged(nameof(SelectedMinPoints));
        OnPropertyChanged(nameof(SelectedMaxPoints));
        OnPropertyChanged(nameof(SelectedLieutenantOnlyUnits));
        OnPropertyChanged(nameof(SelectedTeamsView));

        if (CriteriaItems.Count == 0)
        {
            ActiveCriterionOptions.Clear();
            return;
        }

        if (SelectedCriterion is null)
        {
            SelectedCriterion = CriteriaItems[0];
        }
        else
        {
            LoadSelectedCriterionEditor();
        }
    }

    private void ApplySelections(
        UnitFilterCriteria criteria,
        int minPoints,
        int maxPoints,
        bool lieutenantOnlyUnits,
        bool teamsView)
    {
        ResetSelections(minPoints, maxPoints);

        var query = criteria.ToQuery();
        foreach (var term in query.Terms)
        {
            _committedTerms[term.Field] = term;
        }

        UpdateAllCriterionSummaries();
        SelectedMinPoints = ResolvePointsSelection(minPoints, maxPoints, criteria.MinPoints, minPoints);
        SelectedMaxPoints = ResolvePointsSelection(minPoints, maxPoints, criteria.MaxPoints, maxPoints);
        SelectedLieutenantOnlyUnits = lieutenantOnlyUnits;
        SelectedTeamsView = teamsView;
        OnPropertyChanged(nameof(SelectedMinPoints));
        OnPropertyChanged(nameof(SelectedMaxPoints));
        OnPropertyChanged(nameof(SelectedLieutenantOnlyUnits));
        OnPropertyChanged(nameof(SelectedTeamsView));

        if (SelectedCriterion is null && CriteriaItems.Count > 0)
        {
            SelectedCriterion = CriteriaItems[0];
            return;
        }

        LoadSelectedCriterionEditor();
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
                ActiveCriterionOptions.Add(new SelectableFilterOption(value)
                {
                    IsSelected = selectedValues.Contains(value)
                });
            }

            SelectedActiveMatchMode = term?.MatchMode == UnitFilterMatchMode.All ? AllMode : AnyMode;
        }
        finally
        {
            _isLoadingEditor = false;
        }
    }

    private void UpdateAllCriterionSummaries()
    {
        foreach (var item in CriteriaItems)
        {
            item.Summary = BuildSummary(item.Field);
            item.HasSelections = _committedTerms.ContainsKey(item.Field);
        }
    }

    private string BuildSummary(UnitFilterField field)
    {
        if (!_committedTerms.TryGetValue(field, out var term) || term.Values.Count == 0)
        {
            return "Any";
        }

        var prefix = term.MatchMode == UnitFilterMatchMode.All ? "All" : "Any";
        var values = string.Join(", ", term.Values.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        return $"{prefix}: {values}";
    }

    private static string ResolvePointsSelection(int minPoints, int maxPoints, int? value, int fallback)
    {
        var normalized = value ?? fallback;
        normalized = Math.Min(Math.Max(normalized, minPoints), maxPoints);
        return normalized.ToString();
    }

    private static UnitFilterMatchMode ParseMatchMode(string? mode)
    {
        return string.Equals(mode, AllMode, StringComparison.OrdinalIgnoreCase)
            ? UnitFilterMatchMode.All
            : UnitFilterMatchMode.Any;
    }

    private void OnCriterionChipTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { BindingContext: FilterCriterionItem item })
        {
            SelectedCriterion = item;
        }
    }

    private void OnOptionChipTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border { BindingContext: SelectableFilterOption option })
        {
            return;
        }

        // Selections are live: toggling a value chip commits it immediately so the
        // criterion's active marker and summary stay in sync without an Apply step.
        option.IsSelected = !option.IsSelected;
        if (SelectedCriterion is not null)
        {
            CommitCriterionFromEditor(SelectedCriterion);
        }
    }

    private void OnClearCriterionClicked(object? sender, EventArgs e)
    {
        foreach (var option in ActiveCriterionOptions)
        {
            option.IsSelected = false;
        }

        if (SelectedCriterion is not null)
        {
            CommitCriterionFromEditor(SelectedCriterion);
        }
    }

    private void CommitCriterionFromEditor(FilterCriterionItem criterion)
    {
        var field = criterion.Field;
        var values = ActiveCriterionOptions
            .Where(option => option.IsSelected)
            .Select(option => option.Value)
            .ToList();

        if (values.Count == 0)
        {
            _committedTerms.Remove(field);
        }
        else
        {
            _committedTerms[field] = new UnitFilterTerm(
                field,
                values,
                ParseMatchMode(SelectedActiveMatchMode));
        }

        criterion.Summary = BuildSummary(field);
        criterion.HasSelections = _committedTerms.ContainsKey(field);
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnResetClicked(object? sender, EventArgs e)
    {
        var parsedMin = int.TryParse(PointsOptions.FirstOrDefault(), out var minValue) ? minValue : 0;
        var parsedMax = int.TryParse(PointsOptions.LastOrDefault(), out var maxValue) ? maxValue : parsedMin;
        ResetSelections(parsedMin, parsedMax);
    }

    private void OnFilterArmyClicked(object? sender, EventArgs e)
    {
        if (SelectedCriterion is not null)
        {
            CommitCriterionFromEditor(SelectedCriterion);
        }

        var minPoints = int.TryParse(SelectedMinPoints, out var parsedMin) ? parsedMin : 0;
        var maxPoints = int.TryParse(SelectedMaxPoints, out var parsedMax) ? parsedMax : minPoints;
        if (minPoints > maxPoints)
        {
            (minPoints, maxPoints) = (maxPoints, minPoints);
        }

        var terms = _committedTerms.Values
            .OrderBy(term => term.Field)
            .ToList();

        var criteria = new UnitFilterCriteria
        {
            Terms = terms,
            MinPoints = minPoints,
            MaxPoints = maxPoints,
            LieutenantOnlyUnits = SelectedLieutenantOnlyUnits,
            TeamsView = SelectedTeamsView
        };

        FilterArmyApplied?.Invoke(this, criteria);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    // Single-column layout works at every width, so sizing is all that adapts:
    // cap the popup to a comfortable reading column and let the body ScrollView
    // absorb any overflow rather than running off a short screen.
    private const double MaxPopupWidth = 560d;
    private const double OuterPadding = 32d; // root Grid Padding="16" on both sides

    private void OnPopupSizeChanged(object? sender, EventArgs e)
    {
        var hostHeight = Height > 0
            ? Height
            : Window?.Height ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Height ?? 0;
        if (hostHeight > 0)
        {
            PopupContainer.MaximumHeightRequest = hostHeight * 0.9;
        }

        var width = Width > 0
            ? Width
            : Window?.Width ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Width ?? 0;
        if (width > 0)
        {
            PopupContainer.WidthRequest = Math.Min(width - OuterPadding, MaxPopupWidth);
        }
    }
}
