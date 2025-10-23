# Azure DevOps Pipeline Setup Guide

This guide walks through setting up the complete CI/CD pipeline for the Allowance Tracker application using Azure DevOps for deployment and GitHub Actions for validation.

## Architecture Overview

### Two-Pipeline Strategy

**GitHub Actions (CI/Validation)**
- ✅ Build verification on every push/PR
- ✅ Automated testing (unit, integration)
- ✅ Code quality checks (formatting, linting, warnings)
- ✅ Security scanning (vulnerable packages, npm audit)
- ✅ Fast feedback for developers
- 🚫 No deployment

**Azure DevOps Pipelines (CD/Deployment)**
- ✅ Build production artifacts
- ✅ Deploy to Azure App Service (API)
- ✅ Deploy to Azure Functions (Background Jobs)
- ✅ Deploy to Azure Storage (React Static Website)
- ✅ Run database migrations
- ✅ Health checks and verification
- 🎯 Only runs on main branch

## Prerequisites

### 1. Azure Resources

Create the following Azure resources (or use existing ones):

```bash
# Login to Azure
az login

# Set variables
RESOURCE_GROUP="allowancetracker-rg"
LOCATION="eastus"
APP_SERVICE_PLAN="allowancetracker-plan"
API_APP_NAME="allowancetracker-api"
FUNCTION_APP_NAME="allowancetracker-func"
STORAGE_ACCOUNT="allowancetrackerweb"
SQL_SERVER="allowancetracker-sql"
SQL_DATABASE="allowancetracker-db"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create App Service Plan (Linux, B1 tier)
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --is-linux \
  --sku B1

# Create Web App for API
az webapp create \
  --name $API_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --runtime "DOTNET|8.0"

# Create Function App
az functionapp create \
  --name $FUNCTION_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --storage-account $STORAGE_ACCOUNT \
  --runtime dotnet-isolated \
  --runtime-version 8 \
  --functions-version 4 \
  --os-type Linux

# Create Storage Account for React App
az storage account create \
  --name $STORAGE_ACCOUNT \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --sku Standard_LRS \
  --kind StorageV2

# Enable static website hosting
az storage blob service-properties update \
  --account-name $STORAGE_ACCOUNT \
  --static-website \
  --404-document index.html \
  --index-document index.html

# Create SQL Server and Database
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --admin-user sqladmin \
  --admin-password "YourSecurePassword123!"

az sql db create \
  --name $SQL_DATABASE \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --service-objective S0

# Get SQL connection string
az sql db show-connection-string \
  --name $SQL_DATABASE \
  --server $SQL_SERVER \
  --client ado.net
```

### 2. Azure DevOps Setup

1. **Create Azure DevOps Organization**
   - Go to https://dev.azure.com
   - Create or select an organization
   - Create a new project: "AllowanceTracker"

2. **Connect to GitHub**
   - In Azure DevOps, go to Project Settings → Service Connections
   - Create new Service Connection → GitHub
   - Authorize Azure Pipelines to access your GitHub repository

3. **Create Azure Service Connection**
   - Project Settings → Service Connections
   - Create new Service Connection → Azure Resource Manager
   - Choose "Service Principal (automatic)"
   - Select your subscription and resource group
   - Name it: `AzureSubscription`
   - This will be used for deployments

## Pipeline Configuration

### Required Pipeline Variables

In Azure DevOps, go to **Pipelines → Library** and create a Variable Group named `AllowanceTracker-Production`:

