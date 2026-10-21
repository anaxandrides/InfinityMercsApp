using Microsoft.AspNetCore.Http;

namespace InfinityMercsApp.Api.Auth;

/// <summary>Works out who signed in from a request's bearer token.</summary>
public interface IUserAuthenticator
{
    /// <summary>The signed-in user, or null when the request has no valid token.</summary>
    Task<SignedInUser?> AuthenticateAsync(HttpRequest request, CancellationToken cancellationToken = default);
}
