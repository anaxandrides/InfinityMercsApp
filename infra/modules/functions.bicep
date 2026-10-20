// The API: a Flex Consumption Function app on .NET 10, running as the user-assigned identity.
param location string
param name string
param planName string
param tags object
param identityId string
param identityClientId string
param storageAccountName string
param blobEndpoint string
param deploymentContainer string
param appInsightsConnectionString string
param cosmosEndpoint string
param alwaysReadyInstances int

resource plan 'Microsoft.Web/serverfarms@2024-04-01' = {
  name: planName
  location: location
  tags: tags
  kind: 'functionapp'
  sku: {
    tier: 'FlexConsumption'
    name: 'FC1'
  }
  properties: {
    reserved: true
  }
}

resource site 'Microsoft.Web/sites@2024-04-01' = {
  name: name
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      minTlsVersion: '1.2'
    }
    functionAppConfig: {
      deployment: {
        storage: {
          type: 'blobContainer'
          value: '${blobEndpoint}${deploymentContainer}'
          authentication: {
            type: 'UserAssignedIdentity'
            userAssignedIdentityResourceId: identityId
          }
        }
      }
      scaleAndConcurrency: {
        maximumInstanceCount: 40
        instanceMemoryMB: 2048
        alwaysReady: alwaysReadyInstances > 0 ? [
          {
            name: 'http'
            instanceCount: alwaysReadyInstances
          }
        ] : []
      }
      runtime: {
        name: 'dotnet-isolated'
        version: '10.0'
      }
    }
  }

  resource settings 'config' = {
    name: 'appsettings'
    properties: {
      AzureWebJobsStorage__accountName: storageAccountName
      AzureWebJobsStorage__credential: 'managedidentity'
      AzureWebJobsStorage__clientId: identityClientId
      AZURE_CLIENT_ID: identityClientId
      APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
      APPLICATIONINSIGHTS_AUTHENTICATION_STRING: 'ClientId=${identityClientId};Authorization=AAD'
      ArmyStorage__serviceUri: blobEndpoint
      ArmyStorage__credential: 'managedidentity'
      ArmyStorage__clientId: identityClientId
      Cosmos__accountEndpoint: cosmosEndpoint
    }
  }
}

output name string = site.name
output hostName string = site.properties.defaultHostName
