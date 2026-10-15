using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// The left and right faction slots of a company: each shows its faction's logo, the slot the next pick goes
/// to is outlined, and tapping a slot runs <see cref="SlotTappedCommand"/> with its index (0 or 1).
/// </summary>
public partial class FactionSlotSelectorView : ContentView
{
    private static readonly Color ActiveBorder = Color.FromArgb("#34D399");
    private static readonly Color InactiveBorder = Color.FromArgb("#3A4554");

    public static readonly BindableProperty ShowRightSelectionBoxProperty =
        BindableProperty.Create(nameof(ShowRightSelectionBox), typeof(bool), typeof(FactionSlotSelectorView), false, propertyChanged: OnSlotStateChanged);

    public static readonly BindableProperty ActiveSlotIndexProperty =
        BindableProperty.Create(nameof(ActiveSlotIndex), typeof(int), typeof(FactionSlotSelectorView), 0, propertyChanged: OnSlotStateChanged);

    public static readonly BindableProperty LeftSlotSourceProperty =
        BindableProperty.Create(nameof(LeftSlotSource), typeof(string), typeof(FactionSlotSelectorView));

    public static readonly BindableProperty RightSlotSourceProperty =
        BindableProperty.Create(nameof(RightSlotSource), typeof(string), typeof(FactionSlotSelectorView));

    public static readonly BindableProperty SlotTappedCommandProperty =
        BindableProperty.Create(nameof(SlotTappedCommand), typeof(ICommand), typeof(FactionSlotSelectorView));

    public FactionSlotSelectorView()
    {
        InitializeComponent();
    }

    public bool ShowRightSelectionBox
    {
        get => (bool)GetValue(ShowRightSelectionBoxProperty);
        set => SetValue(ShowRightSelectionBoxProperty, value);
    }

    public int ActiveSlotIndex
    {
        get => (int)GetValue(ActiveSlotIndexProperty);
        set => SetValue(ActiveSlotIndexProperty, value);
    }

    /// <summary>The left slot's logo: a downloaded file or a packaged asset path.</summary>
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

    public ICommand? SlotTappedCommand
    {
        get => (ICommand?)GetValue(SlotTappedCommandProperty);
        set => SetValue(SlotTappedCommandProperty, value);
    }

    public Color LeftSlotBorderColor => IsRightSlotActive ? InactiveBorder : ActiveBorder;

    public Color RightSlotBorderColor => IsRightSlotActive ? ActiveBorder : InactiveBorder;

    private bool IsRightSlotActive => ActiveSlotIndex == 1 && ShowRightSelectionBox;

    private static void OnSlotStateChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (FactionSlotSelectorView)bindable;
        view.OnPropertyChanged(nameof(LeftSlotBorderColor));
        view.OnPropertyChanged(nameof(RightSlotBorderColor));
    }
}
