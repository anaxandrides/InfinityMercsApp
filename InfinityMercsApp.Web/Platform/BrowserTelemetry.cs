using InfinityMercsApp.Services.Telemetry;
using Microsoft.JSInterop;

namespace InfinityMercsApp.Web.Platform;

/// <summary>
/// Telemetry in the browser: the setting (install id, on or off) kept in local storage, since the site's files last
/// only for the visit, and what is waiting sent when the tab is hidden (wwwroot/js/telemetry.js).
/// </summary>
public sealed class BrowserTelemetry(IJSRuntime js, TelemetryPaths paths, IServiceProvider services)
{
    private IJSObjectReference? _module;
    private DotNetObjectReference<BrowserTelemetry>? _self;

    /// <summary>Puts the kept setting where the telemetry store reads it, then keeps the store's (a new install's) back.</summary>
    public async Task StartAsync()
    {
        var module = await ModuleAsync();
        if (await module.InvokeAsync<string?>("loadSettings") is { Length: > 0 } saved)
        {
            Directory.CreateDirectory(paths.Directory);
            await File.WriteAllTextAsync(paths.SettingsPath, saved);
        }

        // Reading the store's setting makes one for a new install.
        _ = services.GetRequiredService<TelemetryStore>().InstallId;
        await SaveAsync();

        _self = DotNetObjectReference.Create(this);
        await module.InvokeVoidAsync("watchHidden", _self);
        services.GetRequiredService<ITelemetryCoordinator>().Start();
    }

    /// <summary>Keeps the store's setting, as after the user turns telemetry on or off.</summary>
    public async Task SaveAsync()
    {
        if (File.Exists(paths.SettingsPath))
        {
            await (await ModuleAsync()).InvokeVoidAsync("saveSettings", await File.ReadAllTextAsync(paths.SettingsPath));
        }
    }

    [JSInvokable]
    public void PageHidden() => services.GetRequiredService<ITelemetryCoordinator>().SendSoon();

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/telemetry.js");
}
