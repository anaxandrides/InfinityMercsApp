namespace InfinityMercsApp.Services.SignIn;

/// <summary>How a sign-in attempt ended.</summary>
public enum SignInStatus
{
    SignedIn,

    /// <summary>The user closed the sign-in page.</summary>
    Cancelled,

    Failed,

    /// <summary>This platform can't sign in yet.</summary>
    Unavailable
}
