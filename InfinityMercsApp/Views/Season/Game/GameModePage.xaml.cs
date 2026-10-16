using InfinityMercsApp.ViewModels.Season.Game;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.Game;

public partial class GameModePage : ContentPageBase
{
    public GameModePage(GameModeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
