using 'main.bicep'

param location = 'eastus'
param environmentName = 'prod'

// Filled in once the Entra External ID tenant and the API's app registration exist (docs/azure-setup.md).
param authAuthority = ''
param authAudience = ''
