# Azure DevOps Pipeline Variables - Setup Checklist

This checklist contains **all variables** you need to configure in Azure DevOps before running the deployment pipeline.

## Step 1: Create Variable Group

1. Go to **Azure DevOps** → Your Project → **Pipelines** → **Library**
2. Click **+ Variable group**
3. Name it: `AllowanceTracker-Production`
4. Click **+ Add** to add each variable below

---

## Step 2: Add Required Variables

### ✅ Required Variables (Must Have)

| Variable Name | Description | Example Value | Secret? | How to Get |
|--------------|-------------|---------------|---------|------------|
| `ResourceGroupName` | Azure resource group name | `allowancetracker-rg` | ❌ No | Your Azure resource group name |
| `ApiAppServiceName` | App Service name for API | `allowancetracker-api` | ❌ No | Your App Service name |
| `FunctionAppName` | Function App name | `allowancetracker-func` | ❌ No | Your Function App name |
| `StorageAccountName` | Storage account for React app | `allowancetrackerweb` | ❌ No | Your Storage Account name (no dashes!) |
| `AzureSqlConnectionString` | **For EF migrations only**<br/>Runtime uses Azure Portal config | `Server=tcp:...` | ✅ **Yes** | See below ⬇️ |

**Important:**
- The pipeline does NOT configure any app settings (all in Azure Portal)
- The pipeline DOES run database migrations (needs connection string for this)
- Connection string is used ONLY for migrations, not configured as app setting
- See [Azure App Service Configuration](AZURE-APP-SERVICE-CONFIG.md)

### 🔧 Optional Variables (Nice to Have)

| Variable Name | Description | Example Value | Secret? | When Needed |
|--------------|-------------|---------------|---------|-------------|
| `ApplicationInsightsConnectionString` | App Insights for monitoring | `InstrumentationKey=...` | ❌ No | If using Application Insights |
| `ReactAppApiUrl` | API URL for React build | `https://allowancetracker-api.azurewebsites.net` | ❌ No | For React app (defaults to localhost) |
| `CdnProfileName` | CDN profile name | `allowancetracker-cdn` | ❌ No | If using Azure CDN |
| `CdnEndpointName` | CDN endpoint name | `allowancetracker-web` | ❌ No | If using Azure CDN |

---

## Step 3: Get SQL Connection String (For Migrations)

### 🔐 Get SQL Connection String

**Option 1: Azure Portal**
1. Go to **Azure Portal** → **SQL Databases** → Your database
2. Click **Connection strings** (left menu)
3. Copy **ADO.NET** connection string
4. Replace `{your_password}` with actual SQL admin password

**Option 2: Azure CLI**
```bash
# Get the connection string template
az sql db show-connection-string \
  --client ado.net \
  --server allowancetracker-sql \
  --name allowancetracker-db

# Example output:
# Server=tcp:allowancetracker-sql.database.windows.net,1433;
# Initial Catalog=allowancetracker-db;
# Persist Security Info=False;
# User ID={your_username};
# Password={your_password};
# MultipleActiveResultSets=False;
# Encrypt=True;
# TrustServerCertificate=False;
# Connection Timeout=30;

# Replace {your_username} and {your_password} with actual values
```

**Final Value Example:**
```
Server=tcp:allowancetracker-sql.database.windows.net,1433;Initial Catalog=allowancetracker-db;Persist Security Info=False;User ID=sqladmin;Password=YourPassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

**Note:** You'll also need to add this connection string in Azure Portal for runtime use. See [Azure App Service Configuration](AZURE-APP-SERVICE-CONFIG.md) for details.

---

## Step 4: Configure Azure Service Connection

### 📊 Get Application Insights Connection String (Optional)

**Option 1: Azure Portal**
1. Go to **Azure Portal** → **Application Insights** → Your instance
2. Click **Properties** (left menu)
3. Copy **Connection String**

**Option 2: Azure CLI**
```bash
az monitor app-insights component show \
  --app allowancetracker-insights \
  --resource-group allowancetracker-rg \
  --query connectionString \
  --output tsv
