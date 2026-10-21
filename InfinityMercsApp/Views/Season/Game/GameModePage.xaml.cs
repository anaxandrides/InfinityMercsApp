using InfinityMercsApp.ViewModels.Season.Game;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.Game;

public partial class GameModePage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public GameModeViewModel ViewModel { get; }

    public GameModePage(GameModeViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
}
