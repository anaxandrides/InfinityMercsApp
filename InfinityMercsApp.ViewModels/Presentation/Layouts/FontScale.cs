namespace InfinityMercsApp.Presentation.Layouts;

/// <summary>
/// The app's shared font sizes for a window's size. They follow the window's shorter side, so a phone, a tablet,
/// a laptop and a wide monitor each get text in proportion to the room they have, and a window resized on a
/// desktop follows too. Rows packed with many columns, such as stat lines, also fit their text to their cells.
/// </summary>
public static class FontScale
{
    // The shorter side of a typical phone, where the base sizes apply.
    private const double PhoneShortSide = 400;

    // From the phone's size up to this one, text grows to the largest scale.
    private const double LargeShortSide = 1000;

    private const double SmallestScale = 0.9;
    private const double LargestScale = 1.25;

    /// <summary>The shared sizes, as on a phone.</summary>
    public static IReadOnlyList<FontSizeToken> Tokens { get; } =
    [
        new("FontSizeMicro", 10),
        new("FontSizeCaption", 11),
        new("FontSizeBody", 13),
        new("FontSizeCardTitleCompact", 15),
        new("FontSizeSectionHeader", 16),
        new("FontSizeSubHeadline", 18),
        new("FontSizeTitleSmall", 20),
        new("FontSizeHeadline", 24),
        new("FontSizeTitleLarge", 26),
        new("FontSizeDisplay", 28)
    ];

    /// <summary>
    /// How much larger than on a phone text is in a window of this size: smaller on a narrow phone, growing to a
    /// quarter larger once the shorter side reaches 1000.
    /// </summary>
    public static double For(double width, double height)
    {
        var shortSide = Math.Min(width, height);
        if (shortSide <= 0)
        {
            return 1;
        }

        if (shortSide < PhoneShortSide)
        {
            return Math.Max(SmallestScale, shortSide / PhoneShortSide);
        }

        var growth = Math.Min(1, (shortSide - PhoneShortSide) / (LargeShortSide - PhoneShortSide));
        return 1 + ((LargestScale - 1) * growth);
    }

    /// <summary>A token's size at a scale, to the half point.</summary>
    public static double SizeOf(FontSizeToken token, double scale) => Math.Round(token.BaseSize * scale * 2, MidpointRounding.AwayFromZero) / 2;
}
