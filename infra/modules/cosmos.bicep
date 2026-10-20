// Cosmos DB for companies, seasons and leagues: serverless, with key access off so only Entra identities get in.
// The containers come with the features that use them.
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
