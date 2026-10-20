namespace InfinityMercsApp.Services.SignIn;

/// <summary>How a sign-in attempt ended: the account signed in with, or why there is none.</summary>
public sealed record SignInOutcome(SignInStatus Status, SignedInAccount? Account, string Message)
{
    public static SignInOutcome SignedIn(SignedInAccount account) => new(SignInStatus.SignedIn, account, string.Empty);

    public static SignInOutcome Cancelled { get; } = new(SignInStatus.Cancelled, null, "Sign-in was cancelled.");

    public static SignInOutcome Unavailable { get; } = new(SignInStatus.Unavailable, null, "Signing in isn't available on this device yet.");

    public static SignInOutcome Failed(string message) => new(SignInStatus.Failed, null, message);
}
