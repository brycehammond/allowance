# Azure Deployment Guide - API + React

This guide explains how to deploy the Allowance Tracker application to Azure using Azure Pipelines with a modern architecture:
- **.NET API** → Azure App Service (Linux)
- **React Frontend** → Azure Storage Static Website

## Architecture Overview

```
┌─────────────────────────────────────────────┐
│           Azure DevOps Pipeline             │
│  ┌──────────────┐      ┌─────────────────┐ │
│  │  Build API   │      │  Build React    │ │
│  │  (dotnet)    │      │  (npm + vite)   │ │
│  └──────┬───────┘      └────────┬────────┘ │
└─────────┼──────────────────────┼───────────┘
          │                      │
          ▼                      ▼
┌──────────────────┐    ┌──────────────────┐
│  App Service     │    │  Storage Account │
│  (API Backend)   │◄───┤  (Static Site)   │
│  .NET 8 Runtime  │    │  React SPA       │
└────────┬─────────┘    └──────────────────┘
         │
         ▼
┌──────────────────┐
│  Azure SQL       │
│  Database        │
└──────────────────┘
```

## Prerequisites

- Azure Subscription
- Azure DevOps account with access to your repository
- Azure CLI installed (optional, for local setup)

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

**Get connection string:**
```bash
echo "Server=tcp:allowancetracker-sql.database.windows.net,1433;Initial Catalog=AllowanceTracker;Persist Security Info=False;User ID=sqladmin;Password=<YourPassword>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
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

### 1.4 Create Storage Account (Frontend)

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

### 1.5 Configure CORS on API

```bash
# Get the frontend URL first
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
  --allowed-origins "http://localhost:5173" "http://localhost:3000"
```

### 1.6 Create CDN (Optional but Recommended)

For better performance and custom domain support:

```bash
# Create CDN Profile
az cdn profile create \
  --name allowancetracker-cdn \
  --resource-group allowancetracker-rg \
  --sku Standard_Microsoft

# Get storage account primary endpoint
ORIGIN=$(az storage account show \
  --name allowancetrackerweb \
  --resource-group allowancetracker-rg \
  --query "primaryEndpoints.web" \
  --output tsv | sed 's|https://||' | sed 's|/$||')

# Create CDN Endpoint
az cdn endpoint create \
  --name allowancetracker \
  --profile-name allowancetracker-cdn \
  --resource-group allowancetracker-rg \
  --origin "$ORIGIN" \
  --origin-host-header "$ORIGIN" \
  --enable-compression true \
  --query-string-caching-behavior BypassCaching
```

---

## 2. Azure DevOps Pipeline Setup

### 2.1 Create Service Connection

1. In Azure DevOps, go to **Project Settings** → **Service connections**
2. Click **New service connection**
3. Select **Azure Resource Manager**
4. Choose **Service principal (automatic)**
5. Select your **subscription** and **resource group** (`allowancetracker-rg`)
6. Name it: `Azure-AllowanceTracker`
7. Check "Grant access permission to all pipelines"
8. Click **Save**

### 2.2 Configure Pipeline Variables

Go to **Pipelines** → Select your pipeline → **Edit** → **Variables**

#### Required Variables

| Variable Name | Value | Secret | Description |
|--------------|-------|:------:|-------------|
| `AzureSubscription` | `Azure-AllowanceTracker` | ❌ | Name of your Azure service connection |
| `ResourceGroupName` | `allowancetracker-rg` | ❌ | Azure resource group name |
| `ApiAppServiceName` | `allowancetracker-api` | ❌ | App Service name for API |
| `StorageAccountName` | `allowancetrackerweb` | ❌ | Storage account name for React app |
| `AzureSqlConnectionString` | `Server=tcp:allowancetracker-sql...` | ✅ | Full SQL connection string from step 1.2 |
| `JwtSecretKey` | `<generate-secure-key>` | ✅ | JWT signing key (min 32 characters) |
| `ReactAppApiUrl` | `https://allowancetracker-api.azurewebsites.net` | ❌ | API URL for React frontend |

#### Optional Variables (for CDN)

