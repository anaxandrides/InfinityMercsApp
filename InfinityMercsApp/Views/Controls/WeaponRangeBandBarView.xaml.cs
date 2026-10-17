using InfinityMercsApp.Domain.Models.Metadata.Ranges;
using InfinityMercsApp.Presentation.Weapons;
using SkiaSharp;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views.Controls;

public partial class WeaponRangeBandBarView : ContentView
{
    public static readonly BindableProperty DistanceProperty =
        BindableProperty.Create(
            nameof(Distance),
            typeof(WeaponDistance),
            typeof(WeaponRangeBandBarView),
            null,
            propertyChanged: (bindable, _, _) => ((WeaponRangeBandBarView)bindable).Canvas.InvalidateSurface());

    public static readonly BindableProperty ShowUnitsInInchesProperty =
        BindableProperty.Create(
            nameof(ShowUnitsInInches),
            typeof(bool),
            typeof(WeaponRangeBandBarView),
            true,
            propertyChanged: (bindable, _, _) => ((WeaponRangeBandBarView)bindable).Canvas.InvalidateSurface());

    public static readonly BindableProperty BarHeightRequestProperty =
        BindableProperty.Create(
            nameof(BarHeightRequest),
            typeof(double),
            typeof(WeaponRangeBandBarView),
            88.0,
            propertyChanged: (bindable, _, newVal) =>
                ((WeaponRangeBandBarView)bindable).Canvas.HeightRequest = (double)newVal);

    public static readonly BindableProperty XVisorActiveProperty =
        BindableProperty.Create(
            nameof(XVisorActive),
            typeof(bool),
            typeof(WeaponRangeBandBarView),
            false,
            propertyChanged: (bindable, _, _) => ((WeaponRangeBandBarView)bindable).Canvas.InvalidateSurface());

    public WeaponRangeBandBarView()
    {
        InitializeComponent();
    }

    public WeaponDistance? Distance
    {
        get => (WeaponDistance?)GetValue(DistanceProperty);
        set => SetValue(DistanceProperty, value);
    }

    public bool ShowUnitsInInches
    {
        get => (bool)GetValue(ShowUnitsInInchesProperty);
        set => SetValue(ShowUnitsInInchesProperty, value);
    }

    public double BarHeightRequest
    {
        get => (double)GetValue(BarHeightRequestProperty);
        set => SetValue(BarHeightRequestProperty, value);
    }

    public bool XVisorActive
    {
        get => (bool)GetValue(XVisorActiveProperty);
        set => SetValue(XVisorActiveProperty, value);
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        var bar = RangeBar.From(Distance, XVisorActive, ShowUnitsInInches);
        if (bar.Bands.Count == 0)
            return;

        float canvasWidth = e.Info.Width;
        float canvasHeight = e.Info.Height;
        float density = (float)DeviceDisplay.MainDisplayInfo.Density;
        float cornerRadius = 4f * density;

        // Split: top 65% colored bands, bottom 35% white label strip
        float labelBarHeight = canvasHeight * 0.35f;
        float coloredHeight = canvasHeight - labelBarHeight;

        // Clip the entire drawing to a rounded rect so corners are handled automatically
        using var clipPath = new SKPath();
        clipPath.AddRoundRect(new SKRect(0, 0, canvasWidth, canvasHeight), cornerRadius, cornerRadius);
        canvas.Save();
        canvas.ClipPath(clipPath, SKClipOperation.Intersect, antialias: true);

        using var fillPaint = new SKPaint { IsAntialias = true };
        using var modPaint = new SKPaint { IsAntialias = true, Color = SKColors.Black };
        using var whitePaint = new SKPaint { Color = SKColors.White };
        using var dividerPaint = new SKPaint { Color = new SKColor(180, 180, 180), StrokeWidth = 1f };
        using var labelPaint = new SKPaint { IsAntialias = true, Color = SKColors.Black };

        using var modFont = new SKFont(SKTypeface.Default, coloredHeight * 0.52f) { Embolden = true };
        using var labelFont = new SKFont(SKTypeface.Default, labelBarHeight * 0.58f);

        // Colored bands, each with its modifier centred
        foreach (var band in bar.Bands)
        {
            float left = (float)band.StartFraction * canvasWidth;
            float right = (float)band.EndFraction * canvasWidth;
            fillPaint.Color = ColorOf(band.Tone);
            canvas.DrawRect(new SKRect(left, 0, right, coloredHeight), fillPaint);
            canvas.DrawText(band.Modifier, (left + right) / 2f, coloredHeight / 2f + modFont.Size * 0.38f, SKTextAlign.Center, modFont, modPaint);
        }

        // White label strip
        canvas.DrawRect(new SKRect(0, coloredHeight, canvasWidth, canvasHeight), whitePaint);

        // Thin dividers between bands, and the distance at each band edge
        float textY = coloredHeight + labelBarHeight / 2f + labelFont.Size * 0.38f;
        float edgePadding = 3f * density;
        var lastMark = bar.Marks.Count - 1;
        for (int i = 0; i <= lastMark; i++)
        {
            var mark = bar.Marks[i];
            float markX = (float)mark.Fraction * canvasWidth;
            if (i > 0 && i < lastMark)
                canvas.DrawLine(markX, 0, markX, coloredHeight, dividerPaint);

            // Number alignment: left-edge → left-align, right-edge → right-align, interior → center
            var (align, drawX) = i == 0
                ? (SKTextAlign.Left, markX + edgePadding)
                : i == lastMark
                    ? (SKTextAlign.Right, markX - edgePadding)
                    : (SKTextAlign.Center, markX);
            canvas.DrawText(mark.Label, drawX, textY, align, labelFont, labelPaint);
        }

        canvas.Restore();
    }

    private static SKColor ColorOf(RangeBandTone tone) => tone switch
    {
        RangeBandTone.Bonus => new SKColor(34, 197, 94),    // green
        RangeBandTone.Penalty => new SKColor(245, 158, 11), // amber
        RangeBandTone.Severe => new SKColor(220, 38, 38),   // red
        _ => new SKColor(107, 114, 128)                     // grey
    };
}
