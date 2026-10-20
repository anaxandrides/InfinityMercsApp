// Everything the app's online side runs on, in one resource group. Deployed by .github/workflows/infra.yml.
targetScope = 'subscription'

@description('Region for every resource.')
param location string = 'eastus'

@description('Short environment name, used in resource names.')
param environmentName string = 'prod'

@description('Short app name, used in resource names. Lower-case letters only, so it fits storage account names.')
@maxLength(10)
param appName string = 'infmercs'

@description('API instances kept warm, so the first request after a quiet spell is not a cold start.')
@minValue(0)
param alwaysReadyInstances int = 1

var tags = {
  app: 'InfinityMercsApp'
  environment: environmentName
}

resource group 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: 'rg-${appName}-${environmentName}'
  location: location
  tags: tags
}

module resources 'modules/resources.bicep' = {
  scope: group
  params: {
    location: location
    environmentName: environmentName
    appName: appName
    alwaysReadyInstances: alwaysReadyInstances
    tags: tags
  }
}

output resourceGroupName string = group.name
output functionAppName string = resources.outputs.functionAppName
output functionAppHostName string = resources.outputs.functionAppHostName
output cosmosEndpoint string = resources.outputs.cosmosEndpoint
