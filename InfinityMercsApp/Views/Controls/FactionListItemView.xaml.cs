using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// A single faction tile (centred logo + name) used by the faction strip and the compact faction
/// selector overlay. The logo is shown via <see cref="CachedSvgImage"/>, which binds a native
/// Image to a PNG rasterized once per (icon, size) rather than hosting a live Skia surface.
/// </summary>
public partial class FactionListItemView : ContentView
{
    public event EventHandler? ItemTapped;

    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommand),
            typeof(ICommand),
            typeof(FactionListItemView));

    public static readonly BindableProperty ItemTappedCommandParameterProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommandParameter),
            typeof(object),
            typeof(FactionListItemView));

    public static readonly BindableProperty TitleFormattedProperty =
        BindableProperty.Create(
            nameof(TitleFormatted),
            typeof(FormattedString),
            typeof(FactionListItemView),
            propertyChanged: OnTitleFormattedChanged);

    // Retained for binding compatibility: both call sites set UseVerticalTileLayout="True" in XAML.
    // The control now only renders the vertical tile, so the value is no longer acted on.
    public static readonly BindableProperty UseVerticalTileLayoutProperty =
        BindableProperty.Create(
            nameof(UseVerticalTileLayout),
            typeof(bool),
            typeof(FactionListItemView),
            true);

    public FactionListItemView()
    {
        InitializeComponent();
        UpdateTitleFormattingState(TitleFormatted);
        RefreshTitlePresentation();
    }

    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }

    public object? ItemTappedCommandParameter
    {
        get => GetValue(ItemTappedCommandParameterProperty);
        set => SetValue(ItemTappedCommandParameterProperty, value);
    }

    public FormattedString? TitleFormatted
    {
        get => (FormattedString?)GetValue(TitleFormattedProperty);
        set => SetValue(TitleFormattedProperty, value);
    }

    public bool HasTitleFormatted { get; private set; }

    public bool UseVerticalTileLayout
    {
        get => (bool)GetValue(UseVerticalTileLayoutProperty);
        set => SetValue(UseVerticalTileLayoutProperty, value);
    }

    private static void OnTitleFormattedChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not FactionListItemView view)
        {
            return;
        }

        view.UpdateTitleFormattingState(newValue as FormattedString);
        view.RefreshTitlePresentation();
    }

    private void UpdateTitleFormattingState(FormattedString? formattedTitle)
    {
        var hasFormattedTitle = formattedTitle?.Spans is { Count: > 0 };
        if (HasTitleFormatted == hasFormattedTitle)
        {
            return;
        }

        HasTitleFormatted = hasFormattedTitle;
        OnPropertyChanged(nameof(HasTitleFormatted));
    }

    private void RefreshTitlePresentation()
    {
        VerticalFormattedNameLabel.IsVisible = HasTitleFormatted;
        VerticalNameLabel.IsVisible = !HasTitleFormatted;
    }

    private void OnItemTapped(object? sender, TappedEventArgs e)
    {
        ItemTapped?.Invoke(this, EventArgs.Empty);

        var parameter = ItemTappedCommandParameter ?? BindingContext;
        if (ItemTappedCommand?.CanExecute(parameter) == true)
        {
            ItemTappedCommand.Execute(parameter);
        }
    }
}
