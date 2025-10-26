# Azure Deployment Setup Guide

This guide covers the complete setup for deploying the Allowance Tracker application to Azure using GitHub Actions.

## Architecture Overview

The application is deployed across three Azure services:

1. **Azure App Service** - Hosts the .NET 8 Web API
2. **Azure Functions** - Runs the weekly allowance background job
3. **Azure Storage (Static Website)** - Hosts the React frontend
4. **Azure CDN** (Optional) - Provides global distribution and caching for the React app

## Prerequisites

- Azure subscription
- Azure CLI installed locally
- GitHub repository with admin access
- .NET 8 SDK (for local testing)
- Node.js 20.x (for local testing)

## 1. Azure Resource Setup

### 1.1 Create Resource Group

```bash
# Set variables
RESOURCE_GROUP="allowancetracker-rg"
LOCATION="eastus"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION
```

### 1.2 Create Azure App Service for API

```bash
# Variables
APP_SERVICE_PLAN="allowancetracker-plan"
WEB_APP_NAME="allowancetracker-api"

# Create App Service Plan (Linux, .NET 8)
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNET|8.0"

# Configure always on (keeps app warm)
az webapp config set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --always-on true

# Enable HTTPS only
az webapp update \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true
```

### 1.3 Create Azure SQL Database (for production)

```bash
# Variables
SQL_SERVER_NAME="allowancetracker-sql"
SQL_ADMIN_USER="sqladmin"
SQL_ADMIN_PASSWORD="<YourSecurePassword123!>"
DATABASE_NAME="allowancetracker"

# Create SQL Server
az sql server create \
  --name $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $SQL_ADMIN_USER \
  --admin-password $SQL_ADMIN_PASSWORD

# Allow Azure services to access SQL Server
az sql server firewall-rule create \
  --name AllowAzureServices \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Create database
az sql db create \
  --name $DATABASE_NAME \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --service-objective S0 \
  --backup-storage-redundancy Local

# Get connection string
az sql db show-connection-string \
  --client ado.net \
  --server $SQL_SERVER_NAME \
  --name $DATABASE_NAME
```

### 1.4 Create Azure Function App

```bash
# Variables
FUNCTION_APP_NAME="allowancetracker-func"
STORAGE_ACCOUNT="allowancetrackerfunc"

# Create storage account for function app
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

# Create Function App
az functionapp create \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --storage-account $STORAGE_ACCOUNT \
  --os-type Linux
```

### 1.5 Create Azure Storage for React App

```bash
# Variables
STORAGE_ACCOUNT_WEB="allowancetrackerapp"

# Create storage account
az storage account create \
  --name $STORAGE_ACCOUNT_WEB \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2

# Enable static website hosting
az storage blob service-properties update \
  --account-name $STORAGE_ACCOUNT_WEB \
  --static-website \
  --index-document index.html \
  --404-document index.html

# Get the static website URL
az storage account show \
  --name $STORAGE_ACCOUNT_WEB \
  --resource-group $RESOURCE_GROUP \
  --query "primaryEndpoints.web" \
  --output tsv
```

### 1.6 Create Azure CDN (Optional but Recommended)

```bash
# Variables
CDN_PROFILE="allowancetracker-cdn"
CDN_ENDPOINT="allowancetracker"

# Create CDN profile
az cdn profile create \
  --name $CDN_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --sku Standard_Microsoft

# Create CDN endpoint
az cdn endpoint create \
  --name $CDN_ENDPOINT \
  --resource-group $RESOURCE_GROUP \
  --profile-name $CDN_PROFILE \
  --origin $(az storage account show --name $STORAGE_ACCOUNT_WEB --query "primaryEndpoints.web" --output tsv | sed 's|https://||' | sed 's|/||') \
  --origin-host-header $(az storage account show --name $STORAGE_ACCOUNT_WEB --query "primaryEndpoints.web" --output tsv | sed 's|https://||' | sed 's|/||')

# Enable compression
az cdn endpoint update \
  --name $CDN_ENDPOINT \
  --resource-group $RESOURCE_GROUP \
  --profile-name $CDN_PROFILE \
  --enable-compression true

# Get CDN endpoint URL
echo "CDN URL: https://$CDN_ENDPOINT.azureedge.net"
```

## 2. Configure Application Settings

### 2.1 API App Service Settings

