using System.Text;
using InfinityMercsApp.Api.Auth;
using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Season;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

public sealed class SeasonFunctionsTests
{
    private const string UserId = "user-1";
    private const string DocumentId = "5ea5011";

    private readonly IUserDocumentStore _store = Substitute.For<IUserDocumentStore>();
    private readonly SeasonFunctions _functions;

    public SeasonFunctionsTests()
    {
        var authenticator = Substitute.For<IUserAuthenticator>();
        authenticator.AuthenticateAsync(Arg.Any<HttpRequest>(), Arg.Any<CancellationToken>()).Returns(new SignedInUser(UserId));
        _store.TryCreateAsync(Arg.Any<UserDocument<SeasonFile>>(), Arg.Any<CancellationToken>()).Returns(true);
        var endpoints = new UserDocumentEndpoints(authenticator, new UserDocumentService(_store, TimeProvider.System));
        _functions = new SeasonFunctions(endpoints, new SeasonFileChecks());
    }

    private static HttpRequest Request(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context.Request;
    }

    [Fact]
    public async Task SaveAsync_NewSeason_IsSavedAsASeason()
    {
        var result = await _functions.SaveAsync(
            Request("""{"baseRevision":0,"content":{"seasonIdentifier":"5ea5011","companyName":"Red Team","companyIdentifier":"red","rounds":[{"roundIndex":1}]}}"""),
            DocumentId,
            CancellationToken.None);

        var view = Assert.IsType<UserDocumentView<SeasonFile>>(Assert.IsType<JsonResult>(result).Value);
        Assert.Equal(1, Assert.Single(view.Content?.Rounds ?? []).RoundIndex);
        await _store.Received(1).TryCreateAsync(
            Arg.Is<UserDocument<SeasonFile>>(document => document.Kind == UserDocumentKind.Season && document.Id == "season-5ea5011"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveAsync_SeasonWithoutRounds_IsABadRequest()
    {
        var result = await _functions.SaveAsync(
            Request("""{"baseRevision":0,"content":{"companyName":"Red Team","companyIdentifier":"red"}}"""), DocumentId, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
