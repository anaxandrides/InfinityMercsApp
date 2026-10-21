using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// The left and right faction slots of a company: each shows its faction's logo, the slot the next pick goes
/// to is outlined, and tapping a slot runs <see cref="SlotTappedCommand"/> with its index (0 or 1).
/// </summary>
public partial class FactionSlotSelectorView : ContentView
{
    public static readonly BindableProperty ShowRightSelectionBoxProperty =
        BindableProperty.Create(nameof(ShowRightSelectionBox), typeof(bool), typeof(FactionSlotSelectorView), false);

    public static readonly BindableProperty ActiveSlotIndexProperty =
        BindableProperty.Create(nameof(ActiveSlotIndex), typeof(int), typeof(FactionSlotSelectorView), 0);

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
}
