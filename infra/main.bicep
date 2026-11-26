targetScope = 'resourceGroup'

@description('Short environment name used in resource names. Use lower-case letters and digits only.')
@minLength(2)
@maxLength(5)
param environmentName string = 'dev'

@description('Azure region for all resources. Static Web Apps Free supports a subset of regions.')
param location string = resourceGroup().location

@description('Short resource prefix. Use lower-case letters and digits only.')
@minLength(2)
@maxLength(5)
param namePrefix string = 'eob'

@description('SQL administrator login.')
@minLength(1)
@maxLength(128)
param sqlAdminLogin string = 'sqladmin'

@description('SQL administrator password. Supply it at deployment time and never commit it. Avoid semicolons because it is embedded in a connection string.')
@secure()
@minLength(12)
param sqlAdminPassword string

param databaseName string = 'EmployeeOnBoarding'
param serviceBusQueueName string = 'employee-events'
param storageContainerName string = 'profile-pictures'
param appServicePlanSkuName string = 'B1'
param appServicePlanSkuTier string = 'Basic'
param apiLinuxFxVersion string = 'DOTNETCORE|10.0'
param workerLinuxFxVersion string = 'DOTNET-ISOLATED|10.0'

@description('Allow all IPv4 addresses through the SQL firewall. Keep false outside isolated training subscriptions.')
param allowAllIpAddresses bool = false

@description('Optional single public IP address that can access SQL, typically the deployment agent or trainer laptop.')
param clientIpAddress string = ''

param tags object = {}

var suffix = substring(uniqueString(subscription().id, resourceGroup().id, environmentName), 0, 8)
var nameSuffix = '${environmentName}-${suffix}'
var resourceTags = union(tags, {
  application: 'employee-onboarding'
  environment: environmentName
  workload: 'azure-fundamentals-training'
})

var appInsightsName = 'appi-${namePrefix}-${nameSuffix}'
var logAnalyticsName = 'log-${namePrefix}-${nameSuffix}'
var keyVaultName = 'kv-${namePrefix}-${nameSuffix}'
var storageAccountName = 'st${namePrefix}${environmentName}${suffix}'
var serviceBusName = 'sb-${namePrefix}-${nameSuffix}'
var sqlServerName = 'sql-${namePrefix}-${nameSuffix}'
var appServicePlanName = 'plan-${namePrefix}-${nameSuffix}'
var staticWebAppName = 'swa-${namePrefix}-${nameSuffix}'
var apiAppName = 'app-${namePrefix}-api-${nameSuffix}'
var workerAppName = 'app-${namePrefix}-worker-${nameSuffix}'

module monitoring 'modules/appInsights.bicep' = {
  name: 'monitoring'
  params: {
    name: appInsightsName
    workspaceName: logAnalyticsName
    location: location
    tags: resourceTags
  }
}

module keyVault 'modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    name: keyVaultName
    location: location
    tenantId: tenant().tenantId
    tags: resourceTags
  }
}

module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    name: storageAccountName
    location: location
    containerName: storageContainerName
    tags: resourceTags
  }
}

module serviceBus 'modules/serviceBus.bicep' = {
  name: 'serviceBus'
  params: {
    name: serviceBusName
    location: location
    queueName: serviceBusQueueName
    tags: resourceTags
  }
}

module sql 'modules/sqlDatabase.bicep' = {
  name: 'sql'
  params: {
    serverName: sqlServerName
    databaseName: databaseName
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    location: location
    tags: resourceTags
    allowAllIpAddresses: allowAllIpAddresses
    clientIpAddress: clientIpAddress
  }
}

module plan 'modules/appServicePlan.bicep' = {
  name: 'appServicePlan'
  params: {
    name: appServicePlanName
    location: location
    tags: resourceTags
    skuName: appServicePlanSkuName
    skuTier: appServicePlanSkuTier
  }
}

module staticWebApp 'modules/staticWebApp.bicep' = {
  name: 'staticWebApp'
  params: {
    name: staticWebAppName
    location: location
    tags: resourceTags
  }
}

resource existingKeyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource existingStorageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' existing = {
  name: storageAccountName
}

resource existingServiceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusName
}

var sqlConnectionString = 'Server=tcp:${sql.outputs.fullyQualifiedDomainName},1433;Initial Catalog=${sql.outputs.databaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
var storageConnectionString = 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};AccountKey=${existingStorageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
var serviceBusConnectionString = existingServiceBusNamespace.listKeys().primaryConnectionString

resource sqlConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: existingKeyVault
  name: 'SqlConnectionString'
  properties: {
    value: sqlConnectionString
  }
  dependsOn: [
    keyVault
  ]
}

