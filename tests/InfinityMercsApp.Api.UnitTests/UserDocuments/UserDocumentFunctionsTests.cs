using System.Text;
using System.Text.Json;
using InfinityMercsApp.Api.Auth;
using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

public sealed class UserDocumentFunctionsTests
{
    private const string UserId = "user-1";
    private const string DocumentId = "c0ffee";

    private readonly IUserAuthenticator _authenticator = Substitute.For<IUserAuthenticator>();
    private readonly IUserDocumentStore _store = Substitute.For<IUserDocumentStore>();
    private readonly UserDocumentFunctions _functions;

    public UserDocumentFunctionsTests()
    {
        _authenticator.AuthenticateAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(new SignedInUser(UserId));
        _store.TryCreateAsync(Arg.Any<UserDocument>(), Arg.Any<CancellationToken>()).Returns(true);
        _functions = new UserDocumentFunctions(_authenticator, new UserDocumentService(_store, TimeProvider.System));
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

    private void SignedOut() =>
        _authenticator.AuthenticateAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns((SignedInUser?)null);

    private void Store(int revision) =>
        _store.ReadAsync(UserId, UserDocumentKind.Company, DocumentId, Arg.Any<CancellationToken>()).Returns(new StoredUserDocument(
            new UserDocument(
                UserDocument.IdFor(UserDocumentKind.Company, DocumentId), UserId, UserDocumentKind.Company, DocumentId, revision,
                DateTimeOffset.UnixEpoch, false, null, JsonSerializer.SerializeToElement(new { companyName = "Blue Team" })),
            "etag-1"));

    [Fact]
    public async Task EveryRoute_SignedOut_IsUnauthorized()
    {
        SignedOut();

        Assert.IsType<UnauthorizedResult>(await _functions.ListAsync(Request(), "companies", CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await _functions.GetAsync(Request(), "companies", DocumentId, CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await _functions.SaveAsync(Request("{}"), "companies", DocumentId, CancellationToken.None));
        Assert.IsType<UnauthorizedResult>(await _functions.DeleteAsync(Request(query: "?baseRevision=1"), "companies", DocumentId, CancellationToken.None));
        await _store.DidNotReceiveWithAnyArgs().ReadAsync(UserId, UserDocumentKind.Company, DocumentId, CancellationToken.None);
    }

    [Fact]
    public async Task SaveAsync_NewCompany_IsSavedForTheSignedInUser()
    {
        var result = await _functions.SaveAsync(
            Request("""{"baseRevision":0,"armySnapshotId":"0123456789abcdef","content":{"companyName":"Red Team"}}"""),
            "companies",
            DocumentId,
            CancellationToken.None);

        var view = Assert.IsType<UserDocumentView>(Assert.IsType<JsonResult>(result).Value);
        Assert.Equal(1, view.Revision);
        await _store.Received(1).TryCreateAsync(Arg.Is<UserDocument>(document => document.UserId == UserId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_StaleRevision_IsAConflictWithTheCurrentFile()
    {
        Store(3);

        var result = await _functions.SaveAsync(
            Request("""{"baseRevision":2,"content":{"companyName":"Red Team"}}"""), "companies", DocumentId, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(3, Assert.IsType<UserDocumentView>(conflict.Value).Revision);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"baseRevision":0}""")]
    [InlineData("""{"baseRevision":0,"content":[1,2]}""")]
    [InlineData("""{"baseRevision":-1,"content":{}}""")]
    public async Task SaveAsync_BadBody_IsABadRequest(string body)
    {
        var result = await _functions.SaveAsync(Request(body), "companies", DocumentId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SaveAsync_TooLarge_IsRefused()
    {
        var request = Request("""{"baseRevision":0,"content":{}}""");
        request.ContentLength = UserDocumentFunctions.MaxBodyBytes + 1;

        var result = await _functions.SaveAsync(request, "companies", DocumentId, CancellationToken.None);

        Assert.Equal(StatusCodes.Status413PayloadTooLarge, Assert.IsType<StatusCodeResult>(result).StatusCode);
    }

    [Theory]
    [InlineData("leagues", DocumentId)]
    [InlineData("companies", "../other")]
    public async Task GetAsync_UnknownCollectionOrBadId_IsNotFound(string collection, string documentId)
    {
        var result = await _functions.GetAsync(Request(), collection, documentId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetAsync_OwnFile_IsReturned()
    {
        Store(3);

        var result = await _functions.GetAsync(Request(), "companies", DocumentId, CancellationToken.None);

        Assert.Equal(3, Assert.IsType<UserDocumentView>(Assert.IsType<JsonResult>(result).Value).Revision);
    }

    [Fact]
    public async Task ListAsync_Seasons_ListsTheUsersSeasons()
    {
        _store.ListAsync(UserId, UserDocumentKind.Season, Arg.Any<CancellationToken>()).Returns([]);

        var result = await _functions.ListAsync(Request(), "seasons", CancellationToken.None);

        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<UserDocumentView>>(Assert.IsType<JsonResult>(result).Value));
    }

    [Fact]
    public async Task DeleteAsync_NoBaseRevision_IsABadRequest()
    {
        var result = await _functions.DeleteAsync(Request(), "companies", DocumentId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task DeleteAsync_MissingFile_IsNotFound()
    {
        var result = await _functions.DeleteAsync(Request(query: "?baseRevision=1"), "companies", DocumentId, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }
}
