using System.Collections;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>The selection screen's UNIT SELECTION panel: its header with the filter button, over the unit list.</summary>
public partial class CompanyUnitSelectionListPanelView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(CompanyUnitSelectionListPanelView));

    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommand),
            typeof(ICommand),
            typeof(CompanyUnitSelectionListPanelView));

    public static readonly BindableProperty ShowUnitsListProperty =
        BindableProperty.Create(
            nameof(ShowUnitsList),
            typeof(bool),
            typeof(CompanyUnitSelectionListPanelView),
            true);

    public static readonly BindableProperty IsDenseProperty =
        BindableProperty.Create(
            nameof(IsDense),
            typeof(bool),
            typeof(CompanyUnitSelectionListPanelView),
            false,
            propertyChanged: OnIsDenseChanged);

    // Compact-screen scale for the UNIT SELECTION header. The filter button is sized externally as
    // headerHeight * 0.8, so shrinking the header by this factor shrinks the filter button to match.
    private const double DenseScale = 0.6d;
    private static readonly Thickness HeaderPaddingDefault = new(12, 8, 0, 8);

    public CompanyUnitSelectionListPanelView()
    {
        InitializeComponent();
        ApplyDensity();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }

    public bool ShowUnitsList
    {
        get => (bool)GetValue(ShowUnitsListProperty);
        set => SetValue(ShowUnitsListProperty, value);
    }

    /// <summary>When true (small screens), the UNIT SELECTION header and its filter button render at
    /// 60% size to reclaim vertical space.</summary>
    public bool IsDense
    {
        get => (bool)GetValue(IsDenseProperty);
        set => SetValue(IsDenseProperty, value);
    }

    private static void OnIsDenseChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CompanyUnitSelectionListPanelView panel)
        {
            panel.ApplyDensity();
        }
    }

    private void ApplyDensity()
    {
        if (IsDense)
        {
            UnitSelectionHeaderBorder.Padding = new Thickness(
                HeaderPaddingDefault.Left * DenseScale,
                HeaderPaddingDefault.Top * DenseScale,
                HeaderPaddingDefault.Right,
                HeaderPaddingDefault.Bottom * DenseScale);

            var baseFontSize = ResolveFontSize("FontSizeSectionHeader", 18d);
            UnitSelectionHeaderLabel.FontSize = baseFontSize * DenseScale;
        }
        else
        {
            UnitSelectionHeaderBorder.Padding = HeaderPaddingDefault;
            UnitSelectionHeaderLabel.SetDynamicResource(Label.FontSizeProperty, "FontSizeSectionHeader");
        }
    }

    private static double ResolveFontSize(string key, double fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var raw) == true && raw is double size)
        {
            return size;
        }

        return fallback;
    }

    public static readonly BindableProperty FilterCommandProperty =
        BindableProperty.Create(nameof(FilterCommand), typeof(ICommand), typeof(CompanyUnitSelectionListPanelView));

    public static readonly BindableProperty IsFilterActiveProperty =
        BindableProperty.Create(nameof(IsFilterActive), typeof(bool), typeof(CompanyUnitSelectionListPanelView), false);

    /// <summary>Runs when the filter button in the header is tapped.</summary>
    public ICommand? FilterCommand
    {
        get => (ICommand?)GetValue(FilterCommandProperty);
        set => SetValue(FilterCommandProperty, value);
    }

    /// <summary>Highlights the filter button while a filter is applied.</summary>
    public bool IsFilterActive
    {
        get => (bool)GetValue(IsFilterActiveProperty);
        set => SetValue(IsFilterActiveProperty, value);
    }

    // The filter button fills most of the header's height. Re-assigning an unchanged size would re-raise
    // the header's SizeChanged and keep the layout spinning, so a size within a pixel is left alone.
    private void OnHeaderBorderSizeChanged(object? sender, EventArgs e)
    {
        var size = UnitSelectionHeaderBorder.Height * 0.8;
        if (size <= 0 ||
            (Math.Abs(UnitSelectionFilterButton.WidthRequest - size) < 1.0 && Math.Abs(UnitSelectionFilterButton.HeightRequest - size) < 1.0))
        {
            return;
        }

        UnitSelectionFilterButton.WidthRequest = size;
        UnitSelectionFilterButton.HeightRequest = size;
        UnitSelectionFilterIcon.WidthRequest = size;
        UnitSelectionFilterIcon.HeightRequest = size;
    }
}
