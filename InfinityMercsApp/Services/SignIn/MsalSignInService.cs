using Microsoft.Identity.Client;

namespace InfinityMercsApp.Services.SignIn;

/// <inheritdoc/>
/// <remarks>
/// Signs in with MSAL through the system browser. Android keeps the tokens itself, so a sign-in lasts across
/// launches. Other platforms aren't set up yet: Windows needs its own redirect URI and a token cache.
/// </remarks>
internal sealed class MsalSignInService(IPublicClientApplication msal, SignInSettings settings) : ISignInService
{
    private IReadOnlyList<string> Scopes => [settings.ApiScope];

    /// <inheritdoc/>
#if ANDROID
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
            var result = await Interactive(msal.AcquireTokenInteractive(Scopes)).ExecuteAsync(cancellationToken);
            return SignInOutcome.SignedIn(AccountOf(result.Account));
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

    private async Task<IAccount?> AccountAsync() => (await msal.GetAccountsAsync()).FirstOrDefault();

    private static SignedInAccount AccountOf(IAccount account) =>
        new(string.IsNullOrWhiteSpace(account.Username) ? "your account" : account.Username);

    // The sign-in page opens over the app's current screen.
    private static AcquireTokenInteractiveParameterBuilder Interactive(AcquireTokenInteractiveParameterBuilder builder)
    {
#if ANDROID
        return Platform.CurrentActivity is { } activity
            ? builder.WithParentActivityOrWindow(activity)
            : throw new MsalClientException("no_activity", "The app has no screen open to sign in over.");
#else
        return builder;
#endif
    }
}
