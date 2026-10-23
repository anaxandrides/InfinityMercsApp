using Microsoft.Identity.Client;
#if WINDOWS
using Microsoft.Identity.Client.Extensions.Msal;
#endif

namespace InfinityMercsApp.Services.SignIn;

/// <inheritdoc/>
/// <remarks>
/// Signs in with MSAL through the system browser, on Android and Windows. Android keeps the tokens itself; on Windows
/// MSAL keeps them only in memory, so they are saved to a file encrypted for the Windows user (DPAPI), and a sign-in
/// lasts across launches and lets background syncs renew their token. Other platforms aren't set up yet.
/// </remarks>
internal sealed class MsalSignInService(IPublicClientApplication msal, SignInSettings settings) : ISignInService
{
    private IReadOnlyList<string> Scopes => [settings.ApiScope];

#if WINDOWS
    private const string TokenCacheFileName = "msal-token-cache.bin";

    private readonly Lock _cacheLock = new();
    private Task? _cacheReady;
#endif

    /// <inheritdoc/>
#if ANDROID || WINDOWS
    public bool IsAvailable => true;
#else
    public bool IsAvailable => false;
#endif

    /// <inheritdoc/>
    public async Task<SignedInAccount?> GetAccountAsync(CancellationToken cancellationToken = default) =>
        IsAvailable && await AccountAsync() is { } account ? AccountOf(account) : null;

    /// <inheritdoc/>
    public async Task<SignInOutcome> SignInAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            return SignInOutcome.Unavailable;
        }

        try
        {
            await TokenCacheReadyAsync();
            var result = await Interactive(msal.AcquireTokenInteractive(Scopes)).ExecuteAsync(cancellationToken);
            return SignInOutcome.SignedIn(new SignedInAccount(
                result.Account.Username, AccountNames.DisplayNameFrom(result.ClaimsPrincipal, result.Account.Username)));
        }
        catch (MsalClientException ex) when (ex.ErrorCode == MsalError.AuthenticationCanceledError)
        {
            return SignInOutcome.Cancelled;
        }
        catch (MsalException ex)
        {
            Console.Error.WriteLine($"Signing in failed: {ex.ErrorCode} {ex.Message}");
            return SignInOutcome.Failed($"Signing in failed: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        await TokenCacheReadyAsync();
        foreach (var account in await msal.GetAccountsAsync())
        {
            await msal.RemoveAsync(account);
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || await AccountAsync() is not { } account)
        {
            return null;
        }

        try
        {
            return (await msal.AcquireTokenSilent(Scopes, account).ExecuteAsync(cancellationToken)).AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            return null;
        }
    }

    private async Task<IAccount?> AccountAsync()
    {
        await TokenCacheReadyAsync();
        return (await msal.GetAccountsAsync()).FirstOrDefault();
    }

#if WINDOWS
    // Loads the saved tokens once, before MSAL first needs them.
    private Task TokenCacheReadyAsync()
    {
        lock (_cacheLock)
        {
            return _cacheReady ??= RegisterTokenCacheAsync();
        }
    }

    private async Task RegisterTokenCacheAsync()
    {
        try
        {
            var storage = new StorageCreationPropertiesBuilder(TokenCacheFileName, FileSystem.Current.AppDataDirectory).Build();
            (await MsalCacheHelper.CreateAsync(storage)).RegisterCache(msal.UserTokenCache);
        }
        catch (Exception ex) when (ex is MsalCachePersistenceException or IOException or UnauthorizedAccessException)
        {
            // Signing in still works; it just doesn't outlast the app.
            Console.Error.WriteLine($"The sign-in can't be saved on this device: {ex.Message}");
        }
    }
#else
    private static Task TokenCacheReadyAsync() => Task.CompletedTask;
#endif

    // The name comes from the ID token MSAL keeps with the account, so it is there after a restart too.
    private static SignedInAccount AccountOf(IAccount account) =>
        new(account.Username, AccountNames.DisplayNameFrom(
            account.GetTenantProfiles()?.Select(profile => profile.ClaimsPrincipal).FirstOrDefault(claims => claims is not null),
            account.Username));

    // The sign-in page opens over the app's current screen.
    private static AcquireTokenInteractiveParameterBuilder Interactive(AcquireTokenInteractiveParameterBuilder builder)
    {
#if ANDROID
        return Platform.CurrentActivity is { } activity
            ? builder.WithParentActivityOrWindow(activity)
            : throw new MsalClientException("no_activity", "The app has no screen open to sign in over.");
#elif WINDOWS
        // The default browser, which already has the user's Google session, rather than a window inside the app.
        return builder.WithUseEmbeddedWebView(false);
#else
        return builder;
#endif
    }
}
