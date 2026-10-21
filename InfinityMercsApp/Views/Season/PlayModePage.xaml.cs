using System.Windows.Input;
using InfinityMercsApp.ViewModels.Season;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season;

public partial class PlayModePage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public PlayModeViewModel ViewModel { get; }

    public PlayModePage(PlayModeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
        TileStripPanCommand = new Command<object>(delta =>
        {
            if (delta is double dx)
            {
                UnitStripScrollView.ScrollToAsync(Math.Max(0, UnitStripScrollView.ScrollX - dx), 0, false);
            }
        });
    }

    /// <summary>Drags the unit strip sideways when a tile is panned.</summary>
    public ICommand TileStripPanCommand { get; }
}
