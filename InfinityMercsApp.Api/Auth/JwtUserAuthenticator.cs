using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace InfinityMercsApp.Api.Auth;

/// <inheritdoc/>
/// <remarks>
/// Accepts a token signed by the tenant's current keys, issued by it, for <see cref="AuthSettings.Audience"/>, and not
/// expired. The keys come from the tenant's OpenID metadata and are fetched again when a token names an unknown key.
/// </remarks>
public sealed class JwtUserAuthenticator : IUserAuthenticator
{
    private const string BearerPrefix = "Bearer ";

    private readonly IConfigurationManager<OpenIdConnectConfiguration>? _tenant;
    private readonly AuthSettings _settings;
    private readonly ILogger<JwtUserAuthenticator> _logger;
    private readonly JsonWebTokenHandler _handler = new();

    /// <summary>Reads the tenant's keys from its OpenID metadata, when sign-in is set up.</summary>
    public JwtUserAuthenticator(AuthSettings settings, ILogger<JwtUserAuthenticator> logger)
        : this(
            settings.IsConfigured
                ? new ConfigurationManager<OpenIdConnectConfiguration>(settings.MetadataAddress, new OpenIdConnectConfigurationRetriever())
                : null,
            settings,
            logger)
    {
    }

    /// <param name="tenant">Where the tenant's issuer and keys come from; null when sign-in isn't set up.</param>
    internal JwtUserAuthenticator(
        IConfigurationManager<OpenIdConnectConfiguration>? tenant,
        AuthSettings settings,
        ILogger<JwtUserAuthenticator> logger)
    {
        _tenant = tenant;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SignedInUser?> AuthenticateAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenant is not { } keys || !_settings.IsConfigured)
        {
            _logger.LogWarning("Sign-in isn't set up (Auth:Authority and Auth:Audience), so the request is refused.");
            return null;
        }

        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header[BearerPrefix.Length..].Trim();
        var result = await ValidateAsync(keys, token, cancellationToken);
        if (result.Exception is SecurityTokenSignatureKeyNotFoundException)
        {
            // The tenant may have rolled its keys since they were fetched.
            keys.RequestRefresh();
            result = await ValidateAsync(keys, token, cancellationToken);
        }

        if (!result.IsValid)
        {
            _logger.LogInformation("A bearer token was refused: {Reason}", result.Exception?.Message);
            return null;
        }

        return UserIdOf(result) is { Length: > 0 } userId ? new SignedInUser(userId) : null;
    }

    private async Task<TokenValidationResult> ValidateAsync(
        IConfigurationManager<OpenIdConnectConfiguration> keys,
        string token,
        CancellationToken cancellationToken)
    {
        var configuration = await keys.GetConfigurationAsync(cancellationToken);
        return await _handler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidIssuer = configuration.Issuer,
            ValidAudiences = _settings.ValidAudiences,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidateLifetime = true
        });
    }

    // The object id is the user's id in the tenant; a token without one falls back to its subject.
    private static string? UserIdOf(TokenValidationResult result) =>
        result.Claims.TryGetValue("oid", out var oid) && oid is string objectId ? objectId
        : result.Claims.TryGetValue("sub", out var sub) && sub is string subject ? subject
        : null;
}
