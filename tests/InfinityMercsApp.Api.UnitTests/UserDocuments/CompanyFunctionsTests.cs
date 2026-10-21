using System.Text;
using InfinityMercsApp.Api.Auth;
using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

public sealed class CompanyFunctionsTests
{
    private const string UserId = "user-1";
    private const string DocumentId = "c0ffee";

    private const string Company = """
        {"companyName":"Red Team","companyType":"Standard Company - Vanilla","companyIdentifier":"c0ffee",
         "sourceFactions":[{"factionId":101}],
         "entries":[{"entryIndex":0,"name":"Fusilier","sourceFactionId":101,"sourceUnitId":1,"isLieutenant":true}]}
        """;

    private readonly IUserAuthenticator _authenticator = Substitute.For<IUserAuthenticator>();
    private readonly IUserDocumentStore _store = Substitute.For<IUserDocumentStore>();
    private readonly CompanyFunctions _functions;

    public CompanyFunctionsTests()
    {
        _authenticator.AuthenticateAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(new SignedInUser(UserId));
        _store.TryCreateAsync(Arg.Any<UserDocument<SavedCompanyFile>>(), Arg.Any<CancellationToken>()).Returns(true);
        var endpoints = new UserDocumentEndpoints(_authenticator, new UserDocumentService(_store, TimeProvider.System));
        _functions = new CompanyFunctions(endpoints, new CompanyFileChecks());
    }

    private static HttpRequest Request(string? body = null, string? query = null)
    {
        var context = new DefaultHttpContext();
        if (body is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
        }

        if (query is not null)
        {
            context.Request.QueryString = new QueryString(query);
        }

        return context.Request;
    }

    private static string Save(int baseRevision, string company = Company) =>
        $$"""{"baseRevision":{{baseRevision}},"armySnapshotId":"0123456789abcdef","content":{{company}}}""";

    private void Store(int revision) =>
        _store.ReadAsync<SavedCompanyFile>(UserId, UserDocumentKind.Company, DocumentId, Arg.Any<CancellationToken>()).Returns(
            new StoredUserDocument<SavedCompanyFile>(
                new UserDocument<SavedCompanyFile>(
                    UserDocumentIds.For(UserDocumentKind.Company, DocumentId), UserId, UserDocumentKind.Company, DocumentId, revision,
                    DateTimeOffset.UnixEpoch, false, null, new SavedCompanyFile { CompanyName = "Blue Team" }),
                "etag-1"));

