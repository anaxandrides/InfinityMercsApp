using InfinityMercsApp.Api.UserDocuments;
using InfinityMercsApp.Infrastructure.API.UserDocuments;

namespace InfinityMercsApp.Api.UnitTests.UserDocuments;

public sealed class UserDocumentRoutesTests
{
    [Theory]
    [InlineData("companies", UserDocumentKind.Company)]
    [InlineData("seasons", UserDocumentKind.Season)]
    [InlineData("leagues", null)]
    [InlineData("Companies", null)]
    public void KindOf_Collection_IsItsKind(string collection, UserDocumentKind? expected) =>
        Assert.Equal(expected, UserDocumentRoutes.KindOf(collection));

    [Theory]
    [InlineData("3f2b8a3c-5d7e-4f1a-9b2c-1d2e3f4a5b6c", true)]
    [InlineData("company_1", true)]
    [InlineData("", false)]
    [InlineData("../other-user", false)]
    [InlineData("has space", false)]
    public void IsDocumentId_Text_IsLettersDigitsDashesAndUnderscores(string documentId, bool expected) =>
        Assert.Equal(expected, UserDocumentRoutes.IsDocumentId(documentId));

    [Fact]
    public void IsDocumentId_TooLong_IsNot() => Assert.False(UserDocumentRoutes.IsDocumentId(new string('a', 65)));
}
