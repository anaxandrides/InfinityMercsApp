namespace InfinityMercsApp.Views.Controls;

/// <summary>The company's points against the season's starting limit, and whether the company can start.</summary>
public partial class SeasonStartPointsView : ContentView
{
    public static readonly BindableProperty SeasonPointsCapTextProperty =
        BindableProperty.Create(nameof(SeasonPointsCapText), typeof(string), typeof(SeasonStartPointsView), "0");

    public static readonly BindableProperty SelectedStartSeasonPointsProperty =
        BindableProperty.Create(nameof(SelectedStartSeasonPoints), typeof(string), typeof(SeasonStartPointsView), "75", BindingMode.TwoWay);

    public static readonly BindableProperty IsCompanyValidProperty =
        BindableProperty.Create(nameof(IsCompanyValid), typeof(bool), typeof(SeasonStartPointsView), false);

    public SeasonStartPointsView()
    {
        InitializeComponent();
    }

    /// <summary>The company's points so far, shown left of the limit picker.</summary>
    public string SeasonPointsCapText
    {
        get => (string)GetValue(SeasonPointsCapTextProperty);
        set => SetValue(SeasonPointsCapTextProperty, value);
    }

    /// <summary>The chosen starting limit: 60, 75 or 90.</summary>
    public string SelectedStartSeasonPoints
    {
        get => (string)GetValue(SelectedStartSeasonPointsProperty);
        set => SetValue(SelectedStartSeasonPointsProperty, value);
    }

    /// <summary>Whether the company is valid to start; a check when it is, a cross when not.</summary>
    public bool IsCompanyValid
    {
        get => (bool)GetValue(IsCompanyValidProperty);
        set => SetValue(IsCompanyValidProperty, value);
    }
}