    [Fact]
    public async Task EveryRoute_SignedOut_IsUnauthorized()
    {
        _authenticator.AuthenticateAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns((SignedInUser?)null);

        Assert.IsType<UnauthorizedResult>(await _functions.ListAsync(Request(), CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await _functions.GetAsync(Request(), DocumentId, CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await _functions.SaveAsync(Request(Save(0)), DocumentId, CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await _functions.DeleteAsync(Request(query: "?baseRevision=1"), DocumentId, CancellationToken.None));
        await _store.DidNotReceive().ReadAsync<SavedCompanyFile>(
            Arg.Any<string>(), Arg.Any<UserDocumentKind>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_NewCompany_IsSavedForTheSignedInUser()
    {
        var result = await _functions.SaveAsync(Request(Save(0)), DocumentId, CancellationToken.None);

        var view = Assert.IsType<UserDocumentView<SavedCompanyFile>>(Assert.IsType<JsonResult>(result).Value);
        Assert.Equal(1, view.Revision);
        Assert.Equal("Red Team", view.Content?.CompanyName);
        await _store.Received(1).TryCreateAsync(
            Arg.Is<UserDocument<SavedCompanyFile>>(document => document.UserId == UserId && document.Kind == UserDocumentKind.Company),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_StaleRevision_IsAConflictWithTheCurrentCompany()
    {
        Store(3);

        var result = await _functions.SaveAsync(Request(Save(2)), DocumentId, CancellationToken.None);

        var current = Assert.IsType<UserDocumentView<SavedCompanyFile>>(Assert.IsType<ConflictObjectResult>(result).Value);
        Assert.Equal(3, current.Revision);
        Assert.Equal("Blue Team", current.Content?.CompanyName);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"baseRevision":0}""")]
    [InlineData("""{"baseRevision":0,"content":null}""")]
    [InlineData("""{"baseRevision":-1,"content":{"companyName":"a","companyType":"b","companyIdentifier":"c","sourceFactions":[],"entries":[]}}""")]
    public async Task SaveAsync_BadBody_IsABadRequest(string body)
    {
        var result = await _functions.SaveAsync(Request(body), DocumentId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        await _store.DidNotReceive().TryCreateAsync(Arg.Any<UserDocument<SavedCompanyFile>>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("companyName")]
    [InlineData("entries")]
    public async Task SaveAsync_CompanyMissingARequiredField_IsABadRequest(string field)
    {
        var company = Company.Replace($"\"{field}\":", $"\"left-out-{field}\":", StringComparison.Ordinal);

        var result = await _functions.SaveAsync(Request(Save(0, company)), DocumentId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveAsync_CompanyWithoutACaptain_IsABadRequestNamingTheProblem()
    {
        var company = Company.Replace("\"isLieutenant\":true", "\"isLieutenant\":false", StringComparison.Ordinal);

        var result = await _functions.SaveAsync(Request(Save(0, company)), DocumentId, CancellationToken.None);

        var problems = Assert.IsAssignableFrom<IReadOnlyList<string>>(Assert.IsType<BadRequestObjectResult>(result).Value);
        Assert.Equal(["The company must have exactly one Captain."], problems);
    }

    [Fact]
    public async Task SaveAsync_SavedUnderAnotherId_IsABadRequest()
    {
        var result = await _functions.SaveAsync(Request(Save(0)), "another-id", CancellationToken.None);

        var problems = Assert.IsAssignableFrom<IReadOnlyList<string>>(Assert.IsType<BadRequestObjectResult>(result).Value);
        Assert.Equal(["The company's identifier isn't the one it is saved under."], problems);
    }

    [Fact]
    public async Task SaveAsync_TooLarge_IsRefused()
    {
        var request = Request(Save(0));
        request.ContentLength = UserDocumentEndpoints.MaxBodyBytes + 1;

        var result = await _functions.SaveAsync(request, DocumentId, CancellationToken.None);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    [Fact]
    public async Task SaveAsync_LargerThanItsDeclaredLength_IsRefused()
    {
        var request = Request(Save(0, Company.Replace("Red Team", new string('x', UserDocumentEndpoints.MaxBodyBytes), StringComparison.Ordinal)));
        request.ContentLength = null;

        var result = await _functions.SaveAsync(request, DocumentId, CancellationToken.None);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    [Fact]
    public async Task GetAsync_BadId_IsNotFound()
    {
        var result = await _functions.GetAsync(Request(), "../other", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAsync_OwnCompany_IsReturned()
    {
        Store(3);

        var result = await _functions.GetAsync(Request(), DocumentId, CancellationToken.None);

        Assert.Equal(3, Assert.IsType<UserDocumentView<SavedCompanyFile>>(Assert.IsType<JsonResult>(result).Value).Revision);
    }

    [Fact]
    public async Task ListAsync_Companies_ListsTheUsersCompanies()
    {
        _store.ListAsync<SavedCompanyFile>(UserId, UserDocumentKind.Company, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _functions.ListAsync(Request(), CancellationToken.None);

        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<UserDocumentView<SavedCompanyFile>>>(Assert.IsType<JsonResult>(result).Value));
    }

    [Fact]
    public async Task DeleteAsync_NoBaseRevision_IsABadRequest()
    {
        var result = await _functions.DeleteAsync(Request(), DocumentId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteAsync_MissingCompany_IsNotFound()
    {
        var result = await _functions.DeleteAsync(Request(query: "?baseRevision=1"), DocumentId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
