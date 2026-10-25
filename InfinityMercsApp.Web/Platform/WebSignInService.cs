using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Web.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace InfinityMercsApp.Web.Platform;

/// <inheritdoc/>
/// <remarks>
/// The site signs in by redirecting to Entra (Blazor's MSAL); signing in or out here leaves the page, so
/// <see cref="SignInAsync"/> never comes back signed in.
/// </remarks>
public sealed class WebSignInService(AuthenticationStateProvider authentication, IAccessTokens tokens, NavigationManager navigation) : ISignInService
{
    /// <summary>The claim that names the account for the sync ledger: Entra's object id, which never changes.</summary>
    private const string AccountIdClaim = "oid";

    public bool IsAvailable => true;

    public async Task<SignedInAccount?> GetAccountAsync(CancellationToken cancellationToken = default)
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var username = user.FindFirst(AccountIdClaim)?.Value ?? user.Identity.Name ?? string.Empty;
        return new SignedInAccount(username, AccountNames.DisplayNameFrom(user, username));
    }

    public Task<SignInOutcome> SignInAsync(CancellationToken cancellationToken = default)
    {
        navigation.NavigateToLogin("authentication/login");
        return Task.FromResult(SignInOutcome.Cancelled);
    }

    public Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        navigation.NavigateToLogout("authentication/logout");
        return Task.CompletedTask;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) =>
        await GetAccountAsync(cancellationToken) is null ? null : await tokens.GetAsync();
}
