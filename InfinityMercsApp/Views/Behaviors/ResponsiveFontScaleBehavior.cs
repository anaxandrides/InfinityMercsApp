namespace InfinityMercsApp.Views.Behaviors;

/// <summary>
/// Enlarges the app's shared font sizes while the page is showing on a wide screen (by up to 35%), and puts
/// them back when the page goes. The sizes are app resources, so every DynamicResource use follows.
/// </summary>
public sealed class ResponsiveFontScaleBehavior : Behavior<ContentPage>
{
    // The scaled resources and their sizes on a phone-width page.
    private static readonly IReadOnlyDictionary<string, double> BaseFontSizes = new Dictionary<string, double>
    {
        ["FontSizeCaption"] = 12.0,
        ["FontSizeBody"] = 14.0,
        ["FontSizeSectionHeader"] = 18.0,
        ["FontSizeSubHeadline"] = 24.0,
        ["FontSizeTitleSmall"] = 22.0,
    };

    // Widths closer than this to the last one scaled for are left alone.
    private const double WidthTolerance = 20;

    private readonly Dictionary<string, object> _originalSizes = [];
    private double _scaledForWidth = -1;

    /// <summary>How much larger the fonts are at a page width.</summary>
    public static double ScaleFor(double width) => width >= 1000 ? 1.35 : width >= 700 ? 1.2 : width >= 500 ? 1.1 : 1.0;

    protected override void OnAttachedTo(ContentPage page)
    {
        base.OnAttachedTo(page);
        page.Appearing += OnAppearing;
        page.Disappearing += OnDisappearing;
        page.SizeChanged += OnSizeChanged;
    }

    protected override void OnDetachingFrom(ContentPage page)
    {
        page.Appearing -= OnAppearing;
        page.Disappearing -= OnDisappearing;
        page.SizeChanged -= OnSizeChanged;
        base.OnDetachingFrom(page);
    }

    private void OnAppearing(object? sender, EventArgs e)
    {
        if (Application.Current?.Resources is { } resources && _originalSizes.Count == 0)
        {
            foreach (var key in BaseFontSizes.Keys)
            {
                if (resources.TryGetValue(key, out var value))
                {
                    _originalSizes[key] = value;
                }
            }
        }

        if (sender is ContentPage page)
        {
            Scale(page.Width);
        }
    }

    private void OnDisappearing(object? sender, EventArgs e)
    {
        if (Application.Current?.Resources is { } resources)
        {
            foreach (var (key, value) in _originalSizes)
            {
                resources[key] = value;
            }
        }

        _scaledForWidth = -1;
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (sender is ContentPage page)
        {
            Scale(page.Width);
        }
    }

    private void Scale(double width)
    {
        if (width <= 0 || _originalSizes.Count == 0 || Math.Abs(width - _scaledForWidth) < WidthTolerance ||
            Application.Current?.Resources is not { } resources)
        {
            return;
        }

        _scaledForWidth = width;
        var scale = ScaleFor(width);
        foreach (var (key, baseSize) in BaseFontSizes)
        {
            resources[key] = Math.Round(baseSize * scale, 1);
        }
    }
}
