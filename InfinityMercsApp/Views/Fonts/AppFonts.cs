namespace InfinityMercsApp.Views.Fonts;

/// <summary>
/// The fonts the app ships, from Resources/Fonts. Each is registered with MAUI under its alias, and also embedded
/// in the assembly as <c>Fonts/{FileName}</c> so <see cref="FontTextWidth"/> can measure text in it.
/// </summary>
public static class AppFonts
{
    public static IReadOnlyList<AppFont> All { get; } =
    [
        new("OpenSans-Regular.ttf", "OpenSansRegular"),
        new("OpenSans-Semibold.ttf", "OpenSansSemibold"),
        new("Ethnocentric-Regular.otf", "EthnocentricRegular"),
        new("Conthrax-SemiBold.otf", "ConthraxSemiBold")
    ];
}
