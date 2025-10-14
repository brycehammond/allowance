# GitHub Actions Deployment Guide

This guide explains how to deploy the Allowance Tracker application to Azure using **GitHub Actions** with a modern architecture:
- **.NET API** → Azure App Service (Linux)
- **Azure Function** → Azure Function App (Weekly Allowance Processing)
- **React Frontend** → Azure Storage Static Website

## Architecture Overview

```
┌─────────────────────────────────────────────┐
│           GitHub Actions Workflow           │
│  ┌──────────────┐ ┌──────────────┐ ┌──────┐│
│  │  Build API   │ │Build Function│ │ React││
│  │  (dotnet)    │ │  (dotnet)    │ │(vite)││
│  └──────┬───────┘ └──────┬───────┘ └───┬──┘│
└─────────┼─────────────────┼──────────────┼──┘
          │                 │              │
          ▼                 ▼              ▼
┌──────────────┐  ┌─────────────────┐  ┌─────────┐
│ App Service  │  │  Function App   │  │ Storage │
│ (API)        │  │ (Allowances)    │  │ (React) │
└──────┬───────┘  └────────┬────────┘  └─────────┘
       │                   │
       └───────┬───────────┘
               ▼
      ┌────────────────┐
      │  Azure SQL     │
      └────────────────┘
```

## Prerequisites

- Azure Subscription
- GitHub repository with Actions enabled
- Azure CLI installed (for initial setup)

---

## 1. Azure Resources Setup

### 1.1 Create Resource Group

```bash
az group create \
  --name allowancetracker-rg \
  --location eastus
```

### 1.2 Create Azure SQL Database

```bash
# Create SQL Server
az sql server create \
  --name allowancetracker-sql \
  --resource-group allowancetracker-rg \
  --location eastus \
  --admin-user sqladmin \
  --admin-password <YourStrongPassword123!>

# Create Database
az sql db create \
  --resource-group allowancetracker-rg \
  --server allowancetracker-sql \
  --name AllowanceTracker \
  --service-objective S0 \
  --backup-storage-redundancy Local

# Configure firewall to allow Azure services
az sql server firewall-rule create \
  --resource-group allowancetracker-rg \
  --server allowancetracker-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

**Connection string:**
```
Server=tcp:allowancetracker-sql.database.windows.net,1433;Initial Catalog=AllowanceTracker;Persist Security Info=False;User ID=sqladmin;Password=<YourPassword>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### 1.3 Create App Service (API)

```bash
# Create App Service Plan (Linux)
az appservice plan create \
  --name allowancetracker-plan \
  --resource-group allowancetracker-rg \
  --location eastus \
  --is-linux \
  --sku B1

# Create Web App for API
az webapp create \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --plan allowancetracker-plan \
  --runtime "DOTNETCORE:8.0"

# Enable logging
az webapp log config \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --application-logging filesystem \
  --detailed-error-messages true \
  --failed-request-tracing true \
  --web-server-logging filesystem
```

### 1.4 Create Azure Function App

**Important:** The Function App runs the weekly allowance processing job.

```bash
# Create Storage Account for Function App (required)
az storage account create \
  --name allowancefuncstorage \
  --resource-group allowancetracker-rg \
  --location eastus \
  --sku Standard_LRS

# Create Function App (Linux, .NET 8 Isolated)
az functionapp create \
  --name allowancetracker-function \
  --resource-group allowancetracker-rg \
  --consumption-plan-location eastus \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --storage-account allowancefuncstorage \
  --os-type Linux

# Configure Function App settings
az functionapp config appsettings set \
  --name allowancetracker-function \
  --resource-group allowancetracker-rg \
  --settings \
    "FUNCTIONS_WORKER_RUNTIME=dotnet-isolated"
```

### 1.5 Create Application Insights (Monitoring)

```bash
# Create Application Insights
az monitor app-insights component create \
  --app allowancetracker-insights \
  --location eastus \
  --resource-group allowancetracker-rg \
  --application-type web

# Get connection string
INSIGHTS_CONNECTION=$(az monitor app-insights component show \
  --app allowancetracker-insights \
  --resource-group allowancetracker-rg \
  --query connectionString \
  --output tsv)

# Configure both API and Function with Application Insights
az webapp config appsettings set \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=$INSIGHTS_CONNECTION"

az functionapp config appsettings set \
  --name allowancetracker-function \
  --resource-group allowancetracker-rg \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=$INSIGHTS_CONNECTION"
```

### 1.6 Create Storage Account (Frontend)

```bash
# Create Storage Account
az storage account create \
  --name allowancetrackerweb \
  --resource-group allowancetracker-rg \
  --location eastus \
  --sku Standard_LRS \
  --kind StorageV2 \
  --allow-blob-public-access true

# Enable static website hosting
az storage blob service-properties update \
  --account-name allowancetrackerweb \
  --static-website \
  --index-document index.html \
  --404-document index.html
```

