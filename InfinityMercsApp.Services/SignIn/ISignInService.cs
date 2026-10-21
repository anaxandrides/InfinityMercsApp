namespace InfinityMercsApp.Services.SignIn;

/// <summary>Signs the user in to the app's online side, and gives the tokens its API needs.</summary>
public interface ISignInService
{
    /// <summary>Whether this platform can sign in.</summary>
    bool IsAvailable { get; }

    /// <summary>The account signed in with, remembered from an earlier sign-in; null when signed out.</summary>
    Task<SignedInAccount?> GetAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>Opens the sign-in page; never throws for a failed or cancelled sign-in.</summary>
    Task<SignInOutcome> SignInAsync(CancellationToken cancellationToken = default);

    /// <summary>Forgets the account and its tokens on this device.</summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);

    /// <summary>A token for the API without showing anything, or null when the user has to sign in again.</summary>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
