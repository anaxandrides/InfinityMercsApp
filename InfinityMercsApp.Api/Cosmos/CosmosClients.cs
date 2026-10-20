using System.Text.Json;
using Azure.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace InfinityMercsApp.Api.Cosmos;

/// <summary>Builds the Cosmos DB client from the "Cosmos" settings.</summary>
public static class CosmosClients
{
    /// <summary>
    /// In Azure, "Cosmos:accountEndpoint" is set and the Function app's managed identity signs in; locally,
    /// "Cosmos:connectionString" points at the emulator.
    /// </summary>
    public static CosmosClient Create(IConfiguration configuration)
    {
        var settings = configuration.GetSection("Cosmos");
        var options = new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        };

        if (settings["connectionString"] is { Length: > 0 } connectionString)
        {
            return new CosmosClient(connectionString, options);
        }

        var endpoint = settings["accountEndpoint"] is { Length: > 0 } text
            ? text
            : throw new InvalidOperationException("Set Cosmos:accountEndpoint, or Cosmos:connectionString for the emulator.");
        return new CosmosClient(endpoint, new DefaultAzureCredential(), options);
    }
}