**Get static website URL:**
```bash
az storage account show \
  --name allowancetrackerweb \
  --resource-group allowancetracker-rg \
  --query "primaryEndpoints.web" \
  --output tsv
```

### 1.7 Configure CORS on API

```bash
# Get the frontend URL
FRONTEND_URL=$(az storage account show \
  --name allowancetrackerweb \
  --resource-group allowancetracker-rg \
  --query "primaryEndpoints.web" \
  --output tsv | sed 's:/*$::')

# Configure CORS
az webapp cors add \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --allowed-origins "${FRONTEND_URL}"

# Also allow localhost for development
az webapp cors add \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --allowed-origins "http://localhost:5173"
```

---

## 2. GitHub Actions Setup

### 2.1 Create Azure Service Principal

Create a service principal for GitHub Actions to authenticate with Azure:

```bash
# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

# Create service principal
az ad sp create-for-rbac \
  --name "github-actions-allowancetracker" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/allowancetracker-rg \
  --sdk-auth
```

This will output JSON like:
```json
{
  "clientId": "<guid>",
  "clientSecret": "<secret>",
  "subscriptionId": "<guid>",
  "tenantId": "<guid>",
  ...
}
```

**⚠️ Save this entire JSON output** - you'll need it for GitHub Secrets!

### 2.2 Configure GitHub Secrets

Go to your GitHub repository → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**

Add these secrets:

| Secret Name | Value | Description |
|------------|-------|-------------|
| `AZURE_CREDENTIALS` | *(Entire JSON from step 2.1)* | Service principal credentials |
| `API_APP_SERVICE_NAME` | `allowancetracker-api` | App Service name for API |
| `FUNCTION_APP_NAME` | `allowancetracker-function` | Function App name |
| `RESOURCE_GROUP_NAME` | `allowancetracker-rg` | Azure resource group |
| `STORAGE_ACCOUNT_NAME` | `allowancetrackerweb` | Storage account for React |
| `AZURE_SQL_CONNECTION_STRING` | `Server=tcp:allowancetracker-sql...` | Full SQL connection string |
| `JWT_SECRET_KEY` | *(Generate secure 48-char key)* | JWT signing key |
| `REACT_APP_API_URL` | `https://allowancetracker-api.azurewebsites.net` | API URL for React |
| `APPLICATION_INSIGHTS_CONNECTION_STRING` | *(From step 1.5)* | Application Insights connection |

**Optional (for CDN):**
| Secret Name | Value |
|------------|-------|
| `CDN_PROFILE_NAME` | `allowancetracker-cdn` |
| `CDN_ENDPOINT_NAME` | `allowancetracker` |

### 2.3 Generate Secure JWT Key

**Bash/Linux/macOS:**
```bash
openssl rand -base64 48
```

**PowerShell:**
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 48 | ForEach-Object {[char]$_})
```

---

## 3. GitHub Actions Workflow Explained

The workflow (`.github/workflows/deploy.yml`) contains 7 jobs:

### Job 1: `build-api`
- Builds and tests .NET API
- Runs all 213 unit tests
- Creates deployment artifact

### Job 2: `build-function`
- Builds Azure Function project
- Creates Function deployment artifact

### Job 3: `build-react`
- Builds React app with Vite
- Injects API URL from secrets
- Creates static website artifact

### Job 4: `code-quality`
- Runs .NET code formatting checks
- Runs ESLint and TypeScript checks
- Runs in parallel with builds

### Job 5: `deploy-api`
- Deploys API to App Service
- Configures app settings
- Runs EF Core migrations
- Only runs on `main` branch

### Job 6: `deploy-function`
- Deploys Function to Azure
- Configures connection strings
- Only runs on `main` branch

### Job 7: `deploy-react`
- Deploys React to Storage
- Configures cache headers
- Optionally purges CDN
- Only runs on `main` branch

---

## 4. First Deployment

### Commit and Push

```bash
# Ensure workflow file exists
ls .github/workflows/deploy.yml

# Commit and push to trigger deployment
git add .github/workflows/deploy.yml
git commit -m "Add GitHub Actions deployment workflow"
git push origin main
```

### Monitor Deployment

1. Go to your GitHub repository
2. Click **Actions** tab
3. Watch the workflow run in real-time
4. Each job shows detailed logs

### View Deployed Resources

```bash
# API URL
echo "https://allowancetracker-api.azurewebsites.net"

# Function App URL
echo "https://allowancetracker-function.azurewebsites.net"

# Frontend URL
az storage account show \
  --name allowancetrackerweb \
  --resource-group allowancetracker-rg \
  --query "primaryEndpoints.web" \
  --output tsv
```

---

## 5. Verify Deployment

### Check API

```bash
# Test health endpoint
curl https://allowancetracker-api.azurewebsites.net/api/health

