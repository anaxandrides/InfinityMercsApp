namespace InfinityMercsApp.Services.SignIn;

/// <summary>The account the app is signed in with.</summary>
/// <param name="Username">How the account is shown: usually its email address.</param>
public sealed record SignedInAccount(string Username);
