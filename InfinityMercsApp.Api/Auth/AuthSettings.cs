namespace InfinityMercsApp.Api.Auth;

/// <summary>The "Auth" settings: which tenant's tokens the API accepts, and for which audience.</summary>
public sealed class AuthSettings
{
    /// <summary>The tenant's authority, e.g. https://{tenant}.ciamlogin.com/{tenantId}/v2.0.</summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>The API's application (client) id, which tokens must be issued for.</summary>
    public string Audience { get; init; } = string.Empty;

    /// <summary>
    /// The audiences a token may carry: the client id, as version 2 tokens give it, or the Application ID URI
    /// (api://{client id}), as version 1 tokens do.
    /// </summary>
    public IReadOnlyList<string> ValidAudiences => [Audience, $"api://{Audience}"];

    /// <summary>Whether sign-in is set up; until it is, every request that needs a user is refused.</summary>
    public bool IsConfigured => Authority.Length > 0 && Audience.Length > 0;

    public string MetadataAddress => $"{Authority.TrimEnd('/')}/.well-known/openid-configuration";
}
