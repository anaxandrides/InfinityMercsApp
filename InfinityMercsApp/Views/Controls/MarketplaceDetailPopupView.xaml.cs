using InfinityMercsApp.ViewModels.Components;

namespace InfinityMercsApp.Views.Controls;

public partial class MarketplaceDetailPopupView : ContentView
{
    public MarketplaceDetailPopupView()
    {
        InitializeComponent();
    }

    /// <summary>The BindingContext as its ViewModel, which item templates bind to through x:Reference.</summary>
    public MarketplaceDetailViewModel? ViewModel => BindingContext as MarketplaceDetailViewModel;

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        OnPropertyChanged(nameof(ViewModel));
    }
}
