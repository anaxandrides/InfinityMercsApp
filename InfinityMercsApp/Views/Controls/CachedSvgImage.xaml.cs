using InfinityMercsApp.Services.Logos;
using InfinityMercsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Displays a logo SVG as a cached PNG through <see cref="ISvgRasterCache"/>. Replaces the
/// <c>SKCanvasView</c> + PaintSurface pattern in item templates: the SVG is rasterized once per
/// (icon, pixel size) and this control simply binds a native <see cref="Image"/> to the result.
/// A version guard drops the late result of a recycled cell whose binding has since moved on.
/// </summary>
public partial class CachedSvgImage : ContentView
{
    private ISvgRasterCache? _rasterCache;
    private int _version;

    public static readonly BindableProperty ItemProperty =
        BindableProperty.Create(
            nameof(Item),
            typeof(IViewerListItem),
            typeof(CachedSvgImage),
            propertyChanged: OnSourceInputChanged);

    public static readonly BindableProperty SvgPathProperty =
        BindableProperty.Create(
            nameof(SvgPath),
            typeof(string),
            typeof(CachedSvgImage),
            propertyChanged: OnSourceInputChanged);

    public static readonly BindableProperty LogicalWidthProperty =
        BindableProperty.Create(
            nameof(LogicalWidth),
            typeof(double),
            typeof(CachedSvgImage),
            0d,
            propertyChanged: OnSourceInputChanged);

    public static readonly BindableProperty LogicalHeightProperty =
        BindableProperty.Create(
            nameof(LogicalHeight),
            typeof(double),
            typeof(CachedSvgImage),
            0d,
            propertyChanged: OnSourceInputChanged);

    public CachedSvgImage()
    {
        InitializeComponent();
    }

    /// <summary>The viewer item whose logo to display; resolved via <see cref="LogoPathResolver"/>.</summary>
    public IViewerListItem? Item
    {
        get => (IViewerListItem?)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    /// <summary>An explicit SVG path/logical name (takes precedence over <see cref="Item"/>).</summary>
    public string? SvgPath
    {
        get => (string?)GetValue(SvgPathProperty);
        set => SetValue(SvgPathProperty, value);
    }

    /// <summary>Logical render width; falls back to <see cref="VisualElement.WidthRequest"/> if unset.</summary>
    public double LogicalWidth
    {
        get => (double)GetValue(LogicalWidthProperty);
        set => SetValue(LogicalWidthProperty, value);
    }

    /// <summary>Logical render height; falls back to <see cref="VisualElement.HeightRequest"/> if unset.</summary>
    public double LogicalHeight
    {
        get => (double)GetValue(LogicalHeightProperty);
        set => SetValue(LogicalHeightProperty, value);
    }

    private ISvgRasterCache? RasterCache =>
        _rasterCache ??= IPlatformApplication.Current?.Services?.GetService<ISvgRasterCache>();

    private static void OnSourceInputChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((CachedSvgImage)bindable).QueueReload();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        // Recycled cells re-use the same control instance; reload so a fast rebind picks up the
        // new binding even if the resolved Item reference happens to compare equal.
        QueueReload();
    }

    private void QueueReload() => _ = ReloadAsync();

    private async Task ReloadAsync()
    {
        var version = Interlocked.Increment(ref _version);

        var width = LogicalWidth > 0 ? LogicalWidth : (WidthRequest > 0 ? WidthRequest : 0);
        var height = LogicalHeight > 0 ? LogicalHeight : (HeightRequest > 0 ? HeightRequest : 0);

        var svgPath = SvgPath;
        if (string.IsNullOrWhiteSpace(svgPath) && Item is { } item)
        {
            svgPath = await LogoPathResolver.ResolveBestSvgPathAsync(item).ConfigureAwait(false);
        }

        if (version != _version)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(svgPath) || width <= 0 || height <= 0)
        {
            ApplySource(version, null);
            return;
        }

        string? pngPath = null;
        var cache = RasterCache;
        if (cache is not null)
        {
            pngPath = await cache.GetRasterPathAsync(svgPath, width, height).ConfigureAwait(false);
        }

        ApplySource(version, pngPath);
    }

    private void ApplySource(int version, string? pngPath)
    {
        void Apply()
        {
            // A newer reload superseded this one (recycled cell / fast rebind); drop the result.
            if (version != _version)
            {
                return;
            }

            InnerImage.Source = string.IsNullOrWhiteSpace(pngPath)
                ? null
                : ImageSource.FromFile(pngPath);
        }

        if (MainThread.IsMainThread)
        {
            Apply();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(Apply);
        }
    }
}
