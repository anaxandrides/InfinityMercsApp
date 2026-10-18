using InfinityMercsApp.Domain.Company;

namespace InfinityMercsApp.UnitTests.Company;

public sealed class ProxyPackTests
{
    [Theory]
    [InlineData("Proxy Pack", true)]
    [InlineData(" proxy pack ", true)]
    [InlineData("TAG Company", false)]
    [InlineData(null, false)]
    public void Is_CompanyType_TrueForAProxyPack(string? companyType, bool isProxyPack) =>
        Assert.Equal(isProxyPack, ProxyPack.Is(companyType));
}
