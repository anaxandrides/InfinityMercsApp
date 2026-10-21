using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace InfinityMercsApp.Web.Files;

/// <inheritdoc/>
/// <remarks>Asks MSAL for a token with the API scope set in Program.cs, renewing it quietly when it can.</remarks>
public sealed class MsalAccessTokens(IAccessTokenProvider provider) : IAccessTokens
{
    /// <inheritdoc/>
    public async Task<string?> GetAsync() =>
        (await provider.RequestAccessToken()).TryGetToken(out var token) ? token.Value : null;
}
