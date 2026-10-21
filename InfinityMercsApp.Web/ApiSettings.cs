namespace InfinityMercsApp.Web;

/// <summary>The "Api" section of wwwroot/appsettings.json: where the API is, and the scope its tokens need.</summary>
public sealed class ApiSettings
{
    public string BaseAddress { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;
}
