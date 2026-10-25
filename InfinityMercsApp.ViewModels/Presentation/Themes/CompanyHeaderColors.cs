namespace InfinityMercsApp.Presentation.Themes;

/// <summary>The unit header's two bands and the text colour that reads on each.</summary>
public readonly record struct CompanyHeaderColors(Color Primary, Color Secondary, Color PrimaryText, Color SecondaryText)
{
    public static readonly Color DefaultPrimary = Color.FromArgb("#B91C1C");
    public static readonly Color DefaultSecondary = Color.FromArgb("#7F1D1D");

    /// <summary>The red header shown before a faction is known.</summary>
    public static CompanyHeaderColors Default { get; } = new(DefaultPrimary, DefaultSecondary, Colors.White, Colors.White);
}
