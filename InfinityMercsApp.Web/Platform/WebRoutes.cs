namespace InfinityMercsApp.Web.Platform;

/// <summary>Where the site shows each page the shared view models navigate to by its MAUI route.</summary>
public static class WebRoutes
{
    public const string Home = "";
    public const string NewCompany = "companies/new";
    public const string CompanySource = "companies/new/source";
    public const string CompanySelection = "companies/new/build";
    public const string Season = "season";

    /// <summary>A season by its id, to manage its company.</summary>
    public static string SeasonById(string seasonIdentifier) => $"season/{Uri.EscapeDataString(seasonIdentifier)}";

    /// <summary>The site's address for a MAUI route, e.g. "//CreateNewCompanyPage"; null for a page the site doesn't have.</summary>
    public static string? For(string route) => route.TrimStart('/') switch
    {
        "MercsSeasonPage" or "LoadCompanyPage" or "LoadSeasonPage" => Home,
        "SeasonPage" => Season,
        "CreateNewCompanyPage" => NewCompany,
        "StandardCompanySourcePopupPage" => CompanySource,
        _ => null
    };
}