# Open Swagger docs
open https://allowancetracker-api.azurewebsites.net/swagger
```

### Check Function

```bash
# View Function in Azure Portal
az functionapp show \
  --name allowancetracker-function \
  --resource-group allowancetracker-rg

# Check logs
az functionapp log tail \
  --name allowancetracker-function \
  --resource-group allowancetracker-rg
```

### Check Frontend

```bash
# Get URL and open
FRONTEND_URL=$(az storage account show \
  --name allowancetrackerweb \
  --resource-group allowancetracker-rg \
  --query "primaryEndpoints.web" \
  --output tsv)

open "$FRONTEND_URL"
```

---

## 6. Testing the Weekly Allowance Function

### Via Azure Portal

1. Go to Azure Portal → Function App
2. Navigate to **Functions** → `ProcessWeeklyAllowances`
3. Click **Code + Test**
4. Click **Test/Run** → **Run**
5. Check execution logs

### Via HTTP Trigger (Manual Test)

```bash
# Get Function key
FUNCTION_KEY=$(az functionapp keys list \
  --name allowancetracker-function \
  --resource-group allowancetracker-rg \
  --query "functionKeys.default" \
  --output tsv)

# Manually trigger allowance processing
curl -X POST \
  "https://allowancetracker-function.azurewebsites.net/api/ProcessWeeklyAllowancesManual" \
  -H "x-functions-key: $FUNCTION_KEY"
```

### Check Function Logs

```bash
# Stream logs
az functionapp log tail \
  --name allowancetracker-function \
  --resource-group allowancetracker-rg
```

---

## 7. Cost Optimization

### Monthly Cost Estimates

**With Azure Function (Recommended):**
- App Service B1 (API): ~$13/month
- Function App (Consumption): **~$0/month** (free tier)
- SQL Database S0: ~$15/month
- Storage (React + Function): ~$1/month
- Application Insights: ~$2/month
- **Total: ~$31/month** 💰

**Savings vs Background Service:**
- No need for "Always On" setting
- Function runs on consumption plan (nearly free)
- API can scale independently

---

## 8. Monitoring

### Application Insights Dashboard

View in Azure Portal → Application Insights → `allowancetracker-insights`:

**API Metrics:**
- Request rates and response times
- Failed requests and exceptions
- Dependency calls (SQL queries)

**Function Metrics:**
- Function executions (should show daily)
- Success/failure rate
- Execution duration
- Allowances processed

### Set Up Alerts

```bash
# Alert if Function fails
az monitor metrics alert create \
  --name "function-failures" \
  --resource-group allowancetracker-rg \
  --scopes $(az functionapp show --name allowancetracker-function --resource-group allowancetracker-rg --query id -o tsv) \
  --condition "count FunctionExecutionCount where resultCode == '500' > 0" \
  --window-size 5m \
  --evaluation-frequency 1m \
  --action email@example.com
```

---

## 9. Troubleshooting

### Function Not Running

**Check schedule:**
```bash
# View Function configuration
az functionapp config show \
  --name allowancetracker-function \
  --resource-group allowancetracker-rg
```

**Check logs:**
```bash
az functionapp log tail \
  --name allowancetracker-function \
  --resource-group allowancetracker-rg
```

**Common issues:**
- Function App stopped
- Connection string missing
- Application Insights not configured

### API Issues

Same as before - check logs:
```bash
az webapp log tail \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg
```

---

## 10. GitHub Actions Best Practices

### Branch Protection

Protect `main` branch:
1. Go to **Settings** → **Branches**
2. Add rule for `main`
3. Enable:
   - Require pull request reviews
   - Require status checks to pass
   - Include administrators

### Environment Protection

Add approval gates for production:
1. Go to **Settings** → **Environments**
2. Create `production` environment
3. Add required reviewers
4. Set deployment branches to `main` only

### Secrets Rotation

Rotate secrets every 90 days:
```bash
# Create new service principal
az ad sp create-for-rbac --name "github-actions-allowancetracker-2" ...

# Update GitHub secret
# Delete old service principal
```

---

## Quick Reference

```bash
# View all resources
az resource list --resource-group allowancetracker-rg --output table

# Restart API
az webapp restart --name allowancetracker-api --resource-group allowancetracker-rg

# Restart Function
az functionapp restart --name allowancetracker-function --resource-group allowancetracker-rg

# View costs
az consumption usage list --start-date 2025-10-01 --end-date 2025-10-31

# Delete everything
az group delete --name allowancetracker-rg --yes --no-wait
```

---

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Functions Documentation](https://docs.microsoft.com/azure/azure-functions/)
- [Azure App Service on Linux](https://docs.microsoft.com/azure/app-service/overview)
- [Azure Storage Static Websites](https://docs.microsoft.com/azure/storage/blobs/storage-blob-static-website)
