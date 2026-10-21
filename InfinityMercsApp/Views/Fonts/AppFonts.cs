namespace InfinityMercsApp.Views.Fonts;

/// <summary>
/// The fonts the app ships, from Resources/Fonts: Exo 2, the Infinity Army's own face, under the SIL Open Font
/// License (Resources/Fonts/Exo2-OFL.txt). Each is registered with MAUI under its alias, and also embedded
/// in the assembly as <c>Fonts/{FileName}</c> so <see cref="FontTextWidth"/> can measure text in it.
/// </summary>
public static class AppFonts
{
    public static IReadOnlyList<AppFont> All { get; } =
    [
        new("Exo2-Regular.ttf", "Exo2Regular"),
        new("Exo2-SemiBold.ttf", "Exo2SemiBold"),
        new("Exo2-Bold.ttf", "Exo2Bold")
    ];
}
