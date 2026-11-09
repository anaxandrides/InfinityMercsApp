namespace InfinityMercsApp.Domain.Leagues;

/// <summary>The web addresses that lead into a league.</summary>
public static class LeagueLinks
{
    /// <summary>The web site, which anyone can open from a link or a scanned QR code.</summary>
    public static Uri Site { get; } = new("https://www.infinitythemercs.com/");

    /// <summary>The page that joins the league with <paramref name="code"/>.</summary>
    public static string Join(string code) => new Uri(Site, $"join/{Uri.EscapeDataString(code)}").ToString();
}