| Variable Name | Description | Example Value | Secret? |
|--------------|-------------|---------------|---------|
| `ResourceGroupName` | Azure resource group | `allowancetracker-rg` | No |
| `ApiAppServiceName` | API App Service name | `allowancetracker-api` | No |
| `FunctionAppName` | Function App name | `allowancetracker-func` | No |
| `StorageAccountName` | Storage account name | `allowancetrackerweb` | No |
| `AzureSqlConnectionString` | SQL connection string | `Server=tcp:...` | **Yes** |
| `JwtSecretKey` | JWT signing key (generate secure random string) | `your-secret-key-here` | **Yes** |
| `AzureWebJobsStorage` | Storage connection for Function App | `DefaultEndpointsProtocol=https;AccountName=...` | **Yes** |
| `ApplicationInsightsConnectionString` | App Insights connection (optional) | `InstrumentationKey=...` | No |
| `ReactAppApiUrl` | API URL for React build | `https://allowancetracker-api.azurewebsites.net` | No |
| `CdnProfileName` | CDN profile name (optional) | `allowancetracker-cdn` | No |
| `CdnEndpointName` | CDN endpoint name (optional) | `allowancetracker-web` | No |

**Note:** `AzureSubscription` is **not** a variable - it's a **Service Connection** created in Project Settings (see step 3 in Azure DevOps Setup above).

**To mark variables as secret:**
1. Click the lock icon next to the variable value
2. This prevents the value from being displayed in logs

**To get connection strings:**
```bash
# SQL Connection String
az sql db show-connection-string --client ado.net --server <server-name> --name <database-name>

# Storage Connection String (for AzureWebJobsStorage)
az storage account show-connection-string --name <storage-account-name> --resource-group <resource-group>
```

### Pipeline Setup

1. **Import the Pipeline**
   ```bash
   # In Azure DevOps
   Pipelines → New Pipeline → GitHub → Select Repository → Existing Azure Pipelines YAML file
   # Select: azure-pipelines.yml
   ```

2. **Link Variable Group**
   - Edit the pipeline
   - Click "Variables" → "Variable groups"
   - Link the `AllowanceTracker-Production` variable group

3. **Configure Environments**
   - Go to Pipelines → Environments
   - Create environment: `production`
   - Add approvals (optional):
     - Click the 3 dots → Approvals and checks
     - Add approval → Select reviewers

## Pipeline Stages

The Azure Pipeline consists of 6 stages that run in parallel where possible, using **native Azure DevOps tasks** instead of Azure CLI commands for better integration and error handling.

### Stage 1: Build API
**Tasks Used:** `UseDotNet@2`, `DotNetCoreCLI@2`, `PublishBuildArtifacts@1`

- Restore NuGet packages
- Build .NET solution
- Run all tests with code coverage
- Publish API artifacts

### Stage 2: Build Function
**Tasks Used:** `UseDotNet@2`, `DotNetCoreCLI@2`, `PublishBuildArtifacts@1`

- Build Azure Function project
- Publish Function artifacts

### Stage 3: Build React
**Tasks Used:** `NodeTool@0`, `PublishBuildArtifacts@1`

- Install npm dependencies
- Build React app with Vite
- Publish static files

### Stage 4: Deploy API
**Triggers:** Only on `main` branch, after successful build

**Tasks Used:** `AzureWebApp@1`, `AzureAppServiceSettings@1`, `AzureCLI@2` (for EF migrations only), `PowerShell@2`

1. **Deploy to Azure App Service** - Uses `AzureWebApp@1` task
2. **Configure app settings** - Uses `AzureAppServiceSettings@1` with JSON configuration
3. **Run EF Core migrations** - Uses `AzureCLI@2` (no native task available for EF migrations)
4. **Health check verification** - Uses `PowerShell@2` with `Invoke-WebRequest`

### Stage 5: Deploy Function
**Triggers:** Only on `main` branch, after successful build

**Tasks Used:** `AzureFunctionApp@2`, `AzureAppServiceSettings@1`

1. **Deploy to Azure Function App** - Uses `AzureFunctionApp@2` with zip deployment
2. **Configure function app settings** - Uses `AzureAppServiceSettings@1` (not Azure CLI!)

### Stage 6: Deploy React
**Triggers:** Only on `main` branch, after successful build

**Tasks Used:** `AzurePowerShell@5`, `AzureFileCopy@4`

