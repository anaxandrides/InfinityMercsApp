using InfinityMercsApp.Presentation.Layouts;

namespace InfinityMercsApp.UnitTests.Presentation.Layouts;

/// <summary>Text follows the window's shorter side, whichever way round the window is.</summary>
public sealed class FontScaleTests
{
    public static TheoryData<double, double, double> Windows => new()
    {
        { 360, 780, 0.9 },      // a narrow phone
        { 412, 915, 1.0 },      // a typical phone
        { 915, 412, 1.0 },      // the same phone turned sideways
        { 800, 1280, 1.17 },    // a tablet
        { 1280, 720, 1.13 },    // a 14" laptop at 150%
        { 2560, 1440, 1.25 },   // a wide monitor
        { 0, 0, 1 }             // before the window has a size
    };

    [Theory]
    [MemberData(nameof(Windows))]
    public void FollowsTheShorterSide(double width, double height, double scale) =>
        Assert.Equal(scale, FontScale.For(width, height), 2);

    [Fact]
    public void SizesRoundToTheHalfPoint()
    {
        var body = FontScale.Tokens.Single(token => token.ResourceKey == "FontSizeBody");

        Assert.Equal(13, FontScale.SizeOf(body, 1));
        Assert.Equal(14.5, FontScale.SizeOf(body, 1.13));
    }

    [Fact]
    public void EverySharedSizeIsScaled() =>
        Assert.Equal(
            ["FontSizeMicro", "FontSizeCaption", "FontSizeBody", "FontSizeCardTitleCompact", "FontSizeSectionHeader",
             "FontSizeSubHeadline", "FontSizeTitleSmall", "FontSizeHeadline", "FontSizeTitleLarge", "FontSizeDisplay"],
            FontScale.Tokens.Select(token => token.ResourceKey));
}
