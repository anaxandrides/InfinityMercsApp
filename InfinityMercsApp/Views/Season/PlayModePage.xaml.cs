using System.Windows.Input;
using InfinityMercsApp.ViewModels.Season;

namespace InfinityMercsApp.Views.Season;

public partial class PlayModePage : ContentPageBase
{
    public PlayModePage(PlayModeViewModel viewModel)
    {
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
