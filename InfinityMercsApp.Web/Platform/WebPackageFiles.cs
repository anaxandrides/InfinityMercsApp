using System.Net;
using InfinityMercsApp.Services.Packaging;
using Microsoft.AspNetCore.Components;

namespace InfinityMercsApp.Web.Platform;

/// <summary>
/// The files the app bundles (icons, stores, the glossary), which the site serves under package/. The build lists
/// them in package/files.txt, so asking whether one exists needs no request for the file itself.
/// </summary>
public sealed class WebPackageFiles(HttpClient http, NavigationManager navigation) : IAppPackageFiles
{
    private HashSet<string>? _files;

    public async Task<string> ReadTextAsync(string path) => await http.GetStringAsync(Address(path));

    public async Task<Stream> OpenAsync(string path)
    {
        var response = await http.GetAsync(Address(path));
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new FileNotFoundException("The site has no such packaged file.", path);
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    public async Task<bool> ExistsAsync(string path) => (await FilesAsync()).Contains(Normalized(path));

    private async Task<HashSet<string>> FilesAsync()
    {
        if (_files is null)
        {
            var list = await http.GetStringAsync(Address("files.txt"));
            _files = list.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Normalized)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return _files;
    }

    private Uri Address(string path) => new(new Uri(navigation.BaseUri), "package/" + Normalized(path));

    private static string Normalized(string path) => path.Replace('\\', '/').TrimStart('/');
}
