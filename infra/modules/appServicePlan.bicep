param name string
param location string
param tags object
param skuName string
param skuTier string

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: skuName
    tier: skuTier
    capacity: 1
  }
  properties: {
    reserved: true
    zoneRedundant: false
  }
}

output id string = plan.id
output name string = plan.name
