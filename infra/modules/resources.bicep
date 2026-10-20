// The resource group's contents: the API's identity, monitoring, storage, Cosmos DB and the Function app.
param location string
param environmentName string
param appName string
param alwaysReadyInstances int
param tags object

var suffix = uniqueString(subscription().id, appName, environmentName)

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${appName}-api-${environmentName}'
  location: location
  tags: tags
}

module monitoring 'monitoring.bicep' = {
  params: {
    location: location
    name: '${appName}-${environmentName}'
    tags: tags
    publisherPrincipalId: identity.properties.principalId
  }
}

module storage 'storage.bicep' = {
  params: {
    location: location
    name: take('st${appName}${suffix}', 24)
    tags: tags
    dataPrincipalId: identity.properties.principalId
  }
}

module cosmos 'cosmos.bicep' = {
  params: {
    location: location
    name: 'cosmos-${appName}-${suffix}'
    tags: tags
    dataPrincipalId: identity.properties.principalId
  }
}

module functions 'functions.bicep' = {
  params: {
    location: location
    name: 'func-${appName}-${suffix}'
    planName: 'plan-${appName}-${environmentName}'
    tags: tags
    identityId: identity.id
    identityClientId: identity.properties.clientId
    storageAccountName: storage.outputs.name
    blobEndpoint: storage.outputs.blobEndpoint
    deploymentContainer: storage.outputs.deploymentContainer
    appInsightsConnectionString: monitoring.outputs.connectionString
    cosmosEndpoint: cosmos.outputs.endpoint
    alwaysReadyInstances: alwaysReadyInstances
  }
}

output functionAppName string = functions.outputs.name
output functionAppHostName string = functions.outputs.hostName
output cosmosEndpoint string = cosmos.outputs.endpoint
