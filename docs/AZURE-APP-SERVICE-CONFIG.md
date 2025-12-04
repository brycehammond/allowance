# Azure App Service Configuration Guide

## Overview

**ALL application configuration is managed in Azure Portal.** The Azure DevOps pipeline does NOT configure any app settings or connection strings - it only deploys code.

## Configuration Strategy

### ✅ Everything Configured in Azure Portal
**All settings are managed in App Service Configuration:**
- SQL Connection Strings
- JWT configuration (SecretKey, Issuer, Audience, ExpiryInDays)
- ASPNETCORE_ENVIRONMENT
- Storage Account connections (AzureWebJobsStorage)
- Application Insights connection strings
- SendGrid Email configuration
- AllowedHosts
- Custom domains and SSL certificates

### 🚫 Pipeline Does NOT Configure
**The pipeline ONLY:**
- Builds and deploys code
- Runs database migrations (needs connection string variable for this only)
- Verifies health after deployment

This separation ensures:
- Security (secrets stay in Azure)
- Flexibility (update settings without redeployment)
- Compliance (audit trail in Azure)
- Environment independence (same pipeline, different configs)

---

## Complete Configuration Checklist

### API App Service Configuration

**Connection Strings:**
| Name | Value | Type |
|------|-------|------|
| `DefaultConnection` | SQL connection string | SQLAzure |

**Application Settings:**
| Name | Value | Notes |
|------|-------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment name |
| `Jwt__SecretKey` | Generate with `openssl rand -base64 32` | Keep secret! |
| `Jwt__Issuer` | `AllowanceTracker` | Token issuer |
| `Jwt__Audience` | `AllowanceTracker` | Token audience |
| `Jwt__ExpiryInDays` | `7` | Token lifetime |
| `AllowedHosts` | `*` | CORS hosts |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection string | Optional |
| `SendGrid__ApiKey` | SendGrid API key | For email |
| `SendGrid__FromEmail` | Sender email address | For email |
| `SendGrid__FromName` | Sender display name | For email |

### Function App Configuration

**Connection Strings:**
| Name | Value | Type |
|------|-------|------|
| `DefaultConnection` | SQL connection string | SQLAzure |

**Application Settings:**
| Name | Value | Notes |
|------|-------|-------|
| `AzureWebJobsStorage` | Storage account connection string | Required for Functions |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` | Runtime type |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection string | Optional |

---

## Step 1: Configure Connection String in Azure Portal

### For API App Service

1. **Navigate to App Service**
   ```
   Azure Portal → App Services → allowancetracker-api
   ```

2. **Add Connection String**
   - Click **Configuration** (left menu under Settings)
   - Click **Connection strings** tab
   - Click **+ New connection string**

3. **Configure:**
   ```
   Name: DefaultConnection
   Value: Server=tcp:allowancetracker-sql.database.windows.net,1433;Initial Catalog=allowancetracker-db;User ID=sqladmin;Password=YourPassword123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
   Type: SQLAzure
   Deployment slot setting: ✅ (checked)
   ```

4. **Click OK**, then **Save**

### For Function App

1. **Navigate to Function App**
   ```
   Azure Portal → Function Apps → allowancetracker-func
   ```

2. **Add Connection String** (same process as above)
   ```
   Name: DefaultConnection
   Value: <same SQL connection string>
   Type: SQLAzure
   Deployment slot setting: ✅
   ```

3. **Click OK**, then **Save**

---

## Step 2: Get Connection String

### Option 1: From Azure Portal

1. Go to **SQL Databases** → Your database
2. Click **Connection strings** (left menu)
3. Copy **ADO.NET** connection string
4. Replace `{your_password}` with actual SQL admin password

### Option 2: Azure CLI

```bash
# Get connection string template
az sql db show-connection-string \
  --client ado.net \
  --server allowancetracker-sql \
  --name allowancetracker-db

# Output (replace {your_username} and {your_password}):
Server=tcp:allowancetracker-sql.database.windows.net,1433;
Initial Catalog=allowancetracker-db;
Persist Security Info=False;
User ID={your_username};
Password={your_password};
MultipleActiveResultSets=False;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

### Option 3: From Key Vault (Production Best Practice)

Instead of storing the password directly, reference Azure Key Vault:

1. **Store secret in Key Vault:**
   ```bash
   az keyvault secret set \
     --vault-name allowancetracker-vault \
     --name SqlConnectionString \
     --value "Server=tcp:..."
   ```

2. **Configure App Service to use Key Vault reference:**
   ```
   Name: DefaultConnection
   Value: @Microsoft.KeyVault(SecretUri=https://allowancetracker-vault.vault.azure.net/secrets/SqlConnectionString/)
   Type: SQLAzure
   ```

3. **Grant App Service access to Key Vault:**
   ```bash
   # Enable managed identity on App Service
   az webapp identity assign \
     --name allowancetracker-api \
     --resource-group allowancetracker-rg

   # Grant access to Key Vault
   az keyvault set-policy \
     --name allowancetracker-vault \
     --object-id <managed-identity-object-id> \
     --secret-permissions get list
   ```

