using System.Globalization;
using InfinityMercsApp.Converters;
using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.UnitTests.Converters;

public sealed class AlignmentConverterTests
{
    [Fact]
    public void Convert_ToLayoutOptions_MapsEachAlignment()
    {
        var converter = new AlignmentConverter();

        Assert.Equal(LayoutOptions.Start, converter.Convert(Alignment.Start, typeof(LayoutOptions), null, CultureInfo.InvariantCulture));
        Assert.Equal(LayoutOptions.Center, converter.Convert(Alignment.Center, typeof(LayoutOptions), null, CultureInfo.InvariantCulture));
        Assert.Equal(LayoutOptions.End, converter.Convert(Alignment.End, typeof(LayoutOptions), null, CultureInfo.InvariantCulture));
        Assert.Equal(LayoutOptions.Fill, converter.Convert(Alignment.Fill, typeof(LayoutOptions), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Convert_ToTextAlignment_MapsCenterAndStart()
    {
        var converter = new AlignmentConverter();

        Assert.Equal(TextAlignment.Center, converter.Convert(Alignment.Center, typeof(TextAlignment), null, CultureInfo.InvariantCulture));
        Assert.Equal(TextAlignment.Start, converter.Convert(Alignment.Start, typeof(TextAlignment), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void InsetConverter_Inset_IsTheSameThickness() =>
        Assert.Equal(new Thickness(24, 0, 24, 40), new InsetConverter().Convert(new Inset(24, 0, 24, 40), typeof(Thickness), null, CultureInfo.InvariantCulture));
}
