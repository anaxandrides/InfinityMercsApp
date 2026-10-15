using InfinityMercsApp.ViewModels.Season.Game;

namespace InfinityMercsApp.Views.Season;

public partial class GameModePage : ContentPageBase
{
    public GameModePage(GameModeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