---

## Why Connection String in Portal, Not Pipeline?

### ✅ Advantages

1. **Separation of Concerns**
   - Infrastructure config ≠ Application config
   - Database credentials are infrastructure-level secrets

2. **Security**
   - Stored in Azure, not in pipeline variables
   - Can use Key Vault references
   - Easier to rotate without pipeline changes

3. **Environment Independence**
   - Same pipeline can deploy to dev/staging/prod
   - Each environment has its own connection string
   - No need to manage per-environment pipeline variables

4. **Compliance**
   - Meets security audit requirements
   - Secrets stored in Azure, not DevOps
   - Can enable Key Vault audit logging

### ⚠️ Pipeline Still Needs It For:

**EF Core Migrations Only**
- Pipeline runs migrations BEFORE deployment
- Needs connection string to update database schema
- Stored as pipeline variable: `AzureSqlConnectionString`

---

## Complete Configuration Checklist

### In Azure Portal - App Service

| Setting | Type | Where | Value |
|---------|------|-------|-------|
| DefaultConnection | Connection String | Configuration → Connection strings | SQL connection string |
| SendGrid__ApiKey | App Setting | Configuration → App settings | SendGrid API key |
| SendGrid__FromEmail | App Setting | Configuration → App settings | Sender email address |
| SendGrid__FromName | App Setting | Configuration → App settings | Sender display name |

### In Azure Portal - Function App

| Setting | Type | Where | Value |
|---------|------|-------|-------|
| DefaultConnection | Connection String | Configuration → Connection strings | SQL connection string |
| AzureWebJobsStorage | App Setting | Configuration → App settings | Set by pipeline |

### In Azure DevOps - Pipeline Variables

| Variable | Purpose | Example |
|----------|---------|---------|
| AzureSqlConnectionString | **Migrations only** | Server=tcp:... |
| JwtSecretKey | JWT signing | Generated secret |
| AzureWebJobsStorage | Functions storage | DefaultEndpointsProtocol=https:... |
| ApplicationInsightsConnectionString | Monitoring (optional) | InstrumentationKey=... |

---

## Verification

### Test App Service Configuration

```bash
# Test the API can connect to database
curl https://allowancetracker-api.azurewebsites.net/health

# Expected response:
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy"
    }
  ]
}
```

### View App Service Configuration

```bash
# List all app settings
az webapp config appsettings list \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg

# List connection strings (values masked)
az webapp config connection-string list \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg
```

---

## Updating Configuration

### To Update Connection String

1. **Azure Portal** → App Service → Configuration
2. Click on the connection string
3. Update the value
4. **Save**
5. App Service will automatically restart

### To Rotate SQL Password

```bash
# 1. Change SQL password in Azure
az sql server update \
  --name allowancetracker-sql \
  --resource-group allowancetracker-rg \
  --admin-password "NewPassword123!"

# 2. Update connection string in App Service
# (Use Azure Portal or CLI)

# 3. Update pipeline variable for migrations
# Azure DevOps → Pipelines → Library → Variable Group
```

---

## Troubleshooting

### ❌ "Cannot open database"

**Cause:** App Service can't connect to SQL Server

**Check:**
1. Connection string is correctly configured
2. SQL Server firewall allows Azure services
3. Password is correct (no special character issues)

**Fix:**
```bash
# Allow Azure services
az sql server firewall-rule create \
  --resource-group allowancetracker-rg \
  --server allowancetracker-sql \
  --name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

### ❌ "Login failed for user"

**Cause:** Incorrect username or password

**Fix:**
1. Verify SQL admin username (usually `sqladmin`)
2. Reset SQL admin password if needed
3. Update connection string in App Service

### ❌ Health check shows "Unhealthy"

**Cause:** Database connection issue

**Steps:**
1. Check App Service logs: `az webapp log tail`
2. Verify connection string format
3. Test connection manually with SQL client
4. Check SQL Server is running

---

## Best Practices

1. ✅ **Use Key Vault for production**
   - Store all secrets in Azure Key Vault
   - Reference via `@Microsoft.KeyVault(...)` syntax
   - Enable managed identity

2. ✅ **Use deployment slots for staging**
   - Different connection strings per slot
   - Test in staging before swapping to production

3. ✅ **Enable diagnostics**
   - App Service Logs → Enable Application Logging
   - Monitor connection failures
   - Set up alerts for errors

4. ✅ **Rotate credentials regularly**
   - Change SQL password every 90 days
   - Update Key Vault secret
   - App Service auto-refreshes from Key Vault

5. ✅ **Use connection pooling**
   - Default in Entity Framework Core
   - No additional configuration needed
   - Monitor with Application Insights

---

## Related Documentation

- [Azure DevOps Setup](azure-devops-setup.md)
- [Variable Checklist](AZURE-VARIABLES-CHECKLIST.md)
- [Health Checks](health-checks.md)
