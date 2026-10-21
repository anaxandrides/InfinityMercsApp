using InfinityMercsApp.Domain.Validation;

namespace InfinityMercsApp.UnitTests.Validation;

public sealed class DocumentIdentifiersTests
{
    [Theory]
    [InlineData("3f2b8a3c-5d7e-4f1a-9b2c-1d2e3f4a5b6c", true)]
    [InlineData("company_1", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("../other-user", false)]
    [InlineData("has space", false)]
    public void IsValid_Text_IsLettersDigitsDashesAndUnderscores(string? identifier, bool expected) =>
        Assert.Equal(expected, DocumentIdentifiers.IsValid(identifier));

    [Fact]
    public void IsValid_TooLong_IsNot() => Assert.False(DocumentIdentifiers.IsValid(new string('a', 65)));
}
