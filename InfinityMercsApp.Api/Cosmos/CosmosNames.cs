namespace InfinityMercsApp.Api.Cosmos;

/// <summary>The Cosmos DB database and containers, as infra/modules/cosmos.bicep creates them.</summary>
public static class CosmosNames
{
    public const string Database = "mercs";

    /// <summary>The army data snapshots, all in the one partition <see cref="ArmyPartition"/>.</summary>
    public const string ArmyContainer = "army";

    public const string ArmyPartition = "snapshots";

    /// <summary>Users' companies and seasons, partitioned by user (/userId).</summary>
    public const string UserDataContainer = "user-data";
}
