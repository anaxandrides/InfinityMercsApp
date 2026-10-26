using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>The site's navigation, at https://site.example/, keeping where it was sent.</summary>
internal sealed class TestNavigation : NavigationManager
{
    public TestNavigation() => Initialize("https://site.example/", "https://site.example/");

    public List<string> Visited { get; } = [];

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        Uri = ToAbsoluteUri(uri).ToString();
        Visited.Add(Uri);
    }
}