```

**Example:**
```
InstrumentationKey=12345678-1234-1234-1234-123456789abc;IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/
```

---

## Step 4: Configure Variables in Azure DevOps

### For Regular Variables (Not Secret)

1. In Variable Group, click **+ Add**
2. Enter **Name** (e.g., `ResourceGroupName`)
3. Enter **Value** (e.g., `allowancetracker-rg`)
4. Click **OK**

### For Secret Variables (Passwords, Keys, Connection Strings)

1. In Variable Group, click **+ Add**
2. Enter **Name** (e.g., `AzureSqlConnectionString`)
3. Enter **Value** (your connection string)
4. Click the **🔒 Lock icon** to mark as secret
5. Click **OK**

**Important:** Secret variables will show as `******` in the UI and logs.

---

## Step 5: Create Service Connection (Not a Variable!)

`AzureSubscription` is **not** a variable - it's a **Service Connection**.

### Create Azure Resource Manager Service Connection:

1. Go to **Project Settings** → **Service connections**
2. Click **New service connection**
3. Select **Azure Resource Manager** → **Next**
4. Choose **Service principal (automatic)** → **Next**
5. Configure:
   - **Subscription:** Select your Azure subscription
   - **Resource group:** `allowancetracker-rg` (or leave empty for subscription-level)
   - **Service connection name:** `AzureSubscription`
   - ✅ **Grant access permission to all pipelines**
6. Click **Save**

---

## Step 6: Link Variable Group to Pipeline

1. Go to **Pipelines** → Select your pipeline → **Edit**
2. Click **Variables** (top right)
3. Click **Variable groups**
4. Click **Link variable group**
5. Select `AllowanceTracker-Production`
6. Click **Link**
7. **Save** the pipeline

---

## Quick Reference Table

Use this table to track what you've configured:

### Pipeline Variables

| Variable | Status | Notes |
|----------|--------|-------|
| ☐ `ResourceGroupName` | Required | Azure resource group |
| ☐ `ApiAppServiceName` | Required | App Service name |
| ☐ `FunctionAppName` | Required | Function App name |
| ☐ `StorageAccountName` | Required | Storage account (no dashes) |
| ☐ `AzureSqlConnectionString` | 🔒 Required Secret | For migrations only |
| ☐ `ApplicationInsightsConnectionString` | Optional | App Insights |
| ☐ `ReactAppApiUrl` | Optional | API URL for React |
| ☐ `CdnProfileName` | Optional | CDN profile |
| ☐ `CdnEndpointName` | Optional | CDN endpoint |

### Azure Portal Configuration (NOT in Pipeline!)

| Setting | Location | Notes |
|---------|----------|-------|
| ☐ `DefaultConnection` | App Service → Configuration → Connection strings | SQL connection |
| ☐ `DefaultConnection` | Function App → Configuration → Connection strings | SQL connection |
| ☐ `Jwt__SecretKey` | App Service → Configuration → App settings | JWT signing key |
| ☐ `Jwt__Issuer` | App Service → Configuration → App settings | "AllowanceTracker" |
| ☐ `Jwt__Audience` | App Service → Configuration → App settings | "AllowanceTracker" |
| ☐ `Jwt__ExpiryInDays` | App Service → Configuration → App settings | "7" |
| ☐ `ASPNETCORE_ENVIRONMENT` | App Service → Configuration → App settings | "Production" |
| ☐ `AzureWebJobsStorage` | Function App → Configuration → App settings | Storage connection |
| ☐ `FUNCTIONS_WORKER_RUNTIME` | Function App → Configuration → App settings | "dotnet-isolated" |

### Service Connections

| Connection | Status | Notes |
|----------|--------|-------|
| ☐ `AzureSubscription` | Service Connection | Not a variable! |

---

## Verification

After configuring all variables, verify:

### 1. Check Variable Group
```
Pipelines → Library → AllowanceTracker-Production
```
- Should show all variables
- Secret variables show as `******`
- At least 7 variables configured

### 2. Check Service Connection
```
Project Settings → Service connections → AzureSubscription
```
- Status: ✅ Ready
- Type: Azure Resource Manager
- Authentication: Service Principal

### 3. Test Pipeline
```
Pipelines → Your Pipeline → Run pipeline
```
- Select branch: `main`
- Click **Run**
- Watch for variable errors in logs

---

## Common Issues

### ❌ "Variable $(VariableName) not found"
**Solution:**
- Check variable name spelling (case-sensitive!)
- Ensure variable group is linked to pipeline
- Save pipeline after linking

### ❌ "The connection string is invalid"
**Solution:**
- Verify connection string format
- Check password doesn't have special characters that need escaping
- Test connection string locally first

### ❌ "Service connection not found"
**Solution:**
- Verify service connection name is exactly `AzureSubscription`
- Grant pipeline permissions to service connection
- Check service connection is not expired

### ❌ "Storage account name is invalid"
**Solution:**
- Storage account names must be lowercase
- No dashes or special characters
- 3-24 characters, alphanumeric only

---

## Security Best Practices

1. ✅ Mark all secrets as secret (lock icon)
2. ✅ Use different JWT keys per environment
3. ✅ Rotate secrets periodically (every 90 days)
4. ✅ Limit variable group access to required teams
5. ✅ Never commit secrets to source control
6. ✅ Use Azure Key Vault for production secrets (advanced)

---

## Next Steps

After configuring all variables:

1. ✅ Test the pipeline with a manual run
2. ✅ Verify health checks pass after deployment
3. ✅ Set up Application Insights monitoring
4. ✅ Configure alerts for failures
5. ✅ Document any custom variables you add

---

## Need Help?

- **Azure CLI not installed?** https://docs.microsoft.com/cli/azure/install-azure-cli
- **Can't find resource?** Use `az resource list --resource-group allowancetracker-rg`
- **Service principal issues?** Ensure you have Owner/Contributor role on subscription
