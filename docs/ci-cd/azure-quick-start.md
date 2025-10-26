# Azure Deployment - Quick Start Guide

This is a streamlined guide to get your Allowance Tracker app deployed to Azure as quickly as possible.

## Prerequisites

- Azure subscription
- Azure CLI installed: `az --version`
- GitHub repository with admin access

## Step 1: Login to Azure

```bash
az login
az account set --subscription "<YOUR_SUBSCRIPTION_ID>"
```

## Step 2: Set Variables

```bash
# Replace these with your preferred names
export RESOURCE_GROUP="allowancetracker-rg"
export LOCATION="eastus"
export APP_SERVICE_PLAN="allowancetracker-plan"
export WEB_APP_NAME="allowancetracker-api"
export FUNCTION_APP_NAME="allowancetracker-func"
export STORAGE_ACCOUNT_FUNC="allowancetrackerfunc"
export STORAGE_ACCOUNT_WEB="allowancetrackerapp"
export SQL_SERVER_NAME="allowancetracker-sql"
export SQL_ADMIN_USER="sqladmin"
export SQL_ADMIN_PASSWORD="YourSecurePassword123!"
export DATABASE_NAME="allowancetracker"
export CDN_PROFILE="allowancetracker-cdn"
export CDN_ENDPOINT="allowancetracker"
```

## Step 3: Create Azure Resources

### Resource Group
```bash
az group create --name $RESOURCE_GROUP --location $LOCATION
```

### App Service Plan
```bash
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B1 \
  --is-linux
```

### Web App (API)
```bash
az webapp create \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNET|8.0"

az webapp config set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --always-on true

az webapp update \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --https-only true
```

### SQL Database
```bash
az sql server create \
  --name $SQL_SERVER_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user $SQL_ADMIN_USER \
  --admin-password $SQL_ADMIN_PASSWORD

az sql server firewall-rule create \
  --name AllowAzureServices \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

az sql db create \
  --name $DATABASE_NAME \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER_NAME \
  --service-objective S0 \
  --backup-storage-redundancy Local
```

### Function App
```bash
az storage account create \
  --name $STORAGE_ACCOUNT_FUNC \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS

az functionapp create \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --consumption-plan-location $LOCATION \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --storage-account $STORAGE_ACCOUNT_FUNC \
  --os-type Linux
```

### Storage Account for React App
```bash
az storage account create \
  --name $STORAGE_ACCOUNT_WEB \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2

az storage blob service-properties update \
  --account-name $STORAGE_ACCOUNT_WEB \
  --static-website \
  --index-document index.html \
  --404-document index.html
```

### CDN (Optional)
```bash
az cdn profile create \
  --name $CDN_PROFILE \
  --resource-group $RESOURCE_GROUP \
  --sku Standard_Microsoft

STORAGE_ORIGIN=$(az storage account show \
  --name $STORAGE_ACCOUNT_WEB \
  --query "primaryEndpoints.web" \
  --output tsv | sed 's|https://||' | sed 's|/||')

az cdn endpoint create \
  --name $CDN_ENDPOINT \
  --resource-group $RESOURCE_GROUP \
  --profile-name $CDN_PROFILE \
  --origin $STORAGE_ORIGIN \
  --origin-host-header $STORAGE_ORIGIN

az cdn endpoint update \
  --name $CDN_ENDPOINT \
  --resource-group $RESOURCE_GROUP \
  --profile-name $CDN_PROFILE \
  --enable-compression true
```

## Step 4: Configure Application Settings

### API App Settings
```bash
# Generate a JWT secret (save this!)
JWT_SECRET=$(openssl rand -base64 32)

az webapp config connection-string set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --connection-string-type SQLAzure \
  --settings DefaultConnection="Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Database=$DATABASE_NAME;User ID=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;Encrypt=true;Connection Timeout=30;"

az webapp config appsettings set \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ASPNETCORE_ENVIRONMENT="Production" \
    Jwt__Secret="$JWT_SECRET" \
    Jwt__Issuer="https://$WEB_APP_NAME.azurewebsites.net" \
    Jwt__Audience="https://$WEB_APP_NAME.azurewebsites.net" \
    Jwt__ExpirationInHours="24"
```

### Function App Settings
```bash
az functionapp config appsettings set \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    ConnectionStrings__DefaultConnection="Server=tcp:$SQL_SERVER_NAME.database.windows.net,1433;Database=$DATABASE_NAME;User ID=$SQL_ADMIN_USER;Password=$SQL_ADMIN_PASSWORD;Encrypt=true;Connection Timeout=30;" \
    ASPNETCORE_ENVIRONMENT="Production"
```

