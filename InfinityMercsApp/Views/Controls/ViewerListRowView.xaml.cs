using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>A faction or unit row of the encyclopedia: logo, name and subtitle. Its BindingContext is an <c>IViewerListItem</c>.</summary>
public partial class ViewerListRowView : ContentView
{
    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(nameof(ItemTappedCommand), typeof(ICommand), typeof(ViewerListRowView));

    public ViewerListRowView()
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
