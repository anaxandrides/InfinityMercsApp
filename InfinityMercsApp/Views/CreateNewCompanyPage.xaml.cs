using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Adaptive;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views;

public partial class CreateNewCompanyPage
{
    public CreateNewCompanyPage(CreateNewCompanyPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        ApplyLayout();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await InitializeViewModelAsync();

        ClearResidualNavigationStack();
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    // Icon canvas size and card height for the two card layouts. Phones (Compact) lay the icon and
    // title out side-by-side in a short row; wider screens stack the large icon above the title.
    private const double CompactIconSize = 56d;
    private const double ExpandedIconSize = 120d;
    private const double CompactCardHeight = 72d;
    private const double ExpandedCardHeight = 160d;

    private void ApplyLayout()
    {
        // 1 column on phones, 2 on tablet portrait, 3 on larger screens.
        var cols = IsCompact ? 1 : IsMedium ? 2 : 3;

        // Visible company choices, in display order. Lone Wolf stays hidden and is excluded.
        var cards = new[] { CardStandard, CardCohesive, CardInspiring, CardAirborne, CardTag };
        var rows = (int)Math.Ceiling(cards.Length / (double)cols);

        CardGrid.ColumnDefinitions = [.. Enumerable.Range(0, cols).Select(_ => new ColumnDefinition(GridLength.Star))];
        CardGrid.RowDefinitions = [.. Enumerable.Range(0, rows).Select(_ => new RowDefinition(GridLength.Auto))];

        for (var i = 0; i < cards.Length; i++)
        {
            Grid.SetRow(cards[i], i / cols);
            Grid.SetColumn(cards[i], i % cols);
            cards[i].MinimumHeightRequest = IsCompact ? CompactCardHeight : ExpandedCardHeight;
        }

        ConfigureCardContent(StandardCardContent, StandardCompanyIcon, StandardCompanyLabel);
        ConfigureCardContent(CohesiveCardContent, CohesiveCompanyIcon, CohesiveCompanyLabel);
        ConfigureCardContent(InspiringCardContent, InspiringLeaderIcon, InspiringLeaderLabel);
        ConfigureCardContent(AirborneCardContent, AirborneCompanyIcon, AirborneCompanyLabel);
        ConfigureCardContent(TagCardContent, TagCompanyIcon, TagCompanyLabel);

        CardGrid.HorizontalOptions = IsCompact ? LayoutOptions.Fill : LayoutOptions.Center;
        CardGrid.MaximumWidthRequest = IsCompact ? double.PositiveInfinity : cols == 2 ? 760d : 1100d;
    }

    /// <summary>
    /// Reflows a single company card's interior. On phones (Compact) the icon sits on the left with
    /// the title beside it on the right in a short row; on wider screens the large icon stacks above
    /// the centred title.
    /// </summary>
    private void ConfigureCardContent(Grid content, SvgIcon icon, Label label)
    {
        if (IsCompact)
        {
            content.RowDefinitions = [new RowDefinition(GridLength.Auto)];
            content.ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            ];
            content.RowSpacing = 0;
            content.ColumnSpacing = 12;

            Grid.SetRow(icon, 0);
            Grid.SetColumn(icon, 0);
            icon.WidthRequest = CompactIconSize;
            icon.HeightRequest = CompactIconSize;
            icon.VerticalOptions = LayoutOptions.Center;
            icon.HorizontalOptions = LayoutOptions.Center;

            Grid.SetRow(label, 0);
            Grid.SetColumn(label, 1);
            label.VerticalOptions = LayoutOptions.Center;
            label.HorizontalOptions = LayoutOptions.Start;
            label.HorizontalTextAlignment = TextAlignment.Start;
            label.SetDynamicResource(Label.FontSizeProperty, "FontSizeCardTitleCompact");
        }
        else
        {
            content.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            content.RowDefinitions =
            [
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            ];
            content.ColumnSpacing = 0;
            content.RowSpacing = 6;

            Grid.SetColumn(icon, 0);
            Grid.SetRow(icon, 0);
            icon.WidthRequest = ExpandedIconSize;
            icon.HeightRequest = ExpandedIconSize;
            icon.VerticalOptions = LayoutOptions.Center;
            icon.HorizontalOptions = LayoutOptions.Center;

            Grid.SetColumn(label, 0);
            Grid.SetRow(label, 1);
            label.VerticalOptions = LayoutOptions.Start;
            label.HorizontalOptions = LayoutOptions.Center;
            label.HorizontalTextAlignment = TextAlignment.Center;
            label.SetDynamicResource(Label.FontSizeProperty, "FontSizeHeadline");
        }
    }

    private void ClearResidualNavigationStack()
    {
        var navigation = Navigation;
        var stack = navigation?.NavigationStack;
        if (stack is null || stack.Count <= 1)
        {
            return;
        }

        // Keep only this page so "Create New Company" always starts from a clean state.
        var pagesToRemove = stack.Where(page => !ReferenceEquals(page, this)).ToList();
        foreach (var page in pagesToRemove)
        {
            navigation?.RemovePage(page);
        }
    }
}
