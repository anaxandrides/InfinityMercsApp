namespace InfinityMercsApp.ApiClient;

/// <summary>Where the app's own API is served.</summary>
public static class ApiAddress
{
    /// <summary>The API that infra/main.bicep deploys: its root, ending in a slash.</summary>
    public static Uri Production { get; } = new("https://func-infmercs-prod.azurewebsites.net/api/");
}
