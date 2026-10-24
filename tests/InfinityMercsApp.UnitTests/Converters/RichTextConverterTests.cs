using System.Globalization;
using InfinityMercsApp.Converters;
using InfinityMercsApp.Presentation.Text;

namespace InfinityMercsApp.UnitTests.Converters;

public sealed class RichTextConverterTests
{
    private static FormattedString Convert(RichText text) =>
        Assert.IsType<FormattedString>(new RichTextConverter().Convert(text, typeof(FormattedString), null, CultureInfo.InvariantCulture));

    [Fact]
    public void Convert_Runs_MakesASpanForEachWithItsColor()
    {
        var red = Color.FromArgb("#EF4444");

        var formatted = Convert(new RichText([new RichTextRun("Combi Rifle", red), new RichTextRun(", ")]));

        Assert.Equal(["Combi Rifle", ", "], formatted.Spans.Select(span => span.Text));
        Assert.Equal(red, formatted.Spans[0].TextColor);
    }

    [Fact]
    public void Convert_LinkedRun_OpensItWhenTapped()
    {
        var formatted = Convert(new RichText([new RichTextRun("Mimetism", Link: "https://wiki/mimetism")]));

        var tap = Assert.IsType<TapGestureRecognizer>(Assert.Single(formatted.Spans[0].GestureRecognizers));
        Assert.Equal("https://wiki/mimetism", tap.CommandParameter);
    }

    [Fact]
    public void Convert_EntryGap_IsATinyLine()
    {
        var formatted = Convert(new RichText([new RichTextRun("A"), RichTextRun.EntryGap, new RichTextRun("B")]));

        Assert.Equal(2, formatted.Spans[1].FontSize);
    }

    [Fact]
    public void Convert_NotRichText_IsEmpty() =>
        Assert.Empty(Assert.IsType<FormattedString>(new RichTextConverter().Convert(null, typeof(FormattedString), null, CultureInfo.InvariantCulture)).Spans);
}
