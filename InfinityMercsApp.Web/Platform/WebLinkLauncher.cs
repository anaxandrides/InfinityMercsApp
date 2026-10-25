using InfinityMercsApp.Services.Links;
using Microsoft.JSInterop;

namespace InfinityMercsApp.Web.Platform;

/// <inheritdoc/>
/// <remarks>Opens the link in a new tab.</remarks>
public sealed class WebLinkLauncher(IJSRuntime js) : ILinkLauncher
{
    /// <inheritdoc/>
    public async Task<bool> OpenAsync(string uri)
    {
        await js.InvokeVoidAsync("open", uri, "_blank", "noopener");
        return true;
    }
}