| Variable Name | Value | Secret | Description |
|--------------|-------|:------:|-------------|
| `CdnProfileName` | `allowancetracker-cdn` | ❌ | CDN Profile name |
| `CdnEndpointName` | `allowancetracker` | ❌ | CDN Endpoint name |

### 2.3 Generate Secure JWT Key

Generate a cryptographically secure random key (minimum 32 characters):

**PowerShell:**
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 48 | ForEach-Object {[char]$_})
```

**Bash/Linux/macOS:**
```bash
openssl rand -base64 48
```

**Online (if needed):**
- Use a password generator with at least 48 characters

### 2.4 Create Environment

1. Go to **Pipelines** → **Environments**
2. Click **New environment**
3. Name: `production`
4. Resource: None (it's an empty environment)
5. Click **Create**
6. (Optional) Add **Approvals and checks** for production deployments

---

## 3. Pipeline Stages Explained

The `azure-pipelines.yml` defines 5 stages that run on every commit to `main`:

### Stage 1: Build .NET API
```yaml
- Installs .NET 8 SDK
- Restores NuGet packages
- Builds the solution
- Runs all unit tests (213 tests)
- Publishes code coverage
- Creates deployment artifact (ZIP file)
```

### Stage 2: Build React App
```yaml
- Installs Node.js 20
- Runs npm ci (clean install)
- Builds with Vite (production build)
- Injects VITE_API_URL environment variable
- Creates deployment artifact (dist folder)
```

### Stage 3: Deploy API to App Service
```yaml
- Deploys ZIP to Azure App Service
- Configures app settings:
  - Connection strings
  - JWT configuration
  - Environment variables
