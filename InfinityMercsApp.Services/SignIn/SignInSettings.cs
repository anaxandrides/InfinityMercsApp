namespace InfinityMercsApp.Services.SignIn;

/// <summary>Where the app signs users in, and what it asks for.</summary>
/// <param name="ClientId">The app's registration in the sign-in tenant.</param>
/// <param name="Authority">The Entra External ID tenant's sign-in address.</param>
/// <param name="ApiScope">The API's scope, so the token can call the companies and seasons API.</param>
public sealed record SignInSettings(string ClientId, string Authority, string ApiScope)
{
    /// <summary>The infinitymercs tenant, the Infinity Mercs Android registration and the API's access_as_user scope.</summary>
    public static SignInSettings Production { get; } = new(
        "0bbba93d-840e-48ce-b2d2-8007f227ba8d",
        "https://infinitymercs.ciamlogin.com/",
        "api://6d09c3c6-41d1-4c53-a256-d65e8e4af405/access_as_user");

    /// <summary>Where the browser hands the result back to the app; registered on the app as a mobile redirect URI.</summary>
    public string RedirectUri => $"msal{ClientId}://auth";
}