```bash
# Set connection string
az webapp config connection-string set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Database=$DATABASE_NAME;User ID=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;Encrypt=true;Connection Timeout=30;"

# Set application settings
az webapp config appsettings set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    Jwt__Secret="<YourJwtSecretKeyHere>" \
    Jwt__Issuer="https://$WEB_APP_NAME.azurewebsites.net" \
    Jwt__Audience="https://$WEB_APP_NAME.azurewebsites.net" \
    Jwt__ExpirationInHours="24" \
    CORS__AllowedOrigins="https://$CDN_ENDPOINT.azureedge.net,https://$STORAGE_ACCOUNT_WEB.z13.web.core.windows.net"
```

### 2.2 Azure Function Settings

```bash
# Set application settings for Function App
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ConnectionStrings__DefaultConnection="Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Database=$DATABASE_NAME;User ID=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;Encrypt=true;Connection Timeout=30;" \
    ASPNETCORE_ENVIRONMENT="Production"
```

## 3. GitHub Secrets Configuration

### 3.1 Create Azure Service Principal

Create a service principal with contributor access to your resource group:

```bash
# Create service principal
az ad sp create-for-rbac \
  --name "allowancetracker-github-actions" \
  --role contributor \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

This will output JSON credentials. Copy the entire JSON output.

### 3.2 Configure GitHub Repository Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions

Create the following secrets:

#### Required Secrets

| Secret Name | Description | Example Value |
|-------------|-------------|---------------|
| `AZURE_CREDENTIALS` | Service principal credentials (JSON from step 3.1) | `{"clientId": "...", "clientSecret": "...", ...}` |
| `VITE_API_URL` | Production API URL for React app | `https://allowancetracker-api.azurewebsites.net` |

#### Optional Secrets (if using different names)

| Secret Name | Description | Default Value |
|-------------|-------------|---------------|
| `AZURE_WEBAPP_NAME` | App Service name | `allowancetracker-api` |
| `AZURE_FUNCTIONAPP_NAME` | Function App name | `allowancetracker-func` |
| `AZURE_STORAGE_ACCOUNT` | Storage account for web | `allowancetrackerapp` |

### 3.3 Example AZURE_CREDENTIALS JSON

```json
{
  "clientId": "12345678-1234-1234-1234-123456789012",
  "clientSecret": "your-client-secret-here",
  "subscriptionId": "12345678-1234-1234-1234-123456789012",
  "tenantId": "12345678-1234-1234-1234-123456789012",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

## 4. Workflow Configuration

The deployment workflows are located in `.github/workflows/`:

- **deploy-api.yml** - Deploys .NET API to Azure App Service
- **deploy-function.yml** - Deploys Azure Function
- **deploy-web.yml** - Deploys React app to Azure Storage

### 4.1 Update Workflow Variables

Edit each workflow file to match your Azure resource names:

**deploy-api.yml**:
```yaml
env:
  AZURE_WEBAPP_NAME: 'allowancetracker-api'  # Update if different
```

**deploy-function.yml**:
```yaml
env:
  AZURE_FUNCTIONAPP_NAME: 'allowancetracker-func'  # Update if different
```

**deploy-web.yml**:
```yaml
env:
  AZURE_STORAGE_ACCOUNT: 'allowancetrackerapp'  # Update if different
  AZURE_CDN_ENDPOINT: 'allowancetracker'  # Update if different
  AZURE_CDN_PROFILE: 'allowancetracker-cdn'  # Update if different
  AZURE_RESOURCE_GROUP: 'allowancetracker-rg'  # Update if different
```

## 5. Deploy to Azure

### 5.1 Automatic Deployment

Workflows trigger automatically on push to `main` branch:

```bash
git add .
git commit -m "Add Azure deployment workflows"
git push origin main
```

### 5.2 Manual Deployment

You can also trigger deployments manually:

1. Go to GitHub repository → Actions
2. Select the workflow (e.g., "Deploy API to Azure App Service")
3. Click "Run workflow"
4. Select branch and click "Run workflow"

## 6. Post-Deployment Configuration

### 6.1 Configure Custom Domain (Optional)

For production, configure a custom domain:

```bash
# Add custom domain to App Service
az webapp config hostname add \
  --webapp-name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --hostname api.yourdomain.com

# Add custom domain to CDN
az cdn custom-domain create \
  --endpoint-name $CDN_ENDPOINT \
  --hostname www.yourdomain.com \
  --resource-group $RESOURCE_GROUP \
  --profile-name $CDN_PROFILE \
  --name yourdomain

# Enable HTTPS
az cdn custom-domain enable-https \
  --endpoint-name $CDN_ENDPOINT \
  --resource-group $RESOURCE_GROUP \
  --profile-name $CDN_PROFILE \
  --name yourdomain
```

### 6.2 Configure CORS for API

```bash
az webapp cors add \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --allowed-origins "https://$CDN_ENDPOINT.azureedge.net"
```

### 6.3 Run Database Migrations

After first deployment, run EF Core migrations:

```bash
# Option 1: Use Azure Cloud Shell or local Azure CLI
az webapp ssh --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP

