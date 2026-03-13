@description('Name of the function app')
param functionAppName string = 'allowance-function'

@description('Location for resources')
param location string = resourceGroup().location

@description('Resource ID of the existing App Service Plan (can be cross-resource-group)')
param appServicePlanId string

@description('SQL Server connection string')
@secure()
param connectionString string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Azure Communication Services connection string for email')
@secure()
param acsConnectionString string

@description('From email address for notifications')
param fromEmail string = 'noreply@earnandlearn.app'

@description('CORS allowed origins')
param corsAllowedOrigins array = [
  'https://earnandlearn.app'
  'https://www.earnandlearn.app'
]

// Storage account required for Azure Functions runtime
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${replace(functionAppName, '-', '')}sa'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

// Function App on the dedicated App Service Plan
resource functionApp 'Microsoft.Web/sites@2024-04-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      cors: {
        allowedOrigins: corsAllowedOrigins
      }
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=core.windows.net'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: connectionString
        }
        {
          name: 'AzureEmail__ConnectionString'
          value: acsConnectionString
        }
        {
          name: 'AzureEmail__FromEmail'
          value: fromEmail
        }
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostname string = functionApp.properties.defaultHostName
