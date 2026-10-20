using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>The signed-in user's seasons, under me/seasons.</summary>
public sealed class SeasonFunctions(UserDocumentEndpoints endpoints, SeasonFileChecks checks)
{
    private const UserDocumentKind Kind = UserDocumentKind.Season;

    [Function("ListSeasons")]
    public Task<IActionResult> ListAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me/seasons")] HttpRequest request,
        CancellationToken cancellationToken) =>
        endpoints.ListAsync<SeasonFile>(request, Kind, cancellationToken);

    [Function("GetSeason")]
    public Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me/seasons/{documentId}")] HttpRequest request,
        string documentId,
        CancellationToken cancellationToken) =>
        endpoints.GetAsync<SeasonFile>(request, Kind, documentId, cancellationToken);

    [Function("SaveSeason")]
    public Task<IActionResult> SaveAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "me/seasons/{documentId}")] HttpRequest request,
        string documentId,
        CancellationToken cancellationToken) =>
        endpoints.SaveAsync(request, Kind, documentId, checks, cancellationToken);

    [Function("DeleteSeason")]
    public Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "me/seasons/{documentId}")] HttpRequest request,
        string documentId,
        CancellationToken cancellationToken) =>
        endpoints.DeleteAsync<SeasonFile>(request, Kind, documentId, cancellationToken);
}
