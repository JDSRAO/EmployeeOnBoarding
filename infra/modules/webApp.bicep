param name string
param location string
param tags object
param serverFarmId string
param linuxFxVersion string
param appSettings object
param healthCheckPath string
param alwaysOn bool
param kind string = 'app,linux'

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: serverFarmId
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: union({
      linuxFxVersion: linuxFxVersion
      alwaysOn: alwaysOn
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      http20Enabled: true
      appSettings: [
        for item in items(appSettings): {
          name: item.key
          value: item.value
        }
      ]
    }, empty(healthCheckPath) ? {} : {
      healthCheckPath: healthCheckPath
    })
  }
}

output id string = webApp.id
output name string = webApp.name
output defaultHostName string = webApp.properties.defaultHostName
output principalId string = webApp.identity.principalId
