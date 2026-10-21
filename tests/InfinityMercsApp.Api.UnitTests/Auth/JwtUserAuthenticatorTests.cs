using System.Security.Cryptography;
using InfinityMercsApp.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace InfinityMercsApp.Api.UnitTests.Auth;

public sealed class JwtUserAuthenticatorTests : IDisposable
{
    private const string Issuer = "https://tenant.example/v2.0";
    private const string Audience = "api-client-id";

    private static readonly AuthSettings Settings = new() { Authority = Issuer, Audience = Audience };

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly RSA _otherRsa = RSA.Create(2048);
    private readonly RsaSecurityKey _key;
    private readonly JwtUserAuthenticator _authenticator;

    public JwtUserAuthenticatorTests()
    {
        _key = new RsaSecurityKey(_rsa) { KeyId = "key-1" };
        var tenant = new OpenIdConnectConfiguration { Issuer = Issuer };
        tenant.SigningKeys.Add(_key);
        _authenticator = new JwtUserAuthenticator(
            new StaticConfigurationManager<OpenIdConnectConfiguration>(tenant), Settings, NullLogger<JwtUserAuthenticator>.Instance);
    }

    public void Dispose()
    {
        _rsa.Dispose();
        _otherRsa.Dispose();
    }

    private string Token(
        string issuer = Issuer,
        string audience = Audience,
        IReadOnlyDictionary<string, object>? claims = null,
        DateTime? expires = null,
        SecurityKey? key = null)
    {
        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims?.ToDictionary() ?? new Dictionary<string, object> { ["oid"] = "user-1", ["sub"] = "subject-1" },
            NotBefore = (expires ?? now.AddHours(1)).AddHours(-2),
            IssuedAt = (expires ?? now.AddHours(1)).AddHours(-2),
            Expires = expires ?? now.AddHours(1),
            SigningCredentials = new SigningCredentials(key ?? _key, SecurityAlgorithms.RsaSha256)
        });
    }

    private static HttpRequest Request(string? authorization)
    {
        var context = new DefaultHttpContext();
        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        return context.Request;
    }

    [Fact]
    public async Task AuthenticateAsync_ValidToken_IsItsObjectId()
    {
        var user = await _authenticator.AuthenticateAsync(Request($"Bearer {Token()}"));

        Assert.Equal(new SignedInUser("user-1"), user);
    }

    [Fact]
    public async Task AuthenticateAsync_TokenWithoutObjectId_IsItsSubject()
    {
        var user = await _authenticator.AuthenticateAsync(Request($"Bearer {Token(claims: new Dictionary<string, object> { ["sub"] = "subject-1" })}"));

        Assert.Equal(new SignedInUser("subject-1"), user);
    }

    [Fact]
    public async Task AuthenticateAsync_NoToken_IsNobody()
    {
        Assert.Null(await _authenticator.AuthenticateAsync(Request(null)));
        Assert.Null(await _authenticator.AuthenticateAsync(Request($"Basic {Token()}")));
    }

    [Fact]
    public async Task AuthenticateAsync_ForTheApplicationIdUri_IsItsObjectId()
    {
        var user = await _authenticator.AuthenticateAsync(Request($"Bearer {Token(audience: $"api://{Audience}")}"));

        Assert.Equal(new SignedInUser("user-1"), user);
    }

    [Fact]
    public async Task AuthenticateAsync_ForAnotherAudience_IsNobody() =>
        Assert.Null(await _authenticator.AuthenticateAsync(Request($"Bearer {Token(audience: "someone-else")}")));

    [Fact]
    public async Task AuthenticateAsync_FromAnotherIssuer_IsNobody() =>
        Assert.Null(await _authenticator.AuthenticateAsync(Request($"Bearer {Token(issuer: "https://elsewhere.example/v2.0")}")));

    [Fact]
    public async Task AuthenticateAsync_Expired_IsNobody() =>
        Assert.Null(await _authenticator.AuthenticateAsync(Request($"Bearer {Token(expires: DateTime.UtcNow.AddHours(-1))}")));

    [Fact]
    public async Task AuthenticateAsync_SignedWithAnotherKey_IsNobody() =>
        Assert.Null(await _authenticator.AuthenticateAsync(Request($"Bearer {Token(key: new RsaSecurityKey(_otherRsa) { KeyId = "key-1" })}")));

    [Fact]
    public async Task AuthenticateAsync_SignInNotSetUp_IsNobody()
    {
        var authenticator = new JwtUserAuthenticator(new AuthSettings(), NullLogger<JwtUserAuthenticator>.Instance);

        Assert.Null(await authenticator.AuthenticateAsync(Request($"Bearer {Token()}")));
    }
}
