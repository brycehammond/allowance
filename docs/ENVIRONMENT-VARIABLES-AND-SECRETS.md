# Environment Variables & Secrets - Complete Reference

This document provides a comprehensive reference for all environment variables, secrets, and configuration needed for the Allowance Tracker application across different environments.

## Table of Contents

- [Quick Summary](#quick-summary)
- [GitHub Actions Secrets](#github-actions-secrets)
- [Azure App Service Configuration](#azure-app-service-configuration)
- [Azure Function App Configuration](#azure-function-app-configuration)
- [React Application Environment Variables](#react-application-environment-variables)
- [Local Development](#local-development)
- [Security Best Practices](#security-best-practices)
- [Troubleshooting](#troubleshooting)

---

## Quick Summary

### What You Need for CI/CD (Build & Test Only)

**✅ ZERO secrets required!**

The GitHub Actions workflows for building and testing work out of the box without any configuration.

### What You Need for Azure Deployment

**Required GitHub Secrets**: 6 secrets
**Optional GitHub Secrets**: 2 secrets (CDN only)
**Azure Portal Configuration**: Connection strings, app settings, JWT keys

---

## GitHub Actions Secrets

These secrets are stored in GitHub repository settings and used by the deployment workflows.

### Navigate to Secrets

1. Go to your GitHub repository
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** to add each one

### Required Secrets (6)

| Secret Name | Required For | How to Get | Example Value |
|-------------|--------------|------------|---------------|
| `AZURE_CREDENTIALS` | All Azure deployments | See [Creating Service Principal](#creating-service-principal) | JSON object |
| `AZURE_WEBAPP_NAME` | API deployment | Your App Service name | `allowancetracker-api` |
| `AZURE_FUNCTIONAPP_NAME` | Function deployment | Your Function App name | `allowancetracker-func` |
| `AZURE_STORAGE_ACCOUNT` | Web deployment | Your Storage account name | `allowancetrackerapp` |
| `AZURE_RESOURCE_GROUP` | Web deployment | Your resource group name | `allowancetracker-rg` |
| `VITE_API_URL` | Web deployment | Your API URL | `https://allowancetracker-api.azurewebsites.net` |

### Optional Secrets (2)

| Secret Name | Required For | How to Get | Example Value |
|-------------|--------------|------------|---------------|
| `AZURE_CDN_PROFILE` | CDN purge (optional) | Your CDN profile name | `allowancetracker-cdn` |
| `AZURE_CDN_ENDPOINT` | CDN purge (optional) | Your CDN endpoint name | `allowancetracker` |

### Creating Service Principal

The `AZURE_CREDENTIALS` secret contains a service principal that allows GitHub Actions to authenticate with Azure.

**Step 1: Create Service Principal**

```bash
# Replace <SUBSCRIPTION_ID> with your Azure subscription ID
# Get subscription ID: az account show --query id --output tsv

az ad sp create-for-rbac \
  --name "allowancetracker-github-actions" \
  --role contributor \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/allowancetracker-rg \
  --sdk-auth
```

**Step 2: Copy the JSON Output**

The command will output JSON like this (copy the entire thing):

```json
{
  "clientId": "12345678-1234-1234-1234-123456789012",
  "clientSecret": "abc123-secret-xyz789",
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

**Step 3: Add to GitHub**

1. Go to GitHub → Settings → Secrets and variables → Actions
2. Click **New repository secret**
3. Name: `AZURE_CREDENTIALS`
4. Value: Paste the entire JSON output
5. Click **Add secret**

### Getting Other Secret Values

**AZURE_WEBAPP_NAME**:
```bash
az webapp list --resource-group allowancetracker-rg --query "[].name" --output tsv
```

**AZURE_FUNCTIONAPP_NAME**:
```bash
az functionapp list --resource-group allowancetracker-rg --query "[].name" --output tsv
```

**AZURE_STORAGE_ACCOUNT**:
```bash
az storage account list --resource-group allowancetracker-rg --query "[].name" --output tsv
```

**VITE_API_URL**:
```bash
# Get your App Service URL
az webapp show \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --query defaultHostName \
  --output tsv

# Then prefix with https://
# Example: https://allowancetracker-api.azurewebsites.net
```

---

## Azure App Service Configuration

The API App Service requires configuration in the Azure Portal. **These are NOT set in GitHub Actions** - they're managed directly in Azure.

### Where to Configure

Azure Portal → App Services → **allowancetracker-api** → **Configuration**

### Connection Strings

Navigate to: **Configuration** → **Connection strings** tab

| Name | Value | Type | Deployment Slot Setting |
|------|-------|------|------------------------|
| `DefaultConnection` | SQL connection string (see below) | `SQLAzure` | ✅ Checked |

**SQL Connection String Format**:
```
Server=tcp:allowancetracker-sql.database.windows.net,1433;Initial Catalog=allowancetracker-db;User ID=sqladmin;Password=YOUR_PASSWORD_HERE;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;MultipleActiveResultSets=true
```

**How to Get SQL Connection String**:

Option 1 - Azure Portal:
1. Go to **SQL Databases** → Your database
2. Click **Connection strings** (left menu)
3. Copy **ADO.NET** connection string
4. Replace `{your_password}` with actual SQL admin password

Option 2 - Azure CLI:
```bash
az sql db show-connection-string \
  --client ado.net \
  --server allowancetracker-sql \
  --name allowancetracker-db

# Replace {your_username} and {your_password} with actual values
```

### Application Settings

Navigate to: **Configuration** → **Application settings** tab

| Name | Value | Notes |
|------|-------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment name |
| `Jwt__SecretKey` | Generate secure key (see below) | **Keep secret!** Min 32 chars |
| `Jwt__Issuer` | `AllowanceTracker` | JWT token issuer |
| `Jwt__Audience` | `AllowanceTracker` | JWT token audience |
| `Jwt__ExpiryInDays` | `7` | Token lifetime in days |
| `AllowedHosts` | `*` | Allowed HTTP hosts |
| `App__ResetPasswordUrl` | Your frontend reset password URL | Example: `https://yourdomain.com/reset-password` |
| `SendGrid__ApiKey` | Your SendGrid API key | Optional - for email features |
| `SendGrid__FromEmail` | `noreply@yourdomain.com` | Optional - sender email |
| `SendGrid__FromName` | `Allowance Tracker` | Optional - sender name |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection string | Optional - for monitoring |

**Generating JWT Secret Key**:
```bash
# Generate a secure 32+ character key
openssl rand -base64 32

# Or use this Python one-liner
python3 -c "import secrets; print(secrets.token_urlsafe(32))"

# Or use this PowerShell command
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

### Using Azure Key Vault (Production Best Practice)

Instead of storing secrets directly, reference Azure Key Vault:

**Step 1: Store Secret in Key Vault**
```bash
# Store SQL connection string
az keyvault secret set \
  --vault-name allowancetracker-vault \
  --name SqlConnectionString \
  --value "Server=tcp:..."

# Store JWT secret key
az keyvault secret set \
  --vault-name allowancetracker-vault \
  --name JwtSecretKey \
  --value "your-generated-secret-key"
```

**Step 2: Enable Managed Identity on App Service**
```bash
az webapp identity assign \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg
```

**Step 3: Grant App Service Access to Key Vault**
```bash
# Get the managed identity principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --query principalId \
  --output tsv)

# Grant access to Key Vault
az keyvault set-policy \
  --name allowancetracker-vault \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

**Step 4: Update App Service Configuration**

In Azure Portal, use Key Vault references:

| Name | Value |
|------|-------|
| `ConnectionStrings__DefaultConnection` | `@Microsoft.KeyVault(SecretUri=https://allowancetracker-vault.vault.azure.net/secrets/SqlConnectionString/)` |
| `Jwt__SecretKey` | `@Microsoft.KeyVault(SecretUri=https://allowancetracker-vault.vault.azure.net/secrets/JwtSecretKey/)` |

---

## Azure Function App Configuration

The Function App requires configuration in the Azure Portal for background jobs (weekly allowance processing).

### Where to Configure

Azure Portal → Function App → **allowancetracker-func** → **Configuration**

### Connection Strings

| Name | Value | Type | Deployment Slot Setting |
|------|-------|------|------------------------|
| `DefaultConnection` | Same SQL connection string as API | `SQLAzure` | ✅ Checked |

### Application Settings

| Name | Value | Notes |
|------|-------|-------|
| `AzureWebJobsStorage` | Storage account connection string | Required for Functions runtime |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated` | .NET isolated worker runtime |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment name |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Insights connection string | Optional - for monitoring |

**Getting Storage Account Connection String**:

Option 1 - Azure Portal:
1. Go to **Storage accounts** → Your storage account
2. Click **Access keys** (under Security + networking)
3. Copy **Connection string** from key1 or key2

Option 2 - Azure CLI:
```bash
az storage account show-connection-string \
  --name allowancetrackerdata \
  --resource-group allowancetracker-rg \
  --query connectionString \
  --output tsv
```

---

## React Application Environment Variables

The React application uses Vite for build configuration. Environment variables are set at build time.

### Build-Time Variables (GitHub Actions)

Set in GitHub Secrets and used during build:

| Variable | Set In | Used For | Example Value |
|----------|--------|----------|---------------|
| `VITE_API_URL` | GitHub Secret | API endpoint for production | `https://allowancetracker-api.azurewebsites.net` |

**Workflow Configuration**:
```yaml
# In .github/workflows/deploy-web.yml
- name: Build React app
  working-directory: ./web
  run: npm run build
  env:
    VITE_API_URL: ${{ secrets.VITE_API_URL }}
```

### Local Development (`.env.development`)

Create `web/.env.development` for local development:

```bash
# Local development API endpoint
VITE_API_URL=https://localhost:5001

# Or if running API in Docker
# VITE_API_URL=http://localhost:5000
```

**Note**: This file is gitignored and used only for local development.

### Environment Files

| File | Purpose | Committed? |
|------|---------|-----------|
| `web/.env.development` | Local development | ❌ No (gitignored) |
| `web/.env.production` | Production defaults (if any) | ✅ Yes (no secrets) |
| GitHub Secrets | Production build values | ❌ No (GitHub only) |

---

## Local Development

### Backend (API) - `appsettings.Development.json`

Located at: `src/AllowanceTracker/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=AllowanceTracker;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "SecretKey": "your-local-dev-secret-key-min-32-chars-change-this-for-prod",
    "Issuer": "AllowanceTracker",
    "Audience": "AllowanceTracker",
    "ExpiryInDays": 1
  },
  "SendGrid": {
    "ApiKey": "YOUR_SENDGRID_API_KEY_HERE",
    "FromEmail": "noreply@localhost",
    "FromName": "Allowance Tracker Dev"
  },
  "App": {
    "ResetPasswordUrl": "http://localhost:5173/reset-password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Note**: `appsettings.Development.json` is gitignored and should contain your local development settings.

### Frontend (React) - `.env.development`

Located at: `web/.env.development`

```bash
VITE_API_URL=https://localhost:5001
```

### SQL Server via Docker (Local Development)

```bash
# Start SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sql-server \
  -d mcr.microsoft.com/mssql/server:2022-latest

# Run migrations
dotnet ef database update --project src/AllowanceTracker/AllowanceTracker.csproj

# Verify database
docker exec -it sql-server /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P "YourStrong@Passw0rd" \
  -Q "SELECT name FROM sys.databases"
```

---

## Security Best Practices

### Secret Management

✅ **DO**:
- Use GitHub repository secrets for all credentials
- Use Azure Key Vault for production secrets
- Rotate secrets every 90 days
- Use different secrets per environment (dev/staging/prod)
- Use managed identities where possible
- Enable secret audit logging in Key Vault
- Delete secrets that are no longer needed

❌ **DON'T**:
- Commit secrets to source control
- Echo secrets in workflow logs or console output
- Store secrets in workflow YAML files
- Share secrets via email or chat
- Use weak or short JWT secret keys (min 32 chars)
- Reuse the same secret across environments

### Service Principal Rotation

Rotate the service principal credential every 12 months:

```bash
# Reset credential for existing service principal
az ad sp credential reset \
  --name allowancetracker-github-actions \
  --sdk-auth
```

Then update the `AZURE_CREDENTIALS` secret in GitHub with the new JSON output.

### JWT Secret Key Best Practices

- **Minimum Length**: 32 characters (256 bits)
- **Complexity**: Use random bytes, not dictionary words
- **Rotation**: Change every 90 days in production
- **Storage**: Store in Azure Key Vault, never in source control
- **Different per Environment**: Dev, staging, and prod should have different keys

### SQL Password Rotation

```bash
# 1. Change SQL password in Azure
az sql server update \
  --name allowancetracker-sql \
  --resource-group allowancetracker-rg \
  --admin-password "NewSecurePassword123!"

# 2. Update connection string in Azure Key Vault
az keyvault secret set \
  --vault-name allowancetracker-vault \
  --name SqlConnectionString \
  --value "Server=tcp:allowancetracker-sql.database.windows.net,1433;Initial Catalog=allowancetracker-db;User ID=sqladmin;Password=NewSecurePassword123!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# 3. Restart App Service and Function App (if not using Key Vault references, update manually)
az webapp restart --name allowancetracker-api --resource-group allowancetracker-rg
az functionapp restart --name allowancetracker-func --resource-group allowancetracker-rg
```

---

## Troubleshooting

### GitHub Actions Deployment Issues

#### Error: "Azure login failed"

**Cause**: `AZURE_CREDENTIALS` secret is incorrect or service principal expired

**Solution**:
```bash
# Recreate service principal
az ad sp create-for-rbac \
  --name "allowancetracker-github-actions" \
  --role contributor \
  --scopes /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/allowancetracker-rg \
  --sdk-auth
```
Update the `AZURE_CREDENTIALS` secret in GitHub with the new JSON output.

#### Error: "Resource not found"

**Cause**: Resource names in secrets don't match actual Azure resources

**Solution**:
1. Verify resource names in Azure Portal
2. Compare with GitHub secrets
3. Update secrets to match exact resource names (case-sensitive)

#### Error: "Health check failed"

**Cause**: API cannot connect to database after deployment

**Solution**:
1. Verify connection string in App Service configuration
2. Check SQL Server firewall allows Azure services:
   ```bash
   az sql server firewall-rule create \
     --resource-group allowancetracker-rg \
     --server allowancetracker-sql \
     --name AllowAzureServices \
     --start-ip-address 0.0.0.0 \
     --end-ip-address 0.0.0.0
   ```
3. Test connection from Azure Portal's SQL Query Editor

### App Service Configuration Issues

#### Error: "Cannot open database"

**Cause**: Connection string is incorrect or SQL firewall blocks connection

**Solution**:
1. Verify connection string format in App Service Configuration
2. Check SQL Server firewall rules
3. Ensure password doesn't contain special characters that need escaping
4. Test connection using Azure Data Studio or SQL Server Management Studio

#### Error: "Login failed for user"

**Cause**: SQL username or password is incorrect

**Solution**:
1. Verify SQL admin username (usually `sqladmin`)
2. Reset SQL admin password if needed
3. Update connection string with correct credentials
4. Restart App Service

#### Error: "JWT token validation failed"

**Cause**: JWT secret key mismatch or missing

**Solution**:
1. Verify `Jwt__SecretKey` is set in App Service configuration
2. Ensure key is at least 32 characters
3. Check Key Vault reference is correct (if using Key Vault)
4. Restart App Service after changing

### React Build Issues

#### Error: "API calls return 404 or CORS errors"

**Cause**: `VITE_API_URL` not set or incorrect

**Solution**:
1. Set `VITE_API_URL` secret in GitHub
2. Rebuild and redeploy React app
3. Verify API URL in browser dev tools (Network tab)
4. Check CORS configuration in API `Program.cs`

#### Error: "Environment variable undefined in production"

**Cause**: Vite requires `VITE_` prefix for environment variables

**Solution**:
1. Ensure variable name starts with `VITE_`
2. Set in GitHub Secrets
3. Reference in workflow: `${{ secrets.VITE_API_URL }}`
4. Rebuild application

---

## Configuration Checklist

Use this checklist to ensure all configuration is complete before deployment.

### GitHub Actions (for CI/CD)

- [ ] `AZURE_CREDENTIALS` - Service principal JSON
- [ ] `AZURE_WEBAPP_NAME` - App Service name
- [ ] `AZURE_FUNCTIONAPP_NAME` - Function App name
- [ ] `AZURE_STORAGE_ACCOUNT` - Storage account name
- [ ] `AZURE_RESOURCE_GROUP` - Resource group name
- [ ] `VITE_API_URL` - Production API URL
- [ ] `AZURE_CDN_PROFILE` - CDN profile (optional)
- [ ] `AZURE_CDN_ENDPOINT` - CDN endpoint (optional)

### Azure App Service (API)

- [ ] Connection String: `DefaultConnection` (SQL)
- [ ] App Setting: `ASPNETCORE_ENVIRONMENT` = `Production`
- [ ] App Setting: `Jwt__SecretKey` (32+ chars)
- [ ] App Setting: `Jwt__Issuer` = `AllowanceTracker`
- [ ] App Setting: `Jwt__Audience` = `AllowanceTracker`
- [ ] App Setting: `Jwt__ExpiryInDays` = `7`
- [ ] App Setting: `AllowedHosts` = `*`
- [ ] App Setting: `App__ResetPasswordUrl` (frontend URL)
- [ ] App Setting: `SendGrid__ApiKey` (optional)
- [ ] App Setting: `APPLICATIONINSIGHTS_CONNECTION_STRING` (optional)

### Azure Function App

- [ ] Connection String: `DefaultConnection` (SQL)
- [ ] App Setting: `AzureWebJobsStorage` (Storage connection string)
- [ ] App Setting: `FUNCTIONS_WORKER_RUNTIME` = `dotnet-isolated`
- [ ] App Setting: `ASPNETCORE_ENVIRONMENT` = `Production`
- [ ] App Setting: `APPLICATIONINSIGHTS_CONNECTION_STRING` (optional)

### Azure Storage Account (React App)

- [ ] Static website hosting enabled
- [ ] Primary endpoint configured
- [ ] CORS configured for API domain
- [ ] CDN configured (optional)

### Local Development

- [ ] `appsettings.Development.json` created (gitignored)
- [ ] SQL Server running (Docker or local)
- [ ] Database migrations applied
- [ ] `web/.env.development` created with `VITE_API_URL`

---

## Quick Reference Commands

### Get Azure Resource Information

```bash
# Get subscription ID
az account show --query id --output tsv

# List all resources in resource group
az resource list --resource-group allowancetracker-rg --output table

# Get App Service URL
az webapp show \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --query defaultHostName \
  --output tsv

# Get Function App URL
az functionapp show \
  --name allowancetracker-func \
  --resource-group allowancetracker-rg \
  --query defaultHostName \
  --output tsv

# Get Storage account connection string
az storage account show-connection-string \
  --name allowancetrackerapp \
  --resource-group allowancetracker-rg \
  --output tsv
```

### Test Deployments

```bash
# Test API health
curl https://allowancetracker-api.azurewebsites.net/health

# Test Function App (warmup)
curl https://allowancetracker-func.azurewebsites.net/api/health

# Test React app
curl https://allowancetrackerapp.z13.web.core.windows.net
```

---

## Related Documentation

- [GitHub Actions CI/CD Setup](./ci-cd/github-actions-ci.md)
- [Azure Deployment Setup](./ci-cd/azure-deployment-setup.md)
- [Azure Quick Start](./ci-cd/azure-quick-start.md)
- [GitHub Secrets Setup](./GITHUB-SECRETS-SETUP.md)
- [Azure App Service Config](./AZURE-APP-SERVICE-CONFIG.md)

---

## Summary

**For CI/CD Only**: ✅ No secrets required
**For Azure Deployment**: 6-8 GitHub secrets + Azure Portal configuration
**For Local Development**: Local config files (gitignored)

Most configuration is managed in Azure Portal, not in GitHub Actions. This separation ensures:
- Better security (secrets stay in Azure)
- Flexibility (update settings without redeployment)
- Environment independence (same pipeline, different configs)

Happy deploying! 🚀
