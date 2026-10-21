// The API's storage account: the Functions host's own state, the deployment package, and the army data mirror.
// Shared keys are off, so everything signs in with a managed identity.
param location string
param name string
param tags object

@description('The identity that reads and writes the account: blobs, plus the queues and tables the Functions host uses.')
param dataPrincipalId string

var deploymentContainerName = 'app-package'
var armyDataContainerName = 'army-data'
var roles = [
  'b7e6dc6d-f1e8-4753-8033-0f276bb0955b' // Storage Blob Data Owner
  '974c5e8b-45b9-4653-ba55-5f855dd0fb88' // Storage Queue Data Contributor
  '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3' // Storage Table Data Contributor
]

resource account 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowSharedKeyAccess: false
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }

  resource blobService 'blobServices' = {
    name: 'default'

    resource deployment 'containers' = {
      name: deploymentContainerName
    }

    resource armyData 'containers' = {
      name: armyDataContainerName
    }
  }
}

resource dataRoles 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for role in roles: {
  name: guid(account.id, dataPrincipalId, role)
  scope: account
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role)
    principalId: dataPrincipalId
    principalType: 'ServicePrincipal'
  }
}]

output name string = account.name
output blobEndpoint string = account.properties.primaryEndpoints.blob
output deploymentContainer string = deploymentContainerName
