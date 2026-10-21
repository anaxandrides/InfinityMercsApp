using System.Collections;
using System.Windows.Input;
using InfinityMercsApp.ViewModels.Components;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// The selected unit's header (trait icons, logo, name, statline, peripheral and summaries) over its
/// profiles. The header shows <see cref="Unit"/>; this code only fits fonts and columns to the width.
/// </summary>
public partial class UnitDisplayConfigurationsView : ContentView
{
    private const double HeadingMinFontSize = 11d;
    private const double HeadingFontStep = 0.5d;
    private const double DefaultStatFontSize = 10d;
    private const int AvaColumn = 9;

    // The faction emblem shrinks as the pane narrows and only disappears when there
    // is genuinely no room, so the selected unit's faction stays identifiable at
    // small window sizes instead of the header reading as an empty, cut-off box.
    private const double LogoHideThreshold = 320d;
    private const double LogoCompactThreshold = 460d;
    // Below this pane width the per-profile weapons/skills/equipment block reflows from a single
    // row of four columns into a 2x2 grid so each cell has room instead of wrapping every word.
    private const double ProfileTwoColumnThreshold = 520d;
    private const double LogoFullColumnWidth = 84d;
    private const double LogoFullCanvasSize = 82d;
    private const double LogoCompactColumnWidth = 54d;
    private const double LogoCompactCanvasSize = 50d;
    // The trait-icon column is squeezed on narrow panes so the statline (MOV, CC, ...) gets the
    // freed horizontal space; it shares the logo's compact threshold so both shrink together.
    private const double IconColumnFullWidth = 58d;
    private const double IconColumnFullCanvasSize = 50d;
    private const double IconColumnCompactWidth = 34d;
    private const double IconColumnCompactCanvasSize = 30d;

    public static readonly BindableProperty UnitProperty =
        BindableProperty.Create(nameof(Unit), typeof(IUnitDisplay), typeof(UnitDisplayConfigurationsView));
    public static readonly BindableProperty ProfilesProperty =
        BindableProperty.Create(nameof(Profiles), typeof(IEnumerable), typeof(UnitDisplayConfigurationsView));
    public static readonly BindableProperty ProfileTappedCommandProperty =
        BindableProperty.Create(nameof(ProfileTappedCommand), typeof(ICommand), typeof(UnitDisplayConfigurationsView));
    public static readonly BindableProperty ShowConfigurationsSectionProperty =
        BindableProperty.Create(nameof(ShowConfigurationsSection), typeof(bool), typeof(UnitDisplayConfigurationsView), true,
            propertyChanged: (bindable, _, _) => ((UnitDisplayConfigurationsView)bindable).UpdateConfigurationRows());
    public static readonly BindableProperty ShowUnitNameProperty =
        BindableProperty.Create(nameof(ShowUnitName), typeof(bool), typeof(UnitDisplayConfigurationsView), true);
    public static readonly BindableProperty ShowSummaryRowsProperty =
        BindableProperty.Create(nameof(ShowSummaryRows), typeof(bool), typeof(UnitDisplayConfigurationsView), true);
    public static readonly BindableProperty ShowAvaProperty =
        BindableProperty.Create(nameof(ShowAva), typeof(bool), typeof(UnitDisplayConfigurationsView), true,
            propertyChanged: (bindable, _, _) => ((UnitDisplayConfigurationsView)bindable).UpdateAvaColumns());
    public static readonly BindableProperty IsCompactProfileLayoutProperty =
        BindableProperty.Create(nameof(IsCompactProfileLayout), typeof(bool), typeof(UnitDisplayConfigurationsView), false);

    private double _logoColumnWidth = -1d;
    private double _iconColumnWidth = -1d;
    private double _statFontSize = DefaultStatFontSize;
    private double _statHeaderFontSize = DefaultStatFontSize;

    // Re-entrancy guards for the two size-driven font fitters below. Both adjust a font size from
    // inside a SizeChanged callback, which re-measures the element and re-raises SizeChanged; without
    // these the recursion can outrun WinUI's layout-pass iteration cap and throw LayoutCycleException.
    private bool _adjustingHeadingFontSize;
    private bool _adjustingStatFontSize;

