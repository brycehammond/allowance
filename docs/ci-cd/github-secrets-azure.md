# GitHub Secrets - Azure Deployment Quick Reference

This document provides a quick reference for setting up GitHub secrets required for Azure deployment.

## Secrets Summary

| Secret Name | Required | Used By | Description |
|-------------|----------|---------|-------------|
| `AZURE_CREDENTIALS` | ✅ Yes | All workflows | Service principal for Azure authentication |
| `VITE_API_URL` | ✅ Yes | Web deployment | Production API URL for React app |
| `AZURE_WEBAPP_NAME` | ✅ Yes | API deployment | Name of Azure App Service |
| `AZURE_FUNCTIONAPP_NAME` | ✅ Yes | Function deployment | Name of Azure Function App |
| `AZURE_STORAGE_ACCOUNT` | ✅ Yes | Web deployment | Name of Storage account for React app |
| `AZURE_RESOURCE_GROUP` | ✅ Yes | Web deployment | Name of Azure resource group |
| `AZURE_CDN_PROFILE` | ⚠️ Optional | Web deployment | Name of CDN profile (if using CDN) |
| `AZURE_CDN_ENDPOINT` | ⚠️ Optional | Web deployment | Name of CDN endpoint (if using CDN) |

---

## Required Secrets

### 1. AZURE_CREDENTIALS

**Description**: Service principal credentials for authenticating to Azure

**How to Create**:

**Option A: Using the provided script (recommended)**

```bash
# Run the interactive script
cd docs/ci-cd
./create-service-principal.sh
```

The script will:
- Check if you're logged in to Azure
- Prompt for resource group name
- Create the service principal
- Display the JSON credentials to copy
- Save credentials to a timestamped file

**Option B: Manual command**

```bash
# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

# Set your resource group name
RESOURCE_GROUP="allowancetracker-rg"

# Create service principal
az ad sp create-for-rbac \
  --name "allowancetracker-github-actions" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
  --sdk-auth
```

**Example Value** (copy the entire JSON output):

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

**Where to Add**: GitHub → Settings → Secrets and variables → Actions → New repository secret

---

### 2. VITE_API_URL

**Description**: Production API URL used by the React application

**How to Determine**:

```bash
# After creating your Azure App Service, the URL is:
# https://<your-app-service-name>.azurewebsites.net

# Get it programmatically:
az webapp show \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --query defaultHostName \
  --output tsv
```

**Example Value**:

```
https://allowancetracker-api.azurewebsites.net
```

**Where to Add**: GitHub → Settings → Secrets and variables → Actions → New repository secret

---

### 3. AZURE_WEBAPP_NAME

**Description**: Name of your Azure App Service (API)

**How to Determine**:

```bash
# Get your App Service name
az webapp list --resource-group allowancetracker-rg --query "[].name" --output tsv
```

**Example Value**:

```
allowancetracker-api
```

**Where to Add**: GitHub → Settings → Secrets and variables → Actions → New repository secret

---

### 4. AZURE_FUNCTIONAPP_NAME

**Description**: Name of your Azure Function App

**How to Determine**:

```bash
# Get your Function App name
az functionapp list --resource-group allowancetracker-rg --query "[].name" --output tsv
```

**Example Value**:

```
allowancetracker-func
```

**Where to Add**: GitHub → Settings → Secrets and variables → Actions → New repository secret

---

### 5. AZURE_STORAGE_ACCOUNT

**Description**: Name of your Azure Storage account for the React app

**How to Determine**:

```bash
# Get your Storage account name (look for the one with static website enabled)
az storage account list --resource-group allowancetracker-rg --query "[].name" --output tsv
```

**Example Value**:

```
allowancetrackerapp
```

**Where to Add**: GitHub → Settings → Secrets and variables → Actions → New repository secret

---

### 6. AZURE_RESOURCE_GROUP

**Description**: Name of your Azure resource group

**How to Determine**:

```bash
# List all your resource groups
az group list --query "[].name" --output tsv
```

**Example Value**:

```
allowancetracker-rg
```

**Where to Add**: GitHub → Settings → Secrets and variables → Actions → New repository secret

---

### 7. AZURE_CDN_PROFILE

**Description**: Name of your Azure CDN profile (if using CDN)

**How to Determine**:

```bash
# Get your CDN profile name
az cdn profile list --resource-group allowancetracker-rg --query "[].name" --output tsv
```

**Example Value**:

```
allowancetracker-cdn
```

**Where to Add**: GitHub → Settings → Secrets and variables → Actions → New repository secret

**Note**: This is optional. If not using CDN, the CDN purge step will be skipped automatically.

