using InfinityMercsApp.Services.CompanySelection.Factions;

namespace InfinityMercsApp.Presentation.Themes;

/// <summary>The header and summary colours of a faction's units.</summary>
internal static class FactionColors
{
    /// <summary>The faction's header colours, or the default red for an unknown faction.</summary>
    internal static CompanyHeaderColors GetHeaderColors(string? vanillaFactionName)
    {
        var (primary, secondary) = GetFactionTheme(vanillaFactionName, CompanyHeaderColors.DefaultPrimary, CompanyHeaderColors.DefaultSecondary);
        var primaryText = IsLightColor(primary) ? Colors.Black : Colors.White;
        var secondaryText = IsLightColor(secondary) ? Colors.Black : Colors.White;
        return new CompanyHeaderColors(primary, secondary, primaryText, secondaryText);
    }

    internal static SummaryAccents GetSummaryAccentColorsForSecondaryBackground(Color secondaryBackground)
    {
        return IsLightColor(secondaryBackground) ? SummaryAccents.OnLightBackground : SummaryAccents.OnDarkBackground;
    }

    // Faction colours by normalized vanilla faction name.
    private static readonly IReadOnlyDictionary<string, FactionTheme> FactionThemes = new Dictionary<string, FactionTheme>
    {
        ["panoceania"] = new(Color.FromArgb("#239ac2"), Color.FromArgb("#006a91")),
        ["yujing"] = new(Color.FromArgb("#ff9000"), Color.FromArgb("#995601")),
        ["ariadna"] = new(Color.FromArgb("#007d27"), Color.FromArgb("#005825")),
        ["haqqislam"] = new(Color.FromArgb("#e6da9b"), Color.FromArgb("#8a835d")),
        ["nomads"] = new(Color.FromArgb("#ce181e"), Color.FromArgb("#7c0e13")),
        ["combinedarmy"] = new(Color.FromArgb("#400b5f"), Color.FromArgb("#260739")),
        ["aleph"] = new(Color.FromArgb("#aea6bb"), Color.FromArgb("#696471")),
        ["tohaa"] = new(Color.FromArgb("#3b3b3b"), Color.FromArgb("#252525")),
        ["nonalignedarmy"] = new(Color.FromArgb("#728868"), Color.FromArgb("#728868")),
        ["o12"] = new(Color.FromArgb("#005470"), Color.FromArgb("#dead33")),
        ["jsa"] = new(Color.FromArgb("#a6112b"), Color.FromArgb("#757575"))
    };

    /// <summary>The faction's colours, or the defaults for factions without a theme.</summary>
    internal static FactionTheme GetFactionTheme(string? factionName, Color defaultPrimary, Color defaultSecondary) =>
        FactionThemes.TryGetValue(ThemeFactions.Key(factionName), out var theme)
            ? theme
            : new FactionTheme(defaultPrimary, defaultSecondary);

    // Light enough that black text reads better on it than white.
    private static bool IsLightColor(Color color)
    {
        var luminance = (0.299 * color.Red) + (0.587 * color.Green) + (0.114 * color.Blue);
        return luminance >= 0.6;
    }
}
