// The web app: a Static Web App serving the Blazor WebAssembly site. Deployed by .github/workflows/web.yml with the
// site's deployment token, which the workflow reads at deploy time with the deploy identity, so it is never stored.
param location string
param name string
param tags object

resource site 'Microsoft.Web/staticSites@2024-04-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Disabled'
    allowConfigFileUpdates: true
  }
}

output name string = site.name
output hostName string = site.properties.defaultHostname
