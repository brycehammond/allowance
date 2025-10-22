# Pipeline Variables - Final Summary

## Overview

The Azure DevOps pipeline has been simplified to **NOT manage any app settings**. All application configuration is handled in Azure Portal.

## What You Need to Configure

### 1. Azure DevOps Pipeline Variables (5 Required)

**Variable Group Name:** `AllowanceTracker-Production`

| Variable | Type | Purpose | Example |
|----------|------|---------|---------|
| `ResourceGroupName` | Plain text | Azure resource group | `allowancetracker-rg` |
| `ApiAppServiceName` | Plain text | API App Service name | `allowancetracker-api` |
| `FunctionAppName` | Plain text | Function App name | `allowancetracker-func` |
| `StorageAccountName` | Plain text | Storage account for React | `allowancetrackerweb` |
| `AzureSqlConnectionString` | 🔒 Secret | **For EF migrations only** | `Server=tcp:...` |

**Optional Variables:**
| Variable | Type | Purpose |
|----------|------|---------|
| `ReactAppApiUrl` | Plain text | API URL for React build |
| `CdnProfileName` | Plain text | CDN profile (if using) |
| `CdnEndpointName` | Plain text | CDN endpoint (if using) |
| `ApplicationInsightsConnectionString` | Plain text | App Insights (optional) |

### 2. Azure Portal - API App Service

**Location:** Azure Portal → App Services → `allowancetracker-api` → Configuration

**Connection Strings:**
```
Name: DefaultConnection
Value: <SQL connection string>
Type: SQLAzure
```

**Application Settings:**
```
ASPNETCORE_ENVIRONMENT    = Production
Jwt__SecretKey            = <generate: openssl rand -base64 32>
Jwt__Issuer               = AllowanceTracker
Jwt__Audience             = AllowanceTracker
Jwt__ExpiryInDays         = 7
AllowedHosts              = *
```

**Optional Settings:**
```
APPLICATIONINSIGHTS_CONNECTION_STRING = <App Insights connection>
SendGrid__ApiKey                     = <SendGrid API key>
```

### 3. Azure Portal - Function App

**Location:** Azure Portal → Function Apps → `allowancetracker-func` → Configuration

**Connection Strings:**
```
Name: DefaultConnection
Value: <SQL connection string>
Type: SQLAzure
```

**Application Settings:**
```
AzureWebJobsStorage       = <Storage account connection string>
FUNCTIONS_WORKER_RUNTIME  = dotnet-isolated
```

**Optional Settings:**
```
APPLICATIONINSIGHTS_CONNECTION_STRING = <App Insights connection>
```

---

## Why This Separation?

### ✅ Benefits

1. **Security**
   - Secrets stored in Azure, not DevOps
   - Can integrate with Azure Key Vault
   - Audit trail in Azure Portal

2. **Flexibility**
   - Change settings without redeploying
   - Different configs per environment
   - No pipeline changes needed

3. **Simplicity**
   - Pipeline only builds and deploys code
   - Clear separation of concerns
   - Easier to troubleshoot

4. **Compliance**
   - Meets security requirements
   - Azure RBAC controls access
   - No secrets in version control

### Pipeline Responsibilities

The pipeline ONLY handles:
- ✅ Building code
- ✅ Running tests
- ✅ **Running database migrations** (needs connection string for this)
- ✅ Deploying application code
- ✅ Health check verification

The pipeline does NOT:
- ❌ Configure app settings
- ❌ Manage runtime connection strings
- ❌ Set environment variables
- ❌ Configure infrastructure

**Note:** The SQL connection string variable is used ONLY for running EF Core migrations during deployment. The App Service and Function App read their connection strings from Azure Portal configuration.

---

## Setup Steps

### Quick Setup (15 minutes)

1. **Create Variable Group** (2 min)
   - Azure DevOps → Pipelines → Library
   - Create `AllowanceTracker-Production`
   - Add 5 required variables

2. **Configure API App Service** (5 min)
   - Azure Portal → App Services
   - Add connection string
   - Add 6 application settings

3. **Configure Function App** (3 min)
   - Azure Portal → Function Apps
   - Add connection string
   - Add 2 application settings

4. **Import Pipeline** (3 min)
   - Azure DevOps → Pipelines
   - Link to GitHub
   - Link variable group

5. **Run Pipeline** (2 min)
   - Click Run
   - Watch deployment
   - Verify health check

---

## Comparison: Before vs After

### Before (Pipeline Managed Settings)
```yaml
# Pipeline configured 7+ app settings
- JWT secrets in pipeline variables
- Connection strings duplicated
- Settings mixed with deployment
- Hard to update without redeployment
```

### After (Portal-Only Settings) ✅
```yaml
# Pipeline has only 5 variables
# All app settings in Azure Portal
# Clear separation of code vs config
# Easy to update settings independently
```

---

## Frequently Asked Questions

### Q: Why do I need the SQL connection string in both places?
**A:** The pipeline uses it to run EF Core migrations BEFORE deploying the app. The App Service uses the one from Azure Portal at runtime. They're the same connection string, just stored in two places for two different purposes.

### Q: What if I want to change a JWT secret?
**A:** Just update it in Azure Portal → App Service → Configuration. No pipeline changes or redeployment needed.

### Q: Can I use different settings for staging vs production?
**A:** Yes! Each environment (staging, production) can have different configurations in Azure Portal while using the same pipeline.

### Q: What about Key Vault integration?
**A:** Easy! In Azure Portal, use Key Vault references:
```
@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/JwtSecret/)
```

### Q: How do I see what settings are configured?
**A:** Azure Portal → App Service → Configuration shows all settings in one place.

---

## Documentation Links

- [Complete Variable Checklist](AZURE-VARIABLES-CHECKLIST.md)
- [Azure App Service Configuration](AZURE-APP-SERVICE-CONFIG.md)
- [Quick Start Guide](QUICK-START-AZURE-PIPELINE.md)
- [Full Azure DevOps Setup](azure-devops-setup.md)

---

## Summary

**Pipeline Variables:** 5 required
- 4 resource names (Resource Group, App Service, Function App, Storage)
- 1 secret (SQL connection string for EF migrations)

**Azure Portal Settings:** Everything else
- Connection strings (for runtime)
- JWT configuration
- Environment settings
- Application-specific configuration

**Key Point:** SQL connection string appears in TWO places:
1. Azure DevOps variable (for migrations during deployment)
2. Azure Portal App Service config (for runtime database access)

**Result:** Clean separation, better security, easier management
