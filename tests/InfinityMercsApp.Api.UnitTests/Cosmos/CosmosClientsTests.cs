using InfinityMercsApp.Api.Cosmos;
using Microsoft.Extensions.Configuration;

namespace InfinityMercsApp.Api.UnitTests.Cosmos;

public sealed class CosmosClientsTests
{
    // The emulator's well-known key, published by Microsoft; not a secret.
    private const string EmulatorConnection =
        "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

    private static KeyValuePair<string, string?> Setting(string key, string value) => new(key, value);

    private static IConfiguration Settings(params IEnumerable<KeyValuePair<string, string?>> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Create_ConnectionString_PointsAtTheEmulator()
    {
        using var client = CosmosClients.Create(Settings(Setting("Cosmos:connectionString", EmulatorConnection)));

        Assert.Equal(new Uri("https://localhost:8081/"), client.Endpoint);
    }

    [Fact]
    public void Create_AccountEndpoint_SignsInWithTheIdentity()
    {
        using var client = CosmosClients.Create(Settings(Setting("Cosmos:accountEndpoint", "https://mercs.documents.azure.com:443/")));

        Assert.Equal(new Uri("https://mercs.documents.azure.com:443/"), client.Endpoint);
    }

    [Fact]
    public void Create_NeitherSet_SaysWhatToSet()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => CosmosClients.Create(Settings(Setting("Cosmos:accountEndpoint", string.Empty))));

        Assert.Contains("Cosmos:accountEndpoint", ex.Message, StringComparison.Ordinal);
    }
}
