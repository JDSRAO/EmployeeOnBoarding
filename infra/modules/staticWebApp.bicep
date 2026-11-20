param name string
param location string
param tags object

resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {}
}

output id string = staticWebApp.id
output name string = staticWebApp.name
output defaultHostName string = staticWebApp.properties.defaultHostname
