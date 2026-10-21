using System.Text.Json;
using InfinityMercsApp.Api.Auth;
using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InfinityMercsApp.Api.UserDocuments;

/// <summary>
/// What the company and season routes share: every route needs a valid sign-in token, and a user only ever reaches
/// their own files.
/// </summary>
public sealed class UserDocumentEndpoints(IUserAuthenticator authenticator, UserDocumentService documents)
{
    /// <summary>The largest body accepted; Cosmos DB keeps documents of up to 2 MB.</summary>
    public const int MaxBodyBytes = 1_500_000;

    /// <summary>Bodies must give every required and non-nullable property, so a file can't arrive with parts missing.</summary>
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web) { RespectNullableAnnotations = true };

    public async Task<IActionResult> ListAsync<TContent>(HttpRequest request, UserDocumentKind kind, CancellationToken cancellationToken)
        where TContent : class =>
        await authenticator.AuthenticateAsync(request, cancellationToken) is { } user
            ? Json(await documents.ListAsync<TContent>(user.UserId, kind, cancellationToken))
            : new UnauthorizedResult();

    public async Task<IActionResult> GetAsync<TContent>(HttpRequest request, UserDocumentKind kind, string documentId, CancellationToken cancellationToken)
        where TContent : class
    {
        if (await authenticator.AuthenticateAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        return DocumentIdentifiers.IsValid(documentId) &&
               await documents.GetAsync<TContent>(user.UserId, kind, documentId, cancellationToken) is { } document
            ? Json(document)
            : new NotFoundResult();
    }

    /// <summary>Saves a file once <paramref name="checks"/> pass; 409 with the current file when it changed since the client read it.</summary>
    public async Task<IActionResult> SaveAsync<TContent>(
        HttpRequest request,
        UserDocumentKind kind,
        string documentId,
        IUserFileChecks<TContent> checks,
        CancellationToken cancellationToken)
        where TContent : class
    {
        if (await authenticator.AuthenticateAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (!DocumentIdentifiers.IsValid(documentId))
        {
            return new NotFoundResult();
        }

        if (await ReadBodyAsync(request, cancellationToken) is not { } body)
        {
            return new StatusCodeResult(StatusCodes.Status413PayloadTooLarge);
        }

        if (Parse<TContent>(body) is not { BaseRevision: >= 0 } write)
        {
            return new BadRequestObjectResult(new List<string> { "The body must be { baseRevision, armySnapshotId?, content } with every part of the file present." });
        }

        if (checks.ProblemsWith(documentId, write.Content) is { Count: > 0 } problems)
        {
            return new BadRequestObjectResult(problems);
        }

        return Outcome(await documents.SaveAsync(user.UserId, kind, documentId, write, cancellationToken));
    }

    /// <summary>Deletes a file; the query's baseRevision is the revision the client last read.</summary>
    public async Task<IActionResult> DeleteAsync<TContent>(HttpRequest request, UserDocumentKind kind, string documentId, CancellationToken cancellationToken)
        where TContent : class
    {
        if (await authenticator.AuthenticateAsync(request, cancellationToken) is not { } user)
        {
            return new UnauthorizedResult();
        }

        if (!DocumentIdentifiers.IsValid(documentId))
        {
            return new NotFoundResult();
        }

        if (!int.TryParse(request.Query["baseRevision"], out var baseRevision))
        {
            return new BadRequestObjectResult(new List<string> { "Give the revision last read as ?baseRevision=." });
        }

        return Outcome(await documents.DeleteAsync<TContent>(user.UserId, kind, documentId, baseRevision, cancellationToken));
    }

    // The body, or null when it is larger than MaxBodyBytes.
    private static async Task<byte[]?> ReadBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength > MaxBodyBytes)
        {
            return null;
        }

        using var body = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await request.Body.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (body.Length + read > MaxBodyBytes)
            {
                return null;
            }

            body.Write(buffer, 0, read);
        }

        return body.ToArray();
    }

    private static UserDocumentWrite<TContent>? Parse<TContent>(byte[] body)
        where TContent : class
    {
        try
        {
            return JsonSerializer.Deserialize<UserDocumentWrite<TContent>>(body, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IActionResult Outcome<TContent>(SaveOutcome<TContent> outcome)
        where TContent : class => outcome.Result switch
    {
        SaveResult.Saved => Json(outcome.Current),
        SaveResult.Conflict => new ConflictObjectResult(outcome.Current) { DeclaredType = typeof(UserDocumentView<TContent>) },
        _ => new NotFoundResult()
    };

    private static JsonResult Json(object? value) => new(value, JsonOptions);
}