resource storageConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: existingKeyVault
  name: 'StorageConnectionString'
  properties: {
    value: storageConnectionString
  }
  dependsOn: [
    keyVault
    storage
  ]
}

resource serviceBusConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: existingKeyVault
  name: 'ServiceBusConnectionString'
  properties: {
    value: serviceBusConnectionString
  }
  dependsOn: [
    keyVault
    serviceBus
  ]
}

resource appInsightsConnectionSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: existingKeyVault
  name: 'ApplicationInsightsConnectionString'
  properties: {
    value: monitoring.outputs.connectionString
  }
  dependsOn: [
    keyVault
  ]
}

var sqlConnectionReference = '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/SqlConnectionString/)'
var storageConnectionReference = '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/StorageConnectionString/)'
var serviceBusConnectionReference = '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/ServiceBusConnectionString/)'
var appInsightsConnectionReference = '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/ApplicationInsightsConnectionString/)'
var frontendOrigin = 'https://${staticWebApp.outputs.defaultHostName}'

module apiApp 'modules/webApp.bicep' = {
  name: 'apiApp'
  dependsOn: [
    sqlConnectionSecret
    storageConnectionSecret
    serviceBusConnectionSecret
    appInsightsConnectionSecret
  ]
  params: {
    name: apiAppName
    location: location
    tags: resourceTags
    serverFarmId: plan.outputs.id
    linuxFxVersion: apiLinuxFxVersion
    healthCheckPath: '/health'
    alwaysOn: true
    appSettings: {
      ASPNETCORE_ENVIRONMENT: 'Production'
      WEBSITES_PORT: '8080'
      ConnectionStrings__SqlDatabase: sqlConnectionReference
      ConnectionStrings__BlobStorage: storageConnectionReference
      ServiceBus__ConnectionString: serviceBusConnectionReference
      ServiceBus__QueueName: serviceBusQueueName
      Storage__ContainerName: storageContainerName
      Cors__AllowedOrigins__0: frontendOrigin
      APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionReference
    }
  }
}

module workerApp 'modules/webApp.bicep' = {
  name: 'workerApp'
  dependsOn: [
    sqlConnectionSecret
    storageConnectionSecret
    serviceBusConnectionSecret
    appInsightsConnectionSecret
  ]
  params: {
    name: workerAppName
    location: location
    tags: resourceTags
    serverFarmId: plan.outputs.id
    linuxFxVersion: workerLinuxFxVersion
    healthCheckPath: ''
    alwaysOn: true
    kind: 'functionapp,linux'
    appSettings: {
      AzureWebJobsStorage: storageConnectionReference
      FUNCTIONS_EXTENSION_VERSION: '~4'
      FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
      WEBSITE_RUN_FROM_PACKAGE: '1'
      ConnectionStrings__SqlDatabase: sqlConnectionReference
      ServiceBusConnection: serviceBusConnectionReference
      ServiceBusQueueName: serviceBusQueueName
      APPLICATIONINSIGHTS_CONNECTION_STRING: appInsightsConnectionReference
    }
  }
}

var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource apiKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, apiAppName, keyVaultSecretsUserRoleId)
  scope: existingKeyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      keyVaultSecretsUserRoleId
    )
    principalId: apiApp.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

resource workerKeyVaultAccess 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, workerAppName, keyVaultSecretsUserRoleId)
  scope: existingKeyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      keyVaultSecretsUserRoleId
    )
    principalId: workerApp.outputs.principalId
    principalType: 'ServicePrincipal'
  }
}

output resourceGroupName string = resourceGroup().name
output location string = location
output frontendUrl string = 'https://${staticWebApp.outputs.defaultHostName}'
output apiUrl string = 'https://${apiApp.outputs.defaultHostName}'
output swaggerUrl string = 'https://${apiApp.outputs.defaultHostName}/swagger'
output apiAppName string = apiApp.outputs.name
output workerAppName string = workerApp.outputs.name
output staticWebAppName string = staticWebApp.outputs.name
output keyVaultName string = keyVault.outputs.name
output sqlServerName string = sql.outputs.name
output sqlDatabaseName string = sql.outputs.databaseName
output storageAccountName string = storage.outputs.name
output serviceBusNamespaceName string = serviceBus.outputs.name
output serviceBusQueueName string = serviceBusQueueName
output applicationInsightsName string = monitoring.outputs.name