1. **Enable static website hosting** - Uses `AzurePowerShell@5` with `Enable-AzStorageStaticWebsite`
2. **Upload static assets** - Uses `AzureFileCopy@4` with cache headers for assets
3. **Upload HTML/JS files** - Uses `AzureFileCopy@4` with no-cache headers
4. **Purge CDN (if configured)** - Uses `AzurePowerShell@5` with `Clear-AzCdnEndpointContent`

### Stage 7: Code Quality (Parallel)
**Tasks Used:** `UseDotNet@2`, `DotNetCoreCLI@2`, `NodeTool@0`, `script`

Runs in parallel with builds:
- .NET code formatting check (`dotnet format`)
- Build with warnings as errors
- React ESLint
- TypeScript type checking

## Why Native Azure DevOps Tasks?

The pipeline uses **native Azure DevOps tasks** instead of Azure CLI commands wherever possible:

✅ **Better error handling** - Tasks have built-in retry logic and error reporting
✅ **Declarative configuration** - Easier to read and maintain
✅ **Built-in authentication** - No need to manage credentials manually
✅ **Better logging** - Structured logs with task-specific formatting
✅ **Pipeline visualization** - Tasks show up clearly in the UI
✅ **Type safety** - Input validation at pipeline level

**Azure CLI only used when necessary:**
- EF Core migrations (no native task exists)
- Custom operations not covered by existing tasks

## GitHub Actions (CI Only)

GitHub Actions now focuses exclusively on fast feedback for developers:

### API CI (`.github/workflows/api.yml`)
- ✅ Build and test on every push/PR
- ✅ Code quality checks
- ✅ Security scanning
- 🚫 No deployment

### Web CI (`.github/workflows/web.yml`)
- ✅ Build React app
- ✅ Lint and type check
- ✅ Lighthouse CI (PR only)
- ✅ Security scanning (npm audit)
- 🚫 No deployment

### Function CI (`.github/workflows/function.yml`)
- ✅ Build and test
- ✅ Code quality checks
- 🚫 No deployment

### iOS CI (`.github/workflows/ios.yml`)
- ✅ Build and test iOS app
- ✅ SwiftLint code quality
- 🚫 No deployment (TestFlight/App Store handled separately)

## Deployment Flow

### 1. Developer Workflow

```
Developer pushes to branch
         ↓
GitHub Actions runs (CI)
  - Build
  - Test
  - Code Quality
  - Security Scan
         ↓
Create Pull Request
         ↓
GitHub Actions runs on PR
  - All checks must pass
  - Code review
         ↓
Merge to main
         ↓
Azure DevOps Pipeline triggered
  - Build all projects
  - Run tests
  - Deploy to production
  - Run migrations
  - Health checks
```

### 2. Manual Deployment

To trigger a manual deployment:

1. Go to Azure DevOps → Pipelines
2. Select the pipeline
3. Click "Run pipeline"
4. Select branch: `main`
5. Click "Run"

### 3. Rollback Strategy

If deployment fails or issues are detected:

**Option 1: Revert in Git**
```bash
# Revert the problematic commit
git revert <commit-hash>
git push origin main

# Azure Pipeline will automatically deploy the reverted code
```

**Option 2: Re-run Previous Deployment**
1. Go to Azure DevOps → Pipelines → Runs
2. Find the last successful run
3. Click "..." → "Retain run"
4. Click "Rerun failed jobs" or "Rerun pipeline"

**Option 3: Azure Portal**
1. Go to Azure Portal → App Service
2. Deployment Center → Deployment History
3. Select previous deployment → Redeploy

## Monitoring and Verification

### Post-Deployment Health Checks

The pipeline automatically runs health checks after deployment:

```bash
# API Health Check (includes database connectivity)
curl https://allowancetracker-api.azurewebsites.net/health

# Expected Response: 200 OK with JSON
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": null,
      "duration": 45.2
    }
  ],
  "totalDuration": 45.2
}

# Readiness Check (simple, no database)
curl https://allowancetracker-api.azurewebsites.net/health/ready

# Expected Response: 200 OK
```

For detailed information about health checks, see [Health Checks Documentation](health-checks.md).

