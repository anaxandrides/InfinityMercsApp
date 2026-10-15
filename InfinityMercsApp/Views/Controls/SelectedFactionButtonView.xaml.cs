using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Compact-screen header button that shows the logo(s) of the chosen faction slot(s) and runs
/// <see cref="Command"/> when tapped, to reopen the faction selector. Mirrors the title-bar
/// <see cref="FactionSlotSelectorView"/>, which compact screens hide.
/// </summary>
public partial class SelectedFactionButtonView : ContentView
{
    public static readonly BindableProperty ShowRightSlotProperty =
        BindableProperty.Create(nameof(ShowRightSlot), typeof(bool), typeof(SelectedFactionButtonView), false);

    public static readonly BindableProperty LeftSlotSourceProperty =
        BindableProperty.Create(nameof(LeftSlotSource), typeof(string), typeof(SelectedFactionButtonView), propertyChanged: OnSlotSourceChanged);

    public static readonly BindableProperty RightSlotSourceProperty =
        BindableProperty.Create(nameof(RightSlotSource), typeof(string), typeof(SelectedFactionButtonView), propertyChanged: OnSlotSourceChanged);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(SelectedFactionButtonView));

    public SelectedFactionButtonView()
    {
        InitializeComponent();
    }

    public bool ShowRightSlot
    {
        get => (bool)GetValue(ShowRightSlotProperty);
        set => SetValue(ShowRightSlotProperty, value);
    }

    public string? LeftSlotSource
    {
        get => (string?)GetValue(LeftSlotSourceProperty);
        set => SetValue(LeftSlotSourceProperty, value);
    }

    public string? RightSlotSource
    {
        get => (string?)GetValue(RightSlotSourceProperty);
        set => SetValue(RightSlotSourceProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>"Tap to choose faction" until a faction is chosen.</summary>
    public string PromptText =>
        string.IsNullOrWhiteSpace(LeftSlotSource) && string.IsNullOrWhiteSpace(RightSlotSource) ? "Tap to choose faction" : string.Empty;

    private static void OnSlotSourceChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((SelectedFactionButtonView)bindable).OnPropertyChanged(nameof(PromptText));
}
