// The web app: a Static Web App serving the Blazor WebAssembly site. Deployed by .github/workflows/web.yml with the
// site's deployment token, which the workflow reads at deploy time with the deploy identity, so it is never stored.
param location string
param name string
param tags object

@description('The site\'s own domain, e.g. www.example.com; empty for none. Azure checks it by its CNAME to the site\'s azurestaticapps.net host name, so that record must exist first.')
param customDomain string = ''

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

resource domain 'Microsoft.Web/staticSites/customDomains@2024-04-01' = if (!empty(customDomain)) {
  parent: site
  name: empty(customDomain) ? 'unused' : customDomain
  properties: {}
}

output name string = site.name
output hostName string = site.properties.defaultHostname

@description('Where people reach the site: its own domain when it has one.')
output address string = 'https://${empty(customDomain) ? site.properties.defaultHostname : customDomain}'
