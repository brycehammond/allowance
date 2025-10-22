# Azure DevOps Pipeline - Quick Start Guide

**Goal:** Get your pipeline running in 15 minutes

## Prerequisites

- [ ] Azure subscription with existing resources (App Service, Function App, Storage, SQL Database)
- [ ] Azure DevOps account
- [ ] GitHub repository connected to Azure DevOps

---

## 5-Step Setup

### Step 1: Create Service Connection (2 min)

1. Azure DevOps → **Project Settings** → **Service connections**
2. **New service connection** → **Azure Resource Manager**
3. **Service principal (automatic)**
4. Name: `AzureSubscription`
5. Select your subscription and resource group
6. ✅ Grant access to all pipelines
7. **Save**

---

### Step 2: Create Variable Group (3 min)

1. Azure DevOps → **Pipelines** → **Library**
2. **+ Variable group**
3. Name: `AllowanceTracker-Production`
4. Add these variables:

**Required Variables:**
```
ResourceGroupName          = allowancetracker-rg
ApiAppServiceName          = allowancetracker-api
FunctionAppName            = allowancetracker-func
StorageAccountName         = allowancetrackerweb
```

**Secret Variable** (click 🔒 lock icon):
```
AzureSqlConnectionString   = Server=tcp:... (for EF migrations)
```

5. **Save**

**Note:** The pipeline does NOT configure any app settings. The SQL connection string is ONLY used for running database migrations. ALL runtime configuration happens in Azure Portal (next step).

---

### Step 3: Configure App Service Settings in Azure Portal (5 min)

**All application configuration is managed in Azure Portal, NOT the pipeline.**

#### A. Get SQL Connection String
```bash
# Azure Portal → SQL Database → Connection strings → ADO.NET
# Or use CLI:
az sql db show-connection-string --client ado.net \
  --server YOUR-SQL-SERVER --name YOUR-DATABASE

# Example:
# Server=tcp:allowancetracker-sql.database.windows.net,1433;
# Initial Catalog=allowancetracker-db;User ID=sqladmin;
# Password=YourPass123!;Encrypt=True;
```

#### B. Configure API App Service

1. **Azure Portal** → **App Services** → `allowancetracker-api` → **Configuration**

2. **Connection strings** → **+ New connection string**
   ```
   Name: DefaultConnection
   Value: <SQL connection string from above>
   Type: SQLAzure
   ```

3. **Application settings** → **+ New application setting** (add each):
   ```
   ASPNETCORE_ENVIRONMENT    = Production
   Jwt__SecretKey            = <generate: openssl rand -base64 32>
   Jwt__Issuer               = AllowanceTracker
   Jwt__Audience             = AllowanceTracker
   Jwt__ExpiryInDays         = 7
   AllowedHosts              = *
   ```

4. **Save**

#### C. Configure Function App

1. **Azure Portal** → **Function Apps** → `allowancetracker-func` → **Configuration**

2. **Connection strings** → **+ New connection string**
   ```
   Name: DefaultConnection
   Value: <same SQL connection string>
   Type: SQLAzure
   ```

3. **Application settings** → **+ New application setting** (add each):
   ```
   AzureWebJobsStorage       = <Storage account connection string>
   FUNCTIONS_WORKER_RUNTIME  = dotnet-isolated
   ```

   Get storage connection string:
   ```bash
   az storage account show-connection-string \
     --name YOUR-STORAGE-ACCOUNT --resource-group YOUR-RG
   ```

4. **Save**

---

### Step 4: Import Pipeline (3 min)

