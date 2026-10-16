using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>A unit row of a company selection list or roster card: logo, name and subtitle. Its BindingContext is an <c>IViewerListItem</c>.</summary>
public partial class UnitSelectionListItemView : ContentView
{
    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(nameof(ItemTappedCommand), typeof(ICommand), typeof(UnitSelectionListItemView));

    public UnitSelectionListItemView()
    {
        InitializeComponent();
    }

    /// <summary>Runs with the row's item when it is tapped.</summary>
    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }
}