### Manual Verification

```bash
# Check API version
curl https://allowancetracker-api.azurewebsites.net/api/health

# Check React app
curl https://allowancetrackerweb.z13.web.core.windows.net/

# Check Function App (via Azure Portal)
# Go to Function App → Functions → Monitor
```

### Application Insights (Recommended)

Enable Application Insights for monitoring:

1. Create Application Insights resource
2. Copy connection string
3. Add to pipeline variables: `ApplicationInsightsConnectionString`
4. View telemetry in Azure Portal

## Troubleshooting

### Common Issues

#### 1. Pipeline fails at "Deploy API" stage

**Error:** `No package found with specified pattern`

**Solution:**
- Verify the API build stage completed successfully
- Check artifact paths in pipeline YAML
- Ensure the artifact was published

#### 2. Database migration fails

**Error:** `Unable to connect to database`

**Solution:**
- Verify SQL connection string in pipeline variables
- Check Azure SQL firewall rules (allow Azure services)
- Verify SQL credentials

#### 3. React app shows 404

**Solution:**
- Verify static website hosting is enabled on storage account
- Check blob upload step in pipeline logs
- Verify files are in `$web` container
- Check CDN configuration (if using)

#### 4. Health check fails

**Error:** `Health check returned HTTP 503`

**Solution:**
- App may still be warming up (wait 1-2 minutes)
- Check App Service logs in Azure Portal
- Verify app settings are configured correctly
- Check if migrations ran successfully

### Viewing Logs

**Azure DevOps Pipeline Logs:**
- Pipelines → Select run → View detailed logs for each stage

**App Service Logs:**
```bash
# Stream logs
az webapp log tail --name allowancetracker-api --resource-group allowancetracker-rg

# Download logs
az webapp log download --name allowancetracker-api --resource-group allowancetracker-rg
```

**Function App Logs:**
- Azure Portal → Function App → Monitor → Log stream

## Security Best Practices

1. **Never commit secrets** - Always use pipeline variables
2. **Use managed identities** - Configure Azure resources to use managed identities where possible
3. **Rotate secrets regularly** - Update JWT keys, SQL passwords periodically
4. **Enable HTTPS only** - Enforce HTTPS on all Azure resources
5. **Review approvals** - Add approval gates for production deployments
6. **Limit access** - Use Azure RBAC to control who can deploy

## Performance Optimization

### 1. Enable Build Caching

The pipeline uses NuGet and npm package caching by default.

### 2. Parallel Builds

Build stages run in parallel to reduce total pipeline time:
- API, Function, and React builds run simultaneously
- Code Quality runs in parallel with builds

### 3. Conditional Deployment

Deployments only run on `main` branch to save build minutes.

## Cost Optimization

### Azure Resources Monthly Estimates
- **App Service (B1):** ~$13/month
- **Function App (Consumption):** ~$0-5/month
- **Storage Account:** ~$0.50/month
- **SQL Database (S0):** ~$15/month
- **Total:** ~$30-35/month

### Azure DevOps
- **Free tier:** 1,800 pipeline minutes/month (sufficient for most projects)
- **Parallel jobs:** 1 free parallel job

## Next Steps

1. ✅ Complete this setup guide
2. ✅ Run first deployment
3. ✅ Verify all services are running
4. 🔄 Configure Application Insights
5. 🔄 Set up CDN (optional, for better performance)
6. 🔄 Configure custom domains
7. 🔄 Set up deployment approvals for production

## Support

If you encounter issues:
1. Check the troubleshooting section above
2. Review Azure DevOps pipeline logs
3. Check Azure Portal for resource status
4. Review GitHub Actions for CI failures

## References

- [Azure Pipelines Documentation](https://docs.microsoft.com/azure/devops/pipelines/)
- [Azure App Service Deployment](https://docs.microsoft.com/azure/app-service/)
- [Azure Functions Deployment](https://docs.microsoft.com/azure/azure-functions/)
- [GitHub Actions Documentation](https://docs.github.com/actions)
