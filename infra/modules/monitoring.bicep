// Log Analytics and Application Insights. Local (key) auth is off, so the API publishes as its managed identity.
param location string
param name string
param tags object

@description('The identity allowed to publish telemetry.')
param publisherPrincipalId string

@description('The most the workspace takes in a day, in GB; ingestion stops until the next day once it is reached, so a flood of app telemetry can only cost so much.')
param dailyCapGb int = 1

var monitoringMetricsPublisher = '3913510d-42f4-4e42-8a64-420c390055eb'

resource workspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${name}'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
    workspaceCapping: {
      dailyQuotaGb: dailyCapGb
    }
  }
}

resource insights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${name}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: workspace.id
    DisableLocalAuth: true
  }
}

resource publisher 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(insights.id, publisherPrincipalId, monitoringMetricsPublisher)
  scope: insights
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', monitoringMetricsPublisher)
    principalId: publisherPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output connectionString string = insights.properties.ConnectionString
