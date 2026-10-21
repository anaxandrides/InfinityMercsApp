using InfinityMercsApp.ApiClient.UserDocuments;
using InfinityMercsApp.Domain.Models.Company;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Web.Files;
using NSubstitute;

namespace InfinityMercsApp.Web.UnitTests.Files;

public sealed class UserFilesTests
{
    private const string Token = "token-1";

    private readonly IUserDocumentsApi _api = Substitute.For<IUserDocumentsApi>();
    private readonly IAccessTokens _tokens = Substitute.For<IAccessTokens>();
    private readonly UserFiles _files;

    public UserFilesTests()
    {
        _tokens.GetAsync().Returns(Token);
        _files = new UserFiles(_api, _tokens);
    }

    [Fact]
    public async Task CompaniesAsync_SignedOut_AsksToSignIn()
    {
        _tokens.GetAsync().Returns((string?)null);

        var companies = await _files.CompaniesAsync();

        Assert.Equal(LoadStatus.SignInNeeded, companies.Status);
    }

    [Fact]
    public async Task CompaniesAsync_SomeDeleted_ListsTheRestByName()
    {
        Listed(UserDocumentKind.Company, View("b", deleted: false), View("gone", deleted: true), View("a", deleted: false));
        Fetches("a", TestFiles.Company("Amber Wolves"));
        Fetches("b", TestFiles.Company("Blue Lions"));

        var companies = await _files.CompaniesAsync();

        Assert.Equal(LoadStatus.Loaded, companies.Status);
        Assert.Equal(["Amber Wolves", "Blue Lions"], companies.Value?.Select(company => company.Name));
        await _api.DidNotReceive().GetAsync<SavedCompanyFile>(UserDocumentKind.Company, "gone", Token, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SeasonsAsync_Unreachable_SaysSo()
    {
        _api.ListAsync<SeasonFile>(UserDocumentKind.Season, Token, Arg.Any<CancellationToken>())
            .Returns(UserDocumentsReply<IReadOnlyList<UserDocumentView<SeasonFile>>>.Of(UserDocumentsStatus.Unavailable));

        Assert.Equal(LoadStatus.Unavailable, (await _files.SeasonsAsync()).Status);
    }

    [Fact]
    public async Task CompanyAsync_Deleted_IsNotFound()
    {
        _api.GetAsync<SavedCompanyFile>(UserDocumentKind.Company, "c1", Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(UserDocumentsStatus.Ok, new UserDocumentView<SavedCompanyFile> { DocumentId = "c1", Deleted = true }));

        Assert.Equal(LoadStatus.NotFound, (await _files.CompanyAsync("c1")).Status);
    }

    [Fact]
    public async Task CompanyAsync_TokenRefused_AsksToSignIn()
    {
        _api.GetAsync<SavedCompanyFile>(UserDocumentKind.Company, "c1", Token, Arg.Any<CancellationToken>())
            .Returns(UserDocumentsReply<UserDocumentView<SavedCompanyFile>>.Of(UserDocumentsStatus.Unauthorized));

        Assert.Equal(LoadStatus.SignInNeeded, (await _files.CompanyAsync("c1")).Status);
    }

    [Fact]
    public async Task SeasonAsync_Saved_ShowsIt()
    {
        _api.GetAsync<SeasonFile>(UserDocumentKind.Season, "s1", Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<UserDocumentView<SeasonFile>>(UserDocumentsStatus.Ok, new UserDocumentView<SeasonFile> { DocumentId = "s1", Content = TestFiles.Season() }));

        var season = await _files.SeasonAsync("s1");

        Assert.Equal("Blue Lions", season.Value?.CompanyName);
    }

    private void Listed(UserDocumentKind kind, params IEnumerable<UserDocumentView<SavedCompanyFile>> views) =>
        _api.ListAsync<SavedCompanyFile>(kind, Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<IReadOnlyList<UserDocumentView<SavedCompanyFile>>>(UserDocumentsStatus.Ok, views.ToList()));

    private void Fetches(string id, SavedCompanyFile company) =>
        _api.GetAsync<SavedCompanyFile>(UserDocumentKind.Company, id, Token, Arg.Any<CancellationToken>())
            .Returns(new UserDocumentsReply<UserDocumentView<SavedCompanyFile>>(UserDocumentsStatus.Ok, new UserDocumentView<SavedCompanyFile> { DocumentId = id, Content = company }));

    private static UserDocumentView<SavedCompanyFile> View(string id, bool deleted) => new() { DocumentId = id, Deleted = deleted };
}
