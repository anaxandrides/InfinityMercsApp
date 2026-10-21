using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Web.Presentation;

namespace InfinityMercsApp.Web.Files;

/// <summary>
/// The signed-in user's companies and seasons, read from the API. The API lists files without their content, so each
/// file is fetched for the lists' names and counts; a user has a handful, so that stays quick.
/// </summary>
public sealed class UserFiles(IUserDocumentsApi api, IAccessTokens tokens)
{
    public async Task<Loaded<IReadOnlyList<CompanySummary>>> CompaniesAsync(CancellationToken cancellationToken = default)
    {
        var files = await AllAsync<SavedCompanyFile>(UserDocumentKind.Company, cancellationToken);
        return files.Value is { } fetched
            ? new(LoadStatus.Loaded, fetched.Select(file => CompanySummary.From(file.Id, file.Content))
                .OrderBy(company => company.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList())
            : Loaded<IReadOnlyList<CompanySummary>>.Of(files.Status);
    }

    public async Task<Loaded<IReadOnlyList<SeasonSummary>>> SeasonsAsync(CancellationToken cancellationToken = default)
    {
        var files = await AllAsync<SeasonFile>(UserDocumentKind.Season, cancellationToken);
        return files.Value is { } fetched
            ? new(LoadStatus.Loaded, fetched.Select(file => SeasonSummary.From(file.Id, file.Content))
                .OrderByDescending(season => season.Started, StringComparer.Ordinal)
                .ToList())
            : Loaded<IReadOnlyList<SeasonSummary>>.Of(files.Status);
    }

    public async Task<Loaded<CompanySheet>> CompanyAsync(string id, CancellationToken cancellationToken = default)
    {
        var file = await OneAsync<SavedCompanyFile>(UserDocumentKind.Company, id, cancellationToken);
        return file.Value is { } company ? new(LoadStatus.Loaded, CompanySheet.From(id, company)) : Loaded<CompanySheet>.Of(file.Status);
    }

    public async Task<Loaded<SeasonSheet>> SeasonAsync(string id, CancellationToken cancellationToken = default)
    {
        var file = await OneAsync<SeasonFile>(UserDocumentKind.Season, id, cancellationToken);
        return file.Value is { } season ? new(LoadStatus.Loaded, SeasonSheet.From(id, season)) : Loaded<SeasonSheet>.Of(file.Status);
    }

    // Every file of the kind that hasn't been deleted, with its content.
    private async Task<Loaded<IReadOnlyList<FetchedFile<TContent>>>> AllAsync<TContent>(UserDocumentKind kind, CancellationToken cancellationToken)
        where TContent : class
    {
        if (await tokens.GetAsync() is not { } token)
        {
            return Loaded<IReadOnlyList<FetchedFile<TContent>>>.Of(LoadStatus.SignInNeeded);
        }

        var listing = await api.ListAsync<TContent>(kind, token, cancellationToken);
        if (listing is not { Status: UserDocumentsStatus.Ok, Value: { } views })
        {
            return Loaded<IReadOnlyList<FetchedFile<TContent>>>.Of(StatusOf(listing.Status));
        }

        var replies = await Task.WhenAll(views
            .Where(view => !view.Deleted)
            .Select(view => api.GetAsync<TContent>(kind, view.DocumentId, token, cancellationToken)));
        if (replies.FirstOrDefault(reply => reply.Status is not (UserDocumentsStatus.Ok or UserDocumentsStatus.NotFound)) is { } failed)
        {
            return Loaded<IReadOnlyList<FetchedFile<TContent>>>.Of(StatusOf(failed.Status));
        }

        var files = new List<FetchedFile<TContent>>();
        foreach (var reply in replies)
        {
            if (reply.Value is { Deleted: false, Content: { } content } view)
            {
                files.Add(new FetchedFile<TContent>(view.DocumentId, content));
            }
        }

        return new(LoadStatus.Loaded, files);
    }

    private async Task<Loaded<TContent>> OneAsync<TContent>(UserDocumentKind kind, string id, CancellationToken cancellationToken)
        where TContent : class
    {
        if (await tokens.GetAsync() is not { } token)
        {
            return Loaded<TContent>.Of(LoadStatus.SignInNeeded);
        }

        var reply = await api.GetAsync<TContent>(kind, id, token, cancellationToken);
        return reply switch
        {
            { Status: UserDocumentsStatus.Ok, Value: { Deleted: false, Content: { } content } } => new(LoadStatus.Loaded, content),
            { Status: UserDocumentsStatus.Ok } => Loaded<TContent>.Of(LoadStatus.NotFound),
            _ => Loaded<TContent>.Of(StatusOf(reply.Status))
        };
    }

    private static LoadStatus StatusOf(UserDocumentsStatus status) => status switch
    {
        UserDocumentsStatus.Unauthorized => LoadStatus.SignInNeeded,
        UserDocumentsStatus.NotFound => LoadStatus.NotFound,
        _ => LoadStatus.Unavailable
    };
}