1. Azure DevOps → **Pipelines** → **New pipeline**
2. **GitHub** → Select your repository
3. **Existing Azure Pipelines YAML file**
4. Path: `/azure-pipelines.yml`
5. **Continue**
6. Click **Variables** → **Variable groups**
7. **Link variable group** → Select `AllowanceTracker-Production`
8. **Save** (don't run yet!)

---

### Step 5: First Run (2 min)

1. **Run pipeline**
2. Select branch: `main`
3. **Run**
4. Watch the build stages execute
5. Deployment will happen automatically on main branch

---

## Verification

After pipeline completes:

### ✅ Check API Health
```bash
curl https://YOUR-API-NAME.azurewebsites.net/health
```

Expected response:
```json
{
  "status": "Healthy",
  "checks": [...]
}
```

### ✅ Check React App
```bash
curl https://YOUR-STORAGE-ACCOUNT.z13.web.core.windows.net/
```

Expected: HTML content

### ✅ Check Function App
Azure Portal → Function App → Functions → Monitor

---

## Troubleshooting

### ❌ "Variable not found"
- Check variable group is linked to pipeline
- Verify variable names match exactly (case-sensitive)
- Save pipeline after linking variable group

### ❌ "Cannot connect to database"
- Verify SQL firewall allows Azure services
- Test connection string locally
- Check password doesn't have special characters

### ❌ "Service connection not authorized"
- Go to Service connections → AzureSubscription
- Check status is "Ready"
- Verify permissions on subscription

### ❌ "Health check failed"
- Wait 1-2 minutes for app to warm up
- Check App Service logs in Azure Portal
- Verify database migrations ran successfully

---

## Configuration Quick Reference

### Pipeline Variables (Azure DevOps)

| Variable | Where to Get It | Secret? | Purpose |
|----------|----------------|---------|---------|
| `ResourceGroupName` | Azure Portal | No | Resource group name |
| `ApiAppServiceName` | Azure Portal → App Services | No | API app name |
| `FunctionAppName` | Azure Portal → Function Apps | No | Function app name |
| `StorageAccountName` | Azure Portal → Storage Accounts | No | Storage for React |
| `AzureSqlConnectionString` | SQL Database → Connection strings | **Yes** 🔒 | **Migrations only** |

### App Service Settings (Azure Portal)

| Setting | Where to Configure | Value |
|---------|-------------------|-------|
| `DefaultConnection` | App Service/Function App → Configuration → Connection strings | SQL connection string |
| `Jwt__SecretKey` | App Service → Configuration → App settings | `openssl rand -base64 32` |
| `Jwt__Issuer` | App Service → Configuration → App settings | AllowanceTracker |
| `Jwt__Audience` | App Service → Configuration → App settings | AllowanceTracker |
| `Jwt__ExpiryInDays` | App Service → Configuration → App settings | 7 |
| `ASPNETCORE_ENVIRONMENT` | App Service → Configuration → App settings | Production |
| `AzureWebJobsStorage` | Function App → Configuration → App settings | Storage connection string |
| `FUNCTIONS_WORKER_RUNTIME` | Function App → Configuration → App settings | dotnet-isolated |

---

## What the Pipeline Does

1. **Build Stage** (3 min)
   - ✅ Build .NET API
   - ✅ Build Azure Function
   - ✅ Build React app
   - ✅ Run tests
   - ✅ Code quality checks

2. **Deploy Stage** (5 min) - Only on `main` branch
   - ✅ Deploy API to App Service
   - ✅ Configure app settings
   - ✅ Run database migrations
   - ✅ Health check verification
   - ✅ Deploy Function App
   - ✅ Deploy React to Storage
   - ✅ Purge CDN (if configured)

**Total time:** ~8 minutes

---

## Optional: Application Insights

Add monitoring (recommended for production):

1. Create Application Insights in Azure Portal
2. Copy connection string
3. Add to variable group:
   ```
   ApplicationInsightsConnectionString = InstrumentationKey=...
   ```
4. Re-run pipeline

---

## Optional: CDN Setup

For better performance:

1. Create CDN Profile and Endpoint in Azure Portal
2. Point CDN to storage static website URL
3. Add to variable group:
   ```
   CdnProfileName = allowancetracker-cdn
   CdnEndpointName = allowancetracker-web
   ```
4. Re-run pipeline (will auto-purge CDN after deployment)

---

## Next Steps

- ✅ Set up branch policies on `main`
- ✅ Configure deployment approvals (Pipelines → Environments → production)
- ✅ Set up Application Insights alerts
- ✅ Configure custom domain for App Service
- ✅ Configure custom domain for Storage static website

---

## Getting Help

- 📖 Full setup guide: [docs/azure-devops-setup.md](azure-devops-setup.md)
- 📋 Complete variable list: [docs/AZURE-VARIABLES-CHECKLIST.md](AZURE-VARIABLES-CHECKLIST.md)
- 🏥 Health checks: [docs/health-checks.md](health-checks.md)
