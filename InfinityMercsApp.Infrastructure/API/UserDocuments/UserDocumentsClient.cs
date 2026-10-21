using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace InfinityMercsApp.Infrastructure.API.UserDocuments;

/// <inheritdoc/>
public sealed class UserDocumentsClient(HttpClient httpClient, UserDocumentsEndpoint endpoint) : IUserDocumentsApi
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { RespectNullableAnnotations = true };

    /// <inheritdoc/>
    public Task<UserDocumentsReply<IReadOnlyList<UserDocumentView<TContent>>>> ListAsync<TContent>(
        UserDocumentKind kind, string accessToken, CancellationToken cancellationToken = default)
        where TContent : class =>
        SendAsync<IReadOnlyList<UserDocumentView<TContent>>>(new HttpRequestMessage(HttpMethod.Get, endpoint.List(kind)), accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<UserDocumentsReply<UserDocumentView<TContent>>> GetAsync<TContent>(
        UserDocumentKind kind, string documentId, string accessToken, CancellationToken cancellationToken = default)
        where TContent : class =>
        SendAsync<UserDocumentView<TContent>>(new HttpRequestMessage(HttpMethod.Get, endpoint.Document(kind, documentId)), accessToken, cancellationToken);

    /// <inheritdoc/>
    public Task<UserDocumentsReply<UserDocumentView<TContent>>> SaveAsync<TContent>(
        UserDocumentKind kind, string documentId, UserDocumentWrite<TContent> write, string accessToken, CancellationToken cancellationToken = default)
        where TContent : class =>
        SendAsync<UserDocumentView<TContent>>(
            new HttpRequestMessage(HttpMethod.Put, endpoint.Document(kind, documentId)) { Content = JsonContent.Create(write, options: JsonOptions) },
            accessToken,
            cancellationToken);

    /// <inheritdoc/>
    public Task<UserDocumentsReply<UserDocumentView<TContent>>> DeleteAsync<TContent>(
        UserDocumentKind kind, string documentId, int baseRevision, string accessToken, CancellationToken cancellationToken = default)
        where TContent : class =>
        SendAsync<UserDocumentView<TContent>>(
            new HttpRequestMessage(HttpMethod.Delete, endpoint.Delete(kind, documentId, baseRevision)), accessToken, cancellationToken);

    private async Task<UserDocumentsReply<TValue>> SendAsync<TValue>(HttpRequestMessage request, string accessToken, CancellationToken cancellationToken)
        where TValue : class
    {
        using (request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                using var response = await httpClient.SendAsync(request, cancellationToken);
                var status = StatusOf(response.StatusCode);
                return status is UserDocumentsStatus.Ok or UserDocumentsStatus.Conflict
                    ? new UserDocumentsReply<TValue>(status, await ReadAsync<TValue>(response, cancellationToken))
                    : UserDocumentsReply<TValue>.Of(status);
            }
            catch (Exception ex) when ((ex is HttpRequestException or JsonException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
            {
                Console.Error.WriteLine($"{request.Method} {request.RequestUri} failed: {ex.Message}");
                return UserDocumentsReply<TValue>.Of(UserDocumentsStatus.Unavailable);
            }
        }
    }

    private static async Task<TValue?> ReadAsync<TValue>(HttpResponseMessage response, CancellationToken cancellationToken)
        where TValue : class =>
        response.Content.Headers.ContentLength == 0
            ? null
            : JsonSerializer.Deserialize<TValue>(await HttpBodies.ReadAsync(response, cancellationToken), JsonOptions);

    private static UserDocumentsStatus StatusOf(HttpStatusCode code) => code switch
    {
        >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous => UserDocumentsStatus.Ok,
        HttpStatusCode.Conflict => UserDocumentsStatus.Conflict,
        HttpStatusCode.NotFound => UserDocumentsStatus.NotFound,
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => UserDocumentsStatus.Unauthorized,
        HttpStatusCode.BadRequest or HttpStatusCode.RequestEntityTooLarge => UserDocumentsStatus.Rejected,
        _ => UserDocumentsStatus.Unavailable
    };
}
