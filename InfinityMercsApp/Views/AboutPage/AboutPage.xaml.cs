using InfinityMercsApp.ViewModels;

namespace InfinityMercsApp.Views;

/// <summary>About/attribution page. Links are opened by <see cref="AboutPageViewModel"/>.</summary>
public partial class AboutPage : ContentPageBase
{
    public AboutPage(AboutPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        UpdateAttributionColumns(width);
    }

    // Two attribution columns on screens wider than 500.
    private void UpdateAttributionColumns(double pageWidth)
    {
        if (AttributionEntriesLayout is null)
        {
            return;
        }

        var twoColumns = pageWidth > 500;
        var targetWidth = twoColumns ? Math.Max(0d, (pageWidth - 48d - 16d) / 2d) : -1d;

        foreach (var child in AttributionEntriesLayout.Children)
        {
            if (child is not View view)
            {
                continue;
            }

            view.WidthRequest = targetWidth;
            FlexLayout.SetGrow(view, 0f);
            FlexLayout.SetShrink(view, 0f);
        }
    }
}