## Step 5: Create Service Principal for GitHub

**Option A: Using the provided script (recommended)**

```bash
cd docs/ci-cd
./create-service-principal.sh
```

The script will guide you through the process and save the credentials to a file.

**Option B: Manual command**

```bash
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

az ad sp create-for-rbac \
  --name "allowancetracker-github-actions" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

**Important**: Copy the entire JSON output! You'll need it for GitHub.

## Step 6: Configure GitHub Secrets

Go to your GitHub repository → Settings → Secrets and variables → Actions

Add these secrets (one at a time):

1. **AZURE_CREDENTIALS**
   - Paste the entire JSON from Step 5

2. **VITE_API_URL**
   ```
   https://allowancetracker-api.azurewebsites.net
   ```

3. **AZURE_WEBAPP_NAME**
   ```
   allowancetracker-api
   ```

4. **AZURE_FUNCTIONAPP_NAME**
   ```
   allowancetracker-func
   ```

5. **AZURE_STORAGE_ACCOUNT**
   ```
   allowancetrackerapp
   ```

6. **AZURE_RESOURCE_GROUP**
   ```
   allowancetracker-rg
   ```

7. **AZURE_CDN_PROFILE** (if using CDN)
   ```
   allowancetracker-cdn
   ```

8. **AZURE_CDN_ENDPOINT** (if using CDN)
   ```
   allowancetracker
   ```

## Step 7: Enable CORS for API

```bash
# Get CDN or Storage URL
CDN_URL="https://$CDN_ENDPOINT.azureedge.net"
STORAGE_URL="https://$STORAGE_ACCOUNT_WEB.z13.web.core.windows.net"

az webapp cors add \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --allowed-origins $CDN_URL $STORAGE_URL
```

## Step 8: Deploy!

Push to main branch or trigger manual deployment:

```bash
git add .
git commit -m "Configure Azure deployment"
git push origin main
```

Or manually trigger from GitHub:
1. Go to Actions tab
2. Select a workflow (e.g., "Deploy API to Azure App Service")
3. Click "Run workflow"
4. Select branch: main
5. Click "Run workflow"

## Step 9: Verify Deployment

### Check API
```bash
API_URL="https://$WEB_APP_NAME.azurewebsites.net"
curl $API_URL/health
```

### Check React App
```bash
# If using CDN
open "https://$CDN_ENDPOINT.azureedge.net"

# If not using CDN
open "https://$STORAGE_ACCOUNT_WEB.z13.web.core.windows.net"
```

### Check Function App
```bash
az functionapp list-functions \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP
```

## Troubleshooting

### View API Logs
```bash
az webapp log tail --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP
```

### View Function Logs
```bash
az functionapp log tail --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP
```

### Check GitHub Actions
Go to: https://github.com/YOUR_USERNAME/YOUR_REPO/actions

### Common Issues

**Issue: Deployment succeeds but app shows 500 error**
- Check connection string: `az webapp config connection-string list --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP`
- Check app settings: `az webapp config appsettings list --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP`
- View logs: `az webapp log tail --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP`

**Issue: CORS errors in browser**
```bash
# Add CORS origins
az webapp cors add \
  --name $WEB_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --allowed-origins "https://your-frontend-url.com"
```

**Issue: GitHub Actions fails with "Login failed"**
- Verify `AZURE_CREDENTIALS` secret is correct
- Ensure service principal has contributor access
- Try recreating service principal (Step 5)

## Cost Estimate

With the B1 tier configuration:
- App Service Plan (B1): ~$13/month
- Azure Function (Consumption): ~$0-5/month
- Azure SQL (S0): ~$15/month
- Azure Storage: ~$1/month
- Azure CDN: ~$5-10/month

**Total: ~$34-44/month**

## Next Steps

- [ ] Configure custom domain
- [ ] Enable Application Insights
- [ ] Set up automated backups
- [ ] Configure staging slots
- [ ] Set up monitoring alerts

## Useful Commands

```bash
# List all resources
az resource list --resource-group $RESOURCE_GROUP --output table

# Delete everything (be careful!)
az group delete --name $RESOURCE_GROUP --yes --no-wait

# Get connection strings
az webapp config connection-string list --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP

# Get app settings
az webapp config appsettings list --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP

# Restart API
az webapp restart --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP

# Restart Function
az functionapp restart --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP
```

## Resources

- [Azure Deployment Setup (Detailed)](./azure-deployment-setup.md)
- [GitHub Secrets Reference](./github-secrets-azure.md)
- [Azure Documentation](https://docs.microsoft.com/azure/)
