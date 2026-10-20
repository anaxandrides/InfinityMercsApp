using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

public sealed class UserDocumentIdsTests
{
    [Theory]
    [InlineData(UserDocumentKind.Company, "company-abc")]
    [InlineData(UserDocumentKind.Season, "season-abc")]
    public void For_KindAndId_PrefixesTheKind(UserDocumentKind kind, string expected) =>
        Assert.Equal(expected, UserDocumentIds.For(kind, "abc"));

    [Theory]
    [InlineData("3f2b8a3c-5d7e-4f1a-9b2c-1d2e3f4a5b6c", true)]
    [InlineData("company_1", true)]
    [InlineData("", false)]
    [InlineData("../other-user", false)]
    [InlineData("has space", false)]
    public void IsValid_Text_IsLettersDigitsDashesAndUnderscores(string documentId, bool expected) =>
        Assert.Equal(expected, UserDocumentIds.IsValid(documentId));

    [Fact]
    public void IsValid_TooLong_IsNot() => Assert.False(UserDocumentIds.IsValid(new string('a', 65)));
}
