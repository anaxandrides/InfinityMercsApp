using System.Text;
using System.Text.Json;
using InfinityMercsApp.Api.Auth;
using InfinityMercsApp.Infrastructure.API.UserDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>
/// The signed-in user's companies and seasons, under me/companies and me/seasons. Every route needs a valid sign-in
/// token, and a user only ever reaches their own files.
/// </summary>
public sealed class UserDocumentFunctions(IUserAuthenticator authenticator, UserDocumentService documents)
{
    /// <summary>The largest file accepted; Cosmos DB keeps documents of up to 2 MB.</summary>
    public const int MaxBodyBytes = 1_500_000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Function("ListUserDocuments")]
    public async Task<IActionResult> ListAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me/{collection}")] HttpRequest request,
        string collection,
        CancellationToken cancellationToken)
    {
        if (await authenticator.AuthenticateAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        return UserDocumentRoutes.KindOf(collection) is { } kind
            ? Json(await documents.ListAsync(user.UserId, kind, cancellationToken))
            : new NotFoundResult();
    }

    [Function("GetUserDocument")]
    public async Task<IActionResult> GetAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me/{collection}/{documentId}")] HttpRequest request,
        string collection,
        string documentId,
        CancellationToken cancellationToken)
    {
        if (await authenticator.AuthenticateAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (UserDocumentRoutes.KindOf(collection) is not { } kind || !UserDocumentRoutes.IsDocumentId(documentId))
        {
            return new NotFoundResult();
        }

        return await documents.GetAsync(user.UserId, kind, documentId, cancellationToken) is { } document
            ? Json(document)
            : new NotFoundResult();
    }

    /// <summary>Saves a file; 409 with the current file when it changed since the client read it.</summary>
    [Function("SaveUserDocument")]
    public async Task<IActionResult> SaveAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "me/{collection}/{documentId}")] HttpRequest request,
        string collection,
        string documentId,
        CancellationToken cancellationToken)
    {
        if (await authenticator.AuthenticateAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (UserDocumentRoutes.KindOf(collection) is not { } kind || !UserDocumentRoutes.IsDocumentId(documentId))
        {
            return new NotFoundResult();
        }

        if (request.ContentLength > MaxBodyBytes)
        {
            return new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
        }

        if (await ReadWriteAsync(request, cancellationToken) is not { } write)
        {
            return new BadRequestObjectResult("The body must be { baseRevision, armySnapshotId?, content } with content a JSON object.");
        }

        return Outcome(await documents.SaveAsync(user.UserId, kind, documentId, write, cancellationToken));
    }

    /// <summary>Deletes a file; the query's baseRevision is the revision the client last read.</summary>
    [Function("DeleteUserDocument")]
    public async Task<IActionResult> DeleteAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "me/{collection}/{documentId}")] HttpRequest request,
        string collection,
        string documentId,
        CancellationToken cancellationToken)
    {
        if (await authenticator.AuthenticateAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (UserDocumentRoutes.KindOf(collection) is not { } kind || !UserDocumentRoutes.IsDocumentId(documentId))
        {
            return new NotFoundResult();
        }

        if (!int.TryParse(request.Query["baseRevision"], out var baseRevision))
        {
            return new BadRequestObjectResult("Give the revision last read as ?baseRevision=.");
        }

        return Outcome(await documents.DeleteAsync(user.UserId, kind, documentId, baseRevision, cancellationToken));
    }

    private static async Task<UserDocumentWrite?> ReadWriteAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var write = await JsonSerializer.DeserializeAsync<UserDocumentWrite>(request.Body, JsonOptions, cancellationToken);
            return write is { Content.ValueKind: JsonValueKind.Object, BaseRevision: >= 0 } &&
                   Encoding.UTF8.GetByteCount(write.Content.GetRawText()) <= MaxBodyBytes
                ? write
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IActionResult Outcome(SaveOutcome outcome) => outcome.Result switch
    {
        SaveResult.Saved => Json(outcome.Current),
        SaveResult.Conflict => new ConflictObjectResult(outcome.Current) { DeclaredType = typeof(UserDocumentView) },
        _ => new NotFoundResult()
    };

    private static JsonResult Json(object? value) => new(value, JsonOptions);
}
