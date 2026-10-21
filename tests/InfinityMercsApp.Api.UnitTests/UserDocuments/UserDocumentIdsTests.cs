using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.ApiClient.UserDocuments;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

public sealed class UserDocumentIdsTests
{
    [Theory]
    [InlineData(UserDocumentKind.Company, "company-abc")]
    [InlineData(UserDocumentKind.Season, "season-abc")]
    public void For_KindAndId_PrefixesTheKind(UserDocumentKind kind, string expected) =>
        Assert.Equal(expected, UserDocumentIds.For(kind, "abc"));

}