---

### 8. AZURE_CDN_ENDPOINT

**Description**: Name of your Azure CDN endpoint (if using CDN)

**How to Determine**:

```bash
# Get your CDN endpoint name
az cdn endpoint list --resource-group allowancetracker-rg --profile-name allowancetracker-cdn --query "[].name" --output tsv
```

**Example Value**:

```
allowancetracker
```

**Where to Add**: GitHub → Settings → Secrets and variables → Actions → New repository secret

**Note**: This is optional. If not using CDN, the CDN purge step will be skipped automatically.

---

## Step-by-Step: Adding Secrets to GitHub

1. **Navigate to your repository on GitHub**

2. **Go to Settings**
   - Click the "Settings" tab at the top of the repository

3. **Access Secrets**
   - In the left sidebar, expand "Secrets and variables"
   - Click "Actions"

4. **Add a New Secret**
   - Click "New repository secret"
   - Enter the secret name (e.g., `AZURE_CREDENTIALS`)
   - Paste the secret value
   - Click "Add secret"

5. **Repeat for each secret**

---

## Verification Checklist

Before triggering a deployment, verify you have:

- [ ] Created Azure resources (App Service, Function App, Storage)
- [ ] Created service principal with contributor access
- [ ] Added `AZURE_CREDENTIALS` secret to GitHub
- [ ] Added `VITE_API_URL` secret to GitHub
- [ ] Added `AZURE_WEBAPP_NAME` secret to GitHub
- [ ] Added `AZURE_FUNCTIONAPP_NAME` secret to GitHub
- [ ] Added `AZURE_STORAGE_ACCOUNT` secret to GitHub
- [ ] Added `AZURE_RESOURCE_GROUP` secret to GitHub
- [ ] Added `AZURE_CDN_PROFILE` secret to GitHub (if using CDN)
- [ ] Added `AZURE_CDN_ENDPOINT` secret to GitHub (if using CDN)
- [ ] Configured App Service connection strings and app settings
- [ ] Configured Function App connection strings and app settings
- [ ] Enabled static website hosting on Storage account

---

## Testing Your Secrets

You can test if your secrets are working by triggering a workflow manually:

1. Go to **Actions** tab in your GitHub repository
2. Select one of the deployment workflows (e.g., "Deploy API to Azure App Service")
3. Click **Run workflow**
4. Select the `main` branch
5. Click **Run workflow**

If the workflow succeeds, your secrets are configured correctly!

---

## Security Best Practices

### Secret Rotation

- **Service Principal**: Rotate every 12 months
  ```bash
  # Create a new credential for existing service principal
  az ad sp credential reset \
    --name allowancetracker-github-actions \
    --sdk-auth
  ```
  Update `AZURE_CREDENTIALS` secret with new JSON

- **API URL**: Only changes if you rename/recreate App Service

### Least Privilege

The service principal is scoped to only your resource group. This means:

✅ Can deploy to resources in `allowancetracker-rg`
❌ Cannot access other resource groups
❌ Cannot create new resource groups
❌ Cannot modify subscription settings

### Secret Management

- Never commit secrets to source control
- Use GitHub Environments for additional approval requirements
- Enable branch protection to require reviews before merging to `main`
- Audit secret access regularly

---

## Troubleshooting

### Error: "Azure login failed"

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
Update the secret in GitHub

---

### Error: "Resource not found"

**Cause**: Resource names in workflow don't match actual Azure resources

**Solution**:
1. Verify resource names in Azure Portal
2. Update workflow environment variables or create optional secrets

---

### Error: "Insufficient permissions"

**Cause**: Service principal doesn't have correct role

**Solution**:
```bash
# Grant contributor access
az role assignment create \
  --assignee <CLIENT_ID_FROM_CREDENTIALS> \
  --role Contributor \
  --scope /subscriptions/<SUBSCRIPTION_ID>/resourceGroups/allowancetracker-rg
```

---

## Quick Command Reference

```bash
# Get your subscription ID
az account show --query id --output tsv

# List all resource groups
az group list --output table

# List all App Services
az webapp list --resource-group allowancetracker-rg --output table

# List all Function Apps
az functionapp list --resource-group allowancetracker-rg --output table

# List all Storage accounts
az storage account list --resource-group allowancetracker-rg --output table

# Get App Service URL
az webapp show \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --query defaultHostName \
  --output tsv
```

---

## Next Steps

After configuring secrets:

1. Review the [Azure Deployment Setup Guide](./azure-deployment-setup.md)
2. Trigger a test deployment
3. Verify each service is running
4. Configure Application Insights for monitoring
5. Set up custom domains (optional)
