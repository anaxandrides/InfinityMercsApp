using InfinityMercsApp.Domain.Leagues;

namespace InfinityMercsApp.UnitTests.Leagues;

public sealed class InviteCodesTests
{
    public static TheoryData<string, string> TypedCodes => new()
    {
        { "ABC234", "ABC234" },
        { "abc234", "ABC234" },
        { " abc-234 ", "ABC234" },
        { "ab c2 34", "ABC234" }
    };

    [Theory]
    [MemberData(nameof(TypedCodes))]
    public void Parse_TypedAnyWay_ReadsTheCode(string typed, string code) => Assert.Equal(code, InviteCodes.Parse(typed));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ABC23")]
    [InlineData("ABC2345")]
    [InlineData("ABC0I1")]
    public void Parse_NotACode_IsNull(string? typed) => Assert.Null(InviteCodes.Parse(typed));

    [Fact]
    public void Alphabet_Always_LeavesOutLookAlikes() => Assert.DoesNotContain(InviteCodes.Alphabet, c => "01OIL".Contains(c));

    [Theory]
    [InlineData("ABC234", true)]
    [InlineData("abc234", false)]
    [InlineData("ABCDE", false)]
    public void IsValid_Code_SaysWhetherItIsOneAsIssued(string code, bool valid) => Assert.Equal(valid, InviteCodes.IsValid(code));
}
