using System.Security.Claims;
using InfinityMercsApp.Services.SignIn;
using InfinityMercsApp.Web.Files;
using InfinityMercsApp.Web.Platform;
using Microsoft.AspNetCore.Components.Authorization;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Platform;

/// <summary>Sign-in on the site: the account Blazor's MSAL signed in, and redirects to sign in or out.</summary>
public sealed class WebSignInServiceTests
{
    private readonly AuthenticationStateProvider _authentication = Substitute.For<AuthenticationStateProvider>();
    private readonly IAccessTokens _tokens = Substitute.For<IAccessTokens>();
    private readonly TestNavigation _navigation = new();
    private readonly WebSignInService _service;

    public WebSignInServiceTests()
    {
        _service = new WebSignInService(_authentication, _tokens, _navigation);
        _tokens.GetAsync().Returns("token-1");
    }

    private void SignedInAs(params IEnumerable<Claim> claims) =>
        _authentication.GetAuthenticationStateAsync().Returns(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "msal", "name", "roles"))));

    private void SignedOut() =>
        _authentication.GetAuthenticationStateAsync().Returns(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    [Fact]
    public async Task GetAccountAsync_SignedIn_IsNamedByItsObjectId()
    {
        SignedInAs(new Claim("oid", "object-1"), new Claim("name", "Kestrel"));

        var account = await _service.GetAccountAsync();

        Assert.Equal("object-1", account?.Username);
        Assert.Equal("Kestrel", account?.DisplayName);
    }

    [Fact]
    public async Task GetAccountAsync_NoObjectId_UsesItsName()
    {
        SignedInAs(new Claim("name", "Kestrel"));

        Assert.Equal("Kestrel", (await _service.GetAccountAsync())?.Username);
    }

    [Fact]
    public async Task GetAccountAsync_SignedOut_IsNull()
    {
        SignedOut();

        Assert.Null(await _service.GetAccountAsync());
    }

    [Fact]
    public async Task GetAccessTokenAsync_SignedIn_IsTheToken()
    {
        SignedInAs(new Claim("oid", "object-1"));

        Assert.Equal("token-1", await _service.GetAccessTokenAsync());
    }

    [Fact]
    public async Task GetAccessTokenAsync_SignedOut_IsNullWithoutAskingForOne()
    {
        SignedOut();

        Assert.Null(await _service.GetAccessTokenAsync());
        await _tokens.DidNotReceive().GetAsync();
    }

    [Fact]
    public async Task SignInAsync_Always_LeavesForTheLoginPage()
    {
        Assert.Equal(SignInOutcome.Cancelled, await _service.SignInAsync());

        Assert.Equal(["https://site.example/authentication/login"], _navigation.Visited);
        Assert.True(_service.IsAvailable);
    }

    [Fact]
    public async Task SignOutAsync_Always_LeavesForTheLogoutPage()
    {
        await _service.SignOutAsync();

        Assert.Equal(["https://site.example/authentication/logout"], _navigation.Visited);
    }
}
