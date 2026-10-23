using System.Security.Claims;
using InfinityMercsApp.Services.SignIn;

namespace InfinityMercsApp.UnitTests.Services.SignIn;

public sealed class AccountNamesTests
{
    private const string GoogleUsername = "2b1f0c9e-4d6a-4f1b-9c3e-7a8d5e6f1a2b@infinitymercs.onmicrosoft.com";

    private static ClaimsPrincipal Claims(params IEnumerable<Claim> claims) => new(new ClaimsIdentity(claims));

    [Fact]
    public void DisplayNameFrom_NameClaim_IsTheName() =>
        Assert.Equal("Kestrel", AccountNames.DisplayNameFrom(Claims(new Claim("name", "Kestrel"), new Claim("email", "k@example.com")), GoogleUsername));

    [Fact]
    public void DisplayNameFrom_NoName_IsTheEmail() =>
        Assert.Equal("k@example.com", AccountNames.DisplayNameFrom(Claims(new Claim("name", " "), new Claim("email", "k@example.com")), GoogleUsername));

    [Fact]
    public void DisplayNameFrom_NoClaims_IsTheUsername() =>
        Assert.Equal(GoogleUsername, AccountNames.DisplayNameFrom(null, GoogleUsername));

    [Fact]
    public void DisplayNameFrom_NothingAtAll_IsYourAccount() =>
        Assert.Equal("your account", AccountNames.DisplayNameFrom(null, string.Empty));
}
