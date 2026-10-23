namespace InfinityMercsApp.Services.SignIn;

/// <summary>The account the app is signed in with.</summary>
/// <param name="Username">
/// The account's sign-in name, which stays the same and so tells accounts apart; for a Google account it is a
/// generated <c>{id}@infinitymercs.onmicrosoft.com</c>, so it isn't shown.
/// </param>
/// <param name="DisplayName">How the account is shown: the name given at sign-up, else the email address.</param>
public sealed record SignedInAccount(string Username, string DisplayName);
