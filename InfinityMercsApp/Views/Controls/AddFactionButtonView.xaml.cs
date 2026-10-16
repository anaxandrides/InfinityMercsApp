using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>The round plus/minus button that opens and closes the faction strip.</summary>
public partial class AddFactionButtonView : ContentView
{
    public static readonly BindableProperty IsExpandedProperty =
        BindableProperty.Create(nameof(IsExpanded), typeof(bool), typeof(AddFactionButtonView), true);

    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(AddFactionButtonView));

    public AddFactionButtonView()
    {
        InitializeComponent();
    }

    /// <summary>Runs when the button is tapped, e.g. to open or close the faction strip.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>True while the strip is open: the button shows a minus, otherwise a plus.</summary>
    public bool IsExpanded
    {
        get => (bool)GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }
}