# Inside the container
cd /home/site/wwwroot
dotnet ef database update

# Option 2: Add migration script to startup
# Add to Program.cs:
# if (app.Environment.IsProduction())
# {
#     using var scope = app.Services.CreateScope();
#     var context = scope.ServiceProvider.GetRequiredService<AllowanceContext>();
#     context.Database.Migrate();
# }
```

## 7. Monitoring and Troubleshooting

### 7.1 View Application Logs

```bash
# Stream API logs
az webapp log tail \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Stream Function logs
az functionapp log tail \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP
```

### 7.2 Enable Application Insights (Recommended)

```bash
# Create Application Insights
APP_INSIGHTS_NAME="allowancetracker-insights"

az monitor app-insights component create \
  --app $APP_INSIGHTS_NAME \
  --location $LOCATION \
  --resource-group $RESOURCE_GROUP

# Get instrumentation key
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app $APP_INSIGHTS_NAME \
  --resource-group $RESOURCE_GROUP \
  --query instrumentationKey \
  --output tsv)

# Configure API to use App Insights
az webapp config appsettings set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"

# Configure Function to use App Insights
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=$INSTRUMENTATION_KEY"
```

### 7.3 Common Issues

**Issue: Deployment succeeds but app shows 500 error**
- Check connection string is correct
- Verify database migrations ran
- Check application logs for errors

**Issue: CORS errors in React app**
- Verify CORS origins are configured correctly
- Check API URL in React app matches production URL

**Issue: Function not running**
- Check Function App logs
- Verify timer trigger configuration
- Ensure connection string is set

## 8. Cost Optimization

### 8.1 Current Cost Estimate (B1 tier)

- App Service Plan (B1): ~$13/month
- Azure Function (Consumption): ~$0-5/month (depends on executions)
- Azure SQL (S0): ~$15/month
- Azure Storage: ~$1/month
- Azure CDN: ~$5-10/month (depends on traffic)

**Total: ~$34-44/month**

### 8.2 Free Tier Option

For development/testing:

```bash
# Use Free tier App Service Plan
az appservice plan create \
  --name allowancetracker-plan-free \
  --resource-group $RESOURCE_GROUP \
  --sku FREE

# Use serverless SQL database
az sql db create \
  --name $DATABASE_NAME \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --compute-model Serverless \
  --edition GeneralPurpose \
  --family Gen5 \
  --capacity 1
```

## 9. Backup and Disaster Recovery

### 9.1 Enable App Service Backup

```bash
# Create storage account for backups
BACKUP_STORAGE="allowancetrackerbackup"

az storage account create \
  --name $BACKUP_STORAGE \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

# Configure backup (requires Basic or higher tier)
az webapp config backup create \
  --resource-group $RESOURCE_GROUP \
  --webapp-name $WEB_APP_NAME \
  --backup-name initial-backup \
  --container-url "https://$BACKUP_STORAGE.blob.core.windows.net/backups?<SAS_TOKEN>"
```

### 9.2 Database Backup

Azure SQL automatically creates backups. To restore:

```bash
# List available restore points
az sql db list-restore-points \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --name $DATABASE_NAME

# Restore to a point in time
az sql db restore \
  --name $DATABASE_NAME-restored \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --source-database $DATABASE_NAME \
  --time "2024-01-15T10:30:00Z"
```

## 10. Security Best Practices

### 10.1 Rotate Secrets Regularly

- Rotate JWT secret every 90 days
- Rotate SQL admin password every 90 days
- Rotate service principal credentials annually

### 10.2 Enable Managed Identity

Replace service principal with managed identity for better security:

```bash
# Enable system-assigned managed identity for App Service
az webapp identity assign \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP

# Get the identity's principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --query principalId \
  --output tsv)

# Grant SQL access to managed identity
az sql server ad-admin create \
  --resource-group $RESOURCE_GROUP \
  --server-name $SQL_SERVER_NAME \
  --display-name $WEB_APP_NAME \
  --object-id $PRINCIPAL_ID
```

## 11. Next Steps

- [ ] Set up custom domain and SSL certificates
- [ ] Configure Application Insights dashboards
- [ ] Set up automated backups
- [ ] Configure alerts for errors and performance
- [ ] Set up staging slots for zero-downtime deployments
- [ ] Configure geo-replication for high availability

## Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Azure Storage Static Websites](https://docs.microsoft.com/azure/storage/blobs/storage-blob-static-website)
- [GitHub Actions for Azure](https://docs.microsoft.com/azure/developer/github/github-actions)