- Runs EF Core migrations
- Restarts the app
```

### Stage 4: Deploy React to Storage
```yaml
- Enables static website hosting
- Uploads files to $web container
- Sets cache headers:
  - assets/*: 1 year cache (immutable)
  - index.html: no cache
- Optionally purges CDN
```

### Stage 5: Code Quality (Parallel)
```yaml
.NET:
  - dotnet format check
  - Build with warnings as errors

React:
  - ESLint checks
  - TypeScript type checking
```

---

## 4. First Deployment

### Option A: Commit and Push (Automatic)

```bash
# Commit the updated pipeline
git add azure-pipelines.yml web/package.json
git commit -m "Configure Azure deployment pipeline"
git push origin main
```

The pipeline will automatically trigger and deploy both API and React app.

### Option B: Manual Deployment

#### Deploy API Manually

```bash
# Build and publish
cd src/AllowanceTracker
dotnet publish -c Release -o ./publish

# Create ZIP
cd publish
zip -r ../api.zip .
cd ..

# Deploy
az webapp deployment source config-zip \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --src api.zip

# Run migrations
az webapp ssh --resource-group allowancetracker-rg --name allowancetracker-api
# Then in the SSH session:
dotnet ef database update --no-build
```

#### Deploy React Manually

```bash
# Build
cd web
npm install
VITE_API_URL=https://allowancetracker-api.azurewebsites.net npm run build

# Deploy
az storage blob upload-batch \
  --account-name allowancetrackerweb \
  --auth-mode login \
  --destination '$web' \
  --source ./dist \
  --overwrite true
```

---

## 5. Verify Deployment

### Check API

```bash
# Get API URL
API_URL="https://allowancetracker-api.azurewebsites.net"

# Test health endpoint
curl $API_URL/api/health

# Check Swagger docs
open $API_URL/swagger
```

### Check Frontend

```bash
# Get frontend URL
az storage account show \
  --name allowancetrackerweb \
  --resource-group allowancetracker-rg \
  --query "primaryEndpoints.web" \
  --output tsv

# Open in browser
open "https://allowancetrackerweb.z13.web.core.windows.net"
```

### Check Logs

```bash
# Stream API logs
az webapp log tail \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api

# Download logs
az webapp log download \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --log-file logs.zip
```

---

## 6. Custom Domain Setup

### For API (App Service)

```bash
# Add custom domain
az webapp config hostname add \
  --resource-group allowancetracker-rg \
  --webapp-name allowancetracker-api \
  --hostname api.yourdomain.com

# Create managed certificate (free)
az webapp config ssl create \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --hostname api.yourdomain.com

# Bind certificate
az webapp config ssl bind \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --certificate-thumbprint auto \
  --ssl-type SNI
```

**DNS Configuration:**
```
Type: CNAME
Name: api
Value: allowancetracker-api.azurewebsites.net
```

### For Frontend (CDN)

```bash
# Add custom domain to CDN
az cdn custom-domain create \
  --resource-group allowancetracker-rg \
  --profile-name allowancetracker-cdn \
  --endpoint-name allowancetracker \
  --name app-yourdomain \
  --hostname app.yourdomain.com

# Enable HTTPS (takes 6-8 hours)
az cdn custom-domain enable-https \
  --resource-group allowancetracker-rg \
  --profile-name allowancetracker-cdn \
  --endpoint-name allowancetracker \
  --name app-yourdomain
```

**DNS Configuration:**
```
Type: CNAME
Name: app
Value: allowancetracker.azureedge.net
```

**Update Variables:**
After setting up custom domains, update these pipeline variables:
- `ReactAppApiUrl` → `https://api.yourdomain.com`

Also update CORS on API to allow your custom frontend domain.

---

## 7. Monitoring and Application Insights

### Create Application Insights

```bash
# Create Application Insights
az monitor app-insights component create \
  --app allowancetracker-insights \
  --location eastus \
  --resource-group allowancetracker-rg \
  --application-type web

# Get connection string
INSTRUMENTATION_KEY=$(az monitor app-insights component show \
  --app allowancetracker-insights \
  --resource-group allowancetracker-rg \
  --query connectionString \
  --output tsv)

# Configure App Service
az webapp config appsettings set \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --settings "APPLICATIONINSIGHTS_CONNECTION_STRING=$INSTRUMENTATION_KEY"
```

### View Metrics

- Go to Azure Portal → Application Insights → `allowancetracker-insights`
- View:
  - **Live Metrics** - Real-time requests
  - **Failures** - Exception tracking
  - **Performance** - Response times
  - **Application Map** - Dependency visualization

---

## 8. Scaling and Performance

### Auto-scaling

```bash
# Enable auto-scale
az monitor autoscale create \
  --resource-group allowancetracker-rg \
  --resource allowancetracker-api \
  --resource-type Microsoft.Web/sites \
  --name autoscale-api \
  --min-count 1 \
  --max-count 5 \
  --count 1

# Add CPU rule
az monitor autoscale rule create \
  --resource-group allowancetracker-rg \
  --autoscale-name autoscale-api \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 1

az monitor autoscale rule create \
  --resource-group allowancetracker-rg \
  --autoscale-name autoscale-api \
  --condition "Percentage CPU < 30 avg 5m" \
  --scale in 1
```

### CDN Cache Rules

Configure cache TTL in Azure Portal:
- Static assets (JS/CSS): 1 year
- HTML files: No cache
- API responses: Not cached

---

## 9. Security Best Practices

### ✅ Implemented in Pipeline

- [x] HTTPS enforced on App Service
- [x] Secrets stored in pipeline variables (encrypted)
- [x] SQL firewall configured
- [x] CORS properly configured
- [x] Environment-specific builds

### 🔒 Additional Recommendations

1. **Enable Managed Identity** for database access:
   ```bash
   az webapp identity assign \
     --resource-group allowancetracker-rg \
     --name allowancetracker-api
   ```

2. **Use Azure Key Vault** for secrets:
   ```bash
   az keyvault create \
     --name allowancetracker-kv \
     --resource-group allowancetracker-rg \
     --location eastus
   ```

3. **Enable DDoS Protection** on critical resources

4. **Configure Azure Front Door** for advanced WAF and routing

5. **Enable database auditing and threat detection**

---

## 10. Cost Optimization

### Monthly Cost Estimates

**Development Environment:**
- App Service B1: ~$13/month
- SQL Database Basic: ~$5/month
- Storage Account: ~$0.05/month
- **Total: ~$18/month**

**Production Environment:**
- App Service S1: ~$74/month (includes auto-scaling)
- SQL Database S2: ~$60/month (includes backups)
- Storage Account + CDN: ~$10/month
- Application Insights: ~$5/month (basic)
- **Total: ~$149/month**

### Cost Saving Tips

1. **Use reserved instances** for 1-3 year commitments (30-50% savings)
2. **Stop dev resources** when not in use
3. **Use Azure Cost Management** alerts
4. **Right-size your SQL tier** based on actual usage
5. **Enable auto-shutdown** for dev App Services

---

## 11. Troubleshooting

### API Not Starting

**Check logs:**
```bash
az webapp log tail \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api
```

**Common issues:**
- Missing connection string
- EF migrations not applied
- Incorrect JWT configuration
- Port binding issues

**Solution:**
```bash
# Restart the app
az webapp restart \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api

# Check app settings
az webapp config appsettings list \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api
```

### Frontend Not Loading

**Check static website:**
```bash
# Verify static website is enabled
az storage blob service-properties show \
  --account-name allowancetrackerweb \
  --query "staticWebsite"
```

**Common issues:**
- CORS not configured on API
- Wrong API URL in build
- Files not uploaded to `$web` container

**Solution:**
```bash
# List files in $web container
az storage blob list \
  --account-name allowancetrackerweb \
  --container-name '$web' \
  --output table
```

### CORS Errors

```bash
# Check current CORS settings
az webapp cors show \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api

# Fix CORS
az webapp cors remove \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --allowed-origins '*'

az webapp cors add \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --allowed-origins "https://allowancetrackerweb.z13.web.core.windows.net"
```

### Database Connection Issues

```bash
# Test connection from App Service
az webapp ssh \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api

# Inside SSH session:
sqlcmd -S allowancetracker-sql.database.windows.net -U sqladmin -P <password>
```

---

## 12. Backup and Disaster Recovery

### Database Backups

SQL Database automatically creates:
- Full backups weekly
- Differential backups every 12 hours
- Transaction log backups every 5-10 minutes

**Manual backup:**
```bash
az sql db export \
  --resource-group allowancetracker-rg \
  --server allowancetracker-sql \
  --name AllowanceTracker \
  --admin-user sqladmin \
  --admin-password <password> \
  --storage-key-type StorageAccessKey \
  --storage-key <storage-key> \
  --storage-uri "https://backupstorage.blob.core.windows.net/backups/db.bacpac"
```

### App Service Backups

```bash
az webapp config backup create \
  --resource-group allowancetracker-rg \
  --webapp-name allowancetracker-api \
  --container-url "<storage-container-sas-url>" \
  --backup-name "backup-$(date +%Y%m%d)"
```

---

## 13. CI/CD Best Practices

### Branch Strategy

```
main (production)
  ↑
develop (staging)
  ↑
feature/* (dev)
```

### Multi-Environment Setup

Create separate pipelines for each environment:

**azure-pipelines-dev.yml:**
```yaml
trigger:
  branches:
    include:
    - develop
```

**Variables:**
- Dev: `allowancetracker-api-dev`
- Staging: `allowancetracker-api-staging`
- Production: `allowancetracker-api`

### Deployment Slots

Use App Service deployment slots for zero-downtime deployments:

```bash
# Create staging slot
az webapp deployment slot create \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --slot staging

# Swap slots after validation
az webapp deployment slot swap \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --slot staging \
  --target-slot production
```

---

## 14. Additional Resources

- [Azure App Service Docs](https://docs.microsoft.com/en-us/azure/app-service/)
- [Azure Storage Static Websites](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-static-website)
- [Azure Pipelines YAML Schema](https://docs.microsoft.com/en-us/azure/devops/pipelines/yaml-schema)
- [.NET on Azure](https://docs.microsoft.com/en-us/dotnet/azure/)
- [Vite Environment Variables](https://vitejs.dev/guide/env-and-mode.html)

---

## Quick Reference Commands

```bash
# View all resources
az resource list --resource-group allowancetracker-rg --output table

# Get API URL
az webapp show --name allowancetracker-api --resource-group allowancetracker-rg --query defaultHostName -o tsv

# Get Frontend URL
az storage account show --name allowancetrackerweb --resource-group allowancetracker-rg --query primaryEndpoints.web -o tsv

# Restart API
az webapp restart --name allowancetracker-api --resource-group allowancetracker-rg

# View costs
az consumption usage list --start-date 2025-01-01 --end-date 2025-01-31

# Delete everything
az group delete --name allowancetracker-rg --yes --no-wait
```
