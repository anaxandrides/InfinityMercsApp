using InfinityMercsApp.ViewModels.Season.Loot;
using InfinityMercsApp.Views.Base;

namespace InfinityMercsApp.Views.Season.Loot;

public partial class LootBoxPage : ContentPageBase
{
    /// <summary>The page's ViewModel, which item templates bind to through x:Reference.</summary>
    public LootBoxViewModel ViewModel { get; }

    public LootBoxPage(LootBoxViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        BindingContext = viewModel;
    }
}
