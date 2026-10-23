using System.Security.Claims;

namespace InfinityMercsApp.Services.SignIn;

/// <summary>How a signed-in account is named to the user, from its sign-in token's claims.</summary>
public static class AccountNames
{
    private static readonly IReadOnlyList<string> NameClaims = ["name", "email"];

    /// <summary>
    /// The account's display name (the "name" claim the sign-up flow collects), else its email address, else its
    /// username; never a blank.
    /// </summary>
    public static string DisplayNameFrom(ClaimsPrincipal? claims, string username) =>
        NameClaims
            .Select(type => claims?.FindFirst(type)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))
        ?? (string.IsNullOrWhiteSpace(username) ? "your account" : username);
}
