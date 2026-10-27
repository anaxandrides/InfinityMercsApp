using System.Net;
using Microsoft.Azure.Cosmos;

namespace InfinityMercsApp.Api.UnitTests.Cosmos;

/// <summary>The errors Cosmos DB answers with.</summary>
internal static class CosmosFailures
{
    public static CosmosException With(HttpStatusCode status) => new($"Cosmos answered {status}.", status, 0, string.Empty, 0);
}
