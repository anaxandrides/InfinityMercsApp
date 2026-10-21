using System.Net;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Season;

namespace InfinityMercsApp.UnitTests.ApiClient.UserDocuments;

public sealed class UserDocumentsClientTests
{
    private static readonly UserDocumentsEndpoint Endpoint = new(new Uri("https://api.example/api/"));

    private const string SeasonView =
        """{"documentId":"s1","revision":3,"updatedAt":"2026-10-21T18:00:00+00:00","deleted":false,"content":{"companyName":"Iron Wolves","companyIdentifier":"c1","rounds":[]}}""";

    [Fact]
    public async Task SaveAsync_Saved_SendsTheTokenAndBodyAndReadsTheRevision()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, SeasonView);
        var write = new UserDocumentWrite<SeasonFile> { BaseRevision = 2, Content = new SeasonFile { CompanyName = "Iron Wolves", CompanyIdentifier = "c1" } };

        var reply = await new UserDocumentsClient(new HttpClient(handler), Endpoint).SaveAsync(UserDocumentKind.Season, "s1", write, "token-1");

        Assert.Equal(UserDocumentsStatus.Ok, reply.Status);
        Assert.Equal(3, reply.Value?.Revision);
        Assert.Equal(HttpMethod.Put, handler.Request?.Method);
        Assert.Equal("https://api.example/api/me/seasons/s1", handler.Request?.RequestUri?.ToString());
        Assert.Equal("Bearer token-1", handler.Request?.Headers.Authorization?.ToString());
        Assert.Contains("\"baseRevision\":2", handler.RequestBody);
        Assert.Contains("\"companyName\":\"Iron Wolves\"", handler.RequestBody);
    }

    [Fact]
    public async Task SaveAsync_Conflict_CarriesTheCurrentFile()
    {
        var handler = new CapturingHandler(HttpStatusCode.Conflict, SeasonView);
        var write = new UserDocumentWrite<SeasonFile> { BaseRevision = 1, Content = new SeasonFile() };

        var reply = await new UserDocumentsClient(new HttpClient(handler), Endpoint).SaveAsync(UserDocumentKind.Season, "s1", write, "token-1");

        Assert.Equal(UserDocumentsStatus.Conflict, reply.Status);
        Assert.Equal("Iron Wolves", reply.Value?.Content?.CompanyName);
    }

    [Fact]
    public async Task DeleteAsync_AnyFile_GivesTheBaseRevisionInTheQuery()
    {
        var handler = new CapturingHandler(HttpStatusCode.OK, SeasonView);

        await new UserDocumentsClient(new HttpClient(handler), Endpoint).DeleteAsync<SeasonFile>(UserDocumentKind.Company, "c1", 4, "token-1");

        Assert.Equal(HttpMethod.Delete, handler.Request?.Method);
        Assert.Equal("https://api.example/api/me/companies/c1?baseRevision=4", handler.Request?.RequestUri?.ToString());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, UserDocumentsStatus.Unauthorized)]
    [InlineData(HttpStatusCode.NotFound, UserDocumentsStatus.NotFound)]
    [InlineData(HttpStatusCode.BadRequest, UserDocumentsStatus.Rejected)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, UserDocumentsStatus.Rejected)]
    [InlineData(HttpStatusCode.ServiceUnavailable, UserDocumentsStatus.Unavailable)]
    public async Task GetAsync_FailedRequest_SaysHowItFailed(HttpStatusCode code, UserDocumentsStatus expected)
    {
        var handler = new CapturingHandler(code, string.Empty);

        var reply = await new UserDocumentsClient(new HttpClient(handler), Endpoint).GetAsync<SeasonFile>(UserDocumentKind.Season, "s1", "token-1");

        Assert.Equal(expected, reply.Status);
        Assert.Null(reply.Value);
    }

    [Fact]
    public async Task ListAsync_Unreachable_IsUnavailable()
    {
        var reply = await new UserDocumentsClient(new HttpClient(new FailingHandler()), Endpoint).ListAsync<SeasonFile>(UserDocumentKind.Season, "token-1");

        Assert.Equal(UserDocumentsStatus.Unavailable, reply.Status);
    }
}
