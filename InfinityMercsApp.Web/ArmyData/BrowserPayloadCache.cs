using Microsoft.JSInterop;

namespace InfinityMercsApp.Web.ArmyData;

/// <summary>The army data's payloads kept in the browser between visits, under their hashes (wwwroot/js/armycache.js).</summary>
public sealed class BrowserPayloadCache(IJSRuntime js)
{
    private IJSObjectReference? _module;

    /// <summary>The payload kept under <paramref name="hash"/>; null when none is.</summary>
    public async Task<string?> GetAsync(string hash) => await (await ModuleAsync()).InvokeAsync<string?>("get", hash);

    public async Task PutAsync(string hash, string json) => await (await ModuleAsync()).InvokeVoidAsync("put", hash, json);

    /// <summary>Lets go of every payload but those under <paramref name="hashes"/>.</summary>
    public async Task PruneAsync(IReadOnlyCollection<string> hashes) => await (await ModuleAsync()).InvokeVoidAsync("prune", hashes);

    private async Task<IJSObjectReference> ModuleAsync() =>
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", "./js/armycache.js");
}
