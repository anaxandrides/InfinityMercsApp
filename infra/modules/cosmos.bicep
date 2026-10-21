// Cosmos DB for companies, seasons and leagues: serverless, with key access off so only Entra identities get in.
// Each container comes with the feature that uses it.
param location string
param name string
param tags object

@description('The identity that reads and writes documents.')
param dataPrincipalId string

var databaseName = 'mercs'
var dataContributor = '00000000-0000-0000-0000-000000000002' // Cosmos DB Built-in Data Contributor

resource account 'Microsoft.DocumentDB/databaseAccounts@2024-11-15' = {
  name: name
  location: location
  tags: tags
  kind: 'GlobalDocumentDB'
  properties: {
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    disableLocalAuth: true
    minimalTlsVersion: 'Tls12'
  }

  resource database 'sqlDatabases' = {
    name: databaseName
    properties: {
      resource: {
        id: databaseName
      }
    }

    // Users' companies and seasons (InfinityMercsApp.Api CosmosNames), each user's in their own partition.
    resource userData 'containers' = {
      name: 'user-data'
      properties: {
        resource: {
          id: 'user-data'
          partitionKey: {
            paths: [
              '/userId'
            ]
            kind: 'Hash'
          }
        }
      }
    }

    // Army data snapshots (InfinityMercsApp.Api CosmosNames): a few small documents in one partition.
    resource army 'containers' = {
      name: 'army'
      properties: {
        resource: {
          id: 'army'
          partitionKey: {
            paths: [
              '/partition'
            ]
            kind: 'Hash'
          }
        }
      }
    }
  }

  resource dataRole 'sqlRoleAssignments' = {
    name: guid(account.id, dataPrincipalId, dataContributor)
    properties: {
      roleDefinitionId: '${account.id}/sqlRoleDefinitions/${dataContributor}'
      principalId: dataPrincipalId
      scope: account.id
    }
  }
}

output endpoint string = account.properties.documentEndpoint
