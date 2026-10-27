using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Site;

/// <summary>What the account's API answers with: its companies and seasons, or a failure.</summary>
internal sealed class AccountFiles(IUserDocumentsApi api)
{
    private const string Token = "token-1";

    public void Companies(params IEnumerable<SavedCompanyFile> companies)
    {
        var list = companies.ToList();
        api.ListAsync<SavedCompanyFile>(UserDocumentKind.Company, Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<IReadOnlyList<UserDocumentView<SavedCompanyFile>>>(
                UserDocumentsStatus.Ok, list.Select(company => new UserDocumentView<SavedCompanyFile> { DocumentId = company.CompanyIdentifier }).ToList()));
        foreach (var company in list)
        {
            api.GetAsync<SavedCompanyFile>(UserDocumentKind.Company, company.CompanyIdentifier, Token, Arg.Any<CancellationToken>())
                .Returns(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(
                    UserDocumentsStatus.Ok, new UserDocumentView<SavedCompanyFile> { DocumentId = company.CompanyIdentifier, Content = company }));
        }
    }

    public void Seasons(params IEnumerable<SeasonFile> seasons)
    {
        var list = seasons.ToList();
        api.ListAsync<SeasonFile>(UserDocumentKind.Season, Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<IReadOnlyList<UserDocumentView<SeasonFile>>>(
                UserDocumentsStatus.Ok, list.Select(season => new UserDocumentView<SeasonFile> { DocumentId = season.SeasonIdentifier }).ToList()));
        foreach (var season in list)
        {
            api.GetAsync<SeasonFile>(UserDocumentKind.Season, season.SeasonIdentifier, Token, Arg.Any<CancellationToken>())
                .Returns(new UserDocumentsReply<UserDocumentView<SeasonFile>>(
                    UserDocumentsStatus.Ok, new UserDocumentView<SeasonFile> { DocumentId = season.SeasonIdentifier, Content = season }));
        }
    }

    public void Unreachable()
    {
        api.ListAsync<SavedCompanyFile>(UserDocumentKind.Company, Token, Arg.Any<CancellationToken>())
            .Returns(UserDocumentsReply<IReadOnlyList<UserDocumentView<SavedCompanyFile>>>.Of(UserDocumentsStatus.Unavailable));
        api.ListAsync<SeasonFile>(UserDocumentKind.Season, Token, Arg.Any<CancellationToken>())
            .Returns(UserDocumentsReply<IReadOnlyList<UserDocumentView<SeasonFile>>>.Of(UserDocumentsStatus.Unavailable));
        api.GetAsync<SavedCompanyFile>(UserDocumentKind.Company, Arg.Any<string>(), Token, Arg.Any<CancellationToken>())
            .Returns(UserDocumentsReply<UserDocumentView<SavedCompanyFile>>.Of(UserDocumentsStatus.Unavailable));
        api.GetAsync<SeasonFile>(UserDocumentKind.Season, Arg.Any<string>(), Token, Arg.Any<CancellationToken>())
            .Returns(UserDocumentsReply<UserDocumentView<SeasonFile>>.Of(UserDocumentsStatus.Unavailable));
    }
}