    public UnitDisplayConfigurationsView()
    {
        InitializeComponent();
        UnitNameHeadingLabel.FontSize = HeadingMaxFontSize();
        UnitNameHeadingLabel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == Label.TextProperty.PropertyName)
            {
                FitHeadingFontSize();
            }
        };
        ApplyStatFontSizes(DefaultStatFontSize, DefaultStatFontSize);
        UpdateConfigurationRows();
    }

    /// <summary>The unit the header shows.</summary>
    public IUnitDisplay? Unit
    {
        get => (IUnitDisplay?)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    /// <summary>The configurations listed under the header.</summary>
    public IEnumerable? Profiles
    {
        get => (IEnumerable?)GetValue(ProfilesProperty);
        set => SetValue(ProfilesProperty, value);
    }

    /// <summary>Runs with the tapped configuration, e.g. to add it to the company.</summary>
    public ICommand? ProfileTappedCommand
    {
        get => (ICommand?)GetValue(ProfileTappedCommandProperty);
        set => SetValue(ProfileTappedCommandProperty, value);
    }

    /// <summary>Whether the "Profiles" list is shown under the header.</summary>
    public bool ShowConfigurationsSection
    {
        get => (bool)GetValue(ShowConfigurationsSectionProperty);
        set => SetValue(ShowConfigurationsSectionProperty, value);
    }

    public bool ShowUnitName
    {
        get => (bool)GetValue(ShowUnitNameProperty);
        set => SetValue(ShowUnitNameProperty, value);
    }

    /// <summary>Whether the shared equipment and skills rows are shown.</summary>
    public bool ShowSummaryRows
    {
        get => (bool)GetValue(ShowSummaryRowsProperty);
        set => SetValue(ShowSummaryRowsProperty, value);
    }

    public bool ShowAva
    {
        get => (bool)GetValue(ShowAvaProperty);
        set => SetValue(ShowAvaProperty, value);
    }

    /// <summary>
    /// When true the per-profile weapons/skills/equipment block is laid out as a 2x2 grid
    /// (equipment + skills on top, ranged + close combat below) for narrow panes.
    /// </summary>
    public bool IsCompactProfileLayout
    {
        get => (bool)GetValue(IsCompactProfileLayoutProperty);
        private set => SetValue(IsCompactProfileLayoutProperty, value);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0) return;

        // Reflow the per-profile block to 2x2 on narrow panes. Set before the logo early-return
        // below, whose threshold differs, so crossing this width always updates the layout.
        IsCompactProfileLayout = width < ProfileTwoColumnThreshold;

        UpdateIconColumnWidth(width);

        var (columnWidth, canvasSize) = width < LogoHideThreshold
            ? (0d, 0d)
            : width < LogoCompactThreshold
                ? (LogoCompactColumnWidth, LogoCompactCanvasSize)
                : (LogoFullColumnWidth, LogoFullCanvasSize);

        if (Math.Abs(_logoColumnWidth - columnWidth) < 0.5) return;
        _logoColumnWidth = columnWidth;

        var showLogo = columnWidth > 0d;
        LogoColumnBorder.IsVisible = showLogo;
        ConfigurationsRootGrid.ColumnDefinitions[1].Width = new GridLength(columnWidth);
        if (showLogo)
        {
            SelectedUnitCanvas.WidthRequest = canvasSize;
            SelectedUnitCanvas.HeightRequest = canvasSize;
        }
    }

    // Narrows the trait-icon column (and its canvas) on small panes so the statline keeps the room.
    private void UpdateIconColumnWidth(double width)
    {
        var (columnWidth, canvasSize) = width < LogoCompactThreshold
            ? (IconColumnCompactWidth, IconColumnCompactCanvasSize)
            : (IconColumnFullWidth, IconColumnFullCanvasSize);

        if (Math.Abs(_iconColumnWidth - columnWidth) < 0.5) return;
        _iconColumnWidth = columnWidth;

        ConfigurationsRootGrid.ColumnDefinitions[0].Width = new GridLength(columnWidth);
        HeaderIconsCanvas.WidthRequest = canvasSize;
    }

    private void OnUnitNameHeadingLabelSizeChanged(object? sender, EventArgs e) => FitHeadingFontSize();

    private static double HeadingMaxFontSize() =>
        Application.Current?.Resources.TryGetValue("FontSizeSubHeadline", out var raw) == true && raw is double size ? size : 24d;

    /// <summary>Fits the heading to its width, stepping the font down from the largest size.</summary>
    private void FitHeadingFontSize()
    {
        // Assigning the font below re-measures the label and re-raises its SizeChanged, which calls
        // back here; bail on the re-entrant pass so the fitted value stands and layout can settle.
        if (_adjustingHeadingFontSize)
        {
            return;
        }

        _adjustingHeadingFontSize = true;
        try
        {
            var maxFontSize = HeadingMaxFontSize();
            var availableWidth = UnitNameHeadingLabel.Width;
            if (availableWidth <= 0)
            {
                UnitNameHeadingLabel.FontSize = maxFontSize;
                return;
            }

            for (var size = maxFontSize; size >= HeadingMinFontSize; size -= HeadingFontStep)
            {
                UnitNameHeadingLabel.FontSize = size;
                if (UnitNameHeadingLabel.Measure(double.PositiveInfinity, double.PositiveInfinity).Width <= availableWidth)
                {
                    return;
                }
            }

            UnitNameHeadingLabel.FontSize = HeadingMinFontSize;
        }
        finally
        {
            _adjustingHeadingFontSize = false;
        }
    }

    private void OnStatHeadersBorderSizeChanged(object? sender, EventArgs e)
    {
        if (_adjustingStatFontSize) return;
        if (sender is not View view || view.Height <= 0 || view.Width <= 0) return;

        // Height constraint: labels should fill the row without overflowing vertically. The border
        // sits in an Auto row, so its height grows with the font we set here — a positive feedback
        // loop. We cap by the width-derived size and stop re-applying once settled (below) so the
        // loop converges instead of cycling the layout pass.
        var fontFromHeight = (view.Height - 4) * 0.75;

        // Width constraint: ColumnSpacing=6 with 9 or 10 columns, Border padding=8 each side.
        var numCols = ShowAva ? 10 : 9;
        var colSpacing = 6 * (numCols - 1);
        var colWidth = (view.Width - 16 - colSpacing) / numCols;

        // Headers: use 3.2× to account for wide unicode symbols like ▲ in the vitality header.
        var headerFontFromWidth = colWidth / 3.2;
        var newHeaderFontSize = Math.Max(6.0, Math.Round(Math.Min(fontFromHeight, headerFontFromWidth), 1));

        // Values: bold character width ≈ 0.65× font size, so 4 chars ≈ 2.6× font size.
        var fontFromWidth = colWidth / 2.6;
        var newStatFontSize = Math.Max(6.0, Math.Round(Math.Min(fontFromHeight, fontFromWidth), 1));

        // Already at the target (within a font-pixel): re-applying would re-raise SizeChanged for no
        // visible gain and keep the layout pass spinning. Leave it be.
        if (Math.Abs(_statHeaderFontSize - newHeaderFontSize) < 0.5
            && Math.Abs(_statFontSize - newStatFontSize) < 0.5)
        {
            return;
        }

        _adjustingStatFontSize = true;
        try
        {
            ApplyStatFontSizes(newHeaderFontSize, newStatFontSize);
        }
        finally
        {
            _adjustingStatFontSize = false;
        }
    }

    private void ApplyStatFontSizes(double headerFontSize, double valueFontSize)
    {
        _statHeaderFontSize = headerFontSize;
        _statFontSize = valueFontSize;
        foreach (var label in MainStatHeadersGrid.Children.OfType<Label>())
        {
            label.FontSize = headerFontSize;
        }

        foreach (var label in MainStatValuesGrid.Children.OfType<Label>())
        {
            label.FontSize = valueFontSize;
        }
    }

    // A hidden AVA column gives its width to the other stats.
    private void UpdateAvaColumns()
    {
        var width = ShowAva ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        MainStatHeadersGrid.ColumnDefinitions[AvaColumn].Width = width;
        MainStatValuesGrid.ColumnDefinitions[AvaColumn].Width = width;
        PeripheralStatHeadersGrid.ColumnDefinitions[AvaColumn].Width = width;
        PeripheralStatValuesGrid.ColumnDefinitions[AvaColumn].Width = width;
    }

    // Without the profiles list its title and star row collapse, so the header takes only its own height.
    private void UpdateConfigurationRows()
    {
        var showProfiles = ShowConfigurationsSection;
        ConfigurationsRootGrid.RowDefinitions[6].Height = showProfiles ? GridLength.Auto : new GridLength(0);
        ConfigurationsRootGrid.RowDefinitions[7].Height = showProfiles ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
    }
}
