namespace InfinityMercsApp.Services.Links;

/// <inheritdoc/>
internal sealed class MauiLinkLauncher(ILauncher launcher) : ILinkLauncher
{
    /// <inheritdoc/>
    public Task<bool> OpenAsync(string uri) => launcher.OpenAsync(uri);
}
