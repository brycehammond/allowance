# GitHub Actions Secrets & Environment Variables Setup

This guide explains all secrets and environment variables needed for GitHub Actions CI/CD workflows.

## 📋 Quick Summary

**For CI/CD (Build & Test)**: ✅ No secrets required!

**For Deployment (Optional)**: Only if deploying to Azure

## Current Setup (CI/CD Only)

### ✅ What Works Now (No Secrets Required)

The current GitHub Actions workflows perform:
- ✅ Building .NET API
- ✅ Building Azure Functions
- ✅ Building React frontend
- ✅ Running all tests
- ✅ Code quality checks
- ✅ Security scanning
- ✅ Publishing build artifacts

**All of these work without any secrets configured!**

### 🔧 Environment Variables (Hardcoded in Workflows)

These are already configured in the workflow files:

| Variable | Value | Location | Purpose |
|----------|-------|----------|---------|
| `DOTNET_VERSION` | `8.0.x` | All .NET workflows | .NET SDK version |
| `NODE_VERSION` | `20.x` | All Node workflows | Node.js version |
| `XCODE_VERSION` | `15.2` | iOS workflow | Xcode version |

**No action needed** - these are in the workflow YAML files.

---

## Optional: React Build-Time Configuration

### VITE_API_URL (Optional)

**Current Setup**:
```yaml
# In ci.yml and web.yml
env:
  VITE_API_URL: ${{ secrets.VITE_API_URL || 'https://api.example.com' }}
```

**What This Does**:
- If `VITE_API_URL` secret is set, uses that value
- Otherwise, uses `https://api.example.com` as fallback
- This is the API URL that gets embedded in the React build

**Should You Set This?**

| Scenario | Should Set? | Value |
|----------|-------------|-------|
| Just testing CI/CD | ❌ No | Default works fine |
| Have production API | ✅ Yes | Your actual API URL |
| Developing locally | ❌ No | Use `.env.development` instead |

**How to Set (Optional)**:

1. Go to GitHub repository → **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret**
3. Name: `VITE_API_URL`
4. Value: Your production API URL (e.g., `https://allowancetracker-api.azurewebsites.net`)
5. Click **Add secret**

---

## Optional: Azure Deployment Secrets

**Only needed if you want to deploy to Azure from GitHub Actions**

### Required for Azure Deployment

| Secret Name | Description | How to Get | Required? |
|------------|-------------|------------|-----------|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | API deployment credential | See below ⬇️ | For API deployment |
| `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` | Function deployment credential | See below ⬇️ | For Function deployment |
| `AZURE_STORAGE_CONNECTION_STRING` | Frontend storage credential | See below ⬇️ | For frontend deployment |
| `AZURE_SQL_CONNECTION_STRING` | Database connection (migrations) | See below ⬇️ | For running migrations |

### How to Get Azure Publish Profiles

#### 1. API App Service Publish Profile

**Option 1: Azure Portal**
1. Go to Azure Portal → App Services → Your API app
2. Click **Get publish profile** (top toolbar)
3. Download the `.publishsettings` file
4. Open in text editor and copy entire contents

**Option 2: Azure CLI**
```bash
az webapp deployment list-publishing-profiles \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --xml
```

#### 2. Function App Publish Profile

**Option 1: Azure Portal**
1. Go to Azure Portal → Function App → Your function app
2. Click **Get publish profile** (top toolbar)
3. Download the `.publishsettings` file
4. Open in text editor and copy entire contents

**Option 2: Azure CLI**
```bash
az functionapp deployment list-publishing-profiles \
  --name allowancetracker-func \
  --resource-group allowancetracker-rg \
  --xml
```

#### 3. Storage Account Connection String

**Option 1: Azure Portal**
1. Go to Azure Portal → Storage accounts → Your storage account
2. Click **Access keys** (left menu, under Security + networking)
3. Copy **Connection string** from key1 or key2

**Option 2: Azure CLI**
```bash
az storage account show-connection-string \
  --name allowancetrackerweb \
  --resource-group allowancetracker-rg \
  --query connectionString \
  --output tsv
```

#### 4. SQL Connection String (For Migrations)

**Option 1: Azure Portal**
1. Go to Azure Portal → SQL Databases → Your database
2. Click **Connection strings** (left menu)
3. Copy **ADO.NET** connection string
4. Replace `{your_password}` with actual SQL admin password

**Option 2: Azure CLI**
```bash
az sql db show-connection-string \
  --client ado.net \
  --server allowancetracker-sql \
  --name allowancetracker-db

# Output will have placeholders - replace with actual values
```

**Example:**
```
Server=tcp:allowancetracker-sql.database.windows.net,1433;Initial Catalog=allowancetracker-db;Persist Security Info=False;User ID=sqladmin;Password=YourPassword123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

---

## How to Add Secrets to GitHub

### Step 1: Navigate to Secrets

1. Go to your GitHub repository
2. Click **Settings** (top right)
3. In left sidebar, click **Secrets and variables** → **Actions**

### Step 2: Add Each Secret

For each secret you want to add:

1. Click **New repository secret**
2. Enter **Name** (e.g., `AZURE_WEBAPP_PUBLISH_PROFILE`)
3. Paste **Value** (entire XML content for publish profiles, connection string for others)
4. Click **Add secret**

### Step 3: Verify Secrets

After adding, you should see:
- Secret names listed (values are hidden)
- Green checkmark if added successfully
- You can update but cannot view values again

---

## Environment Variables vs Secrets

### Environment Variables (Public)
- Defined in workflow YAML files
- Visible to everyone
- Used for: SDK versions, build flags
- Example: `DOTNET_VERSION: '8.0.x'`

### Secrets (Private)
- Stored in GitHub repository settings
- Hidden from logs (shows as `***`)
- Used for: credentials, connection strings, API keys
- Example: `${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}`

---

## Creating Deployment Workflows (Optional)

If you want to add Azure deployment, create new workflows:

### Example: API Deployment Workflow

Create `.github/workflows/deploy-api.yml`:

```yaml
name: Deploy API to Azure

on:
  workflow_dispatch:  # Manual trigger
  push:
    branches: [main]
    paths:
      - 'src/AllowanceTracker/**'

env:
  DOTNET_VERSION: '8.0.x'

jobs:
  deploy:
    name: Deploy to Azure App Service
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: dotnet build --configuration Release --no-restore

      - name: Publish API
        run: dotnet publish src/AllowanceTracker/AllowanceTracker.csproj --configuration Release --output ./publish

      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v2
        with:
          app-name: 'allowancetracker-api'
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish

      - name: Run database migrations
        run: |
          dotnet tool install --global dotnet-ef
          dotnet ef database update --project src/AllowanceTracker/AllowanceTracker.csproj --connection "${{ secrets.AZURE_SQL_CONNECTION_STRING }}"
```

**Note**: Only create this if you actually want automated deployment!

---

## Security Best Practices

### ✅ DO:
1. Use repository secrets for all credentials
2. Rotate secrets every 90 days
3. Use different secrets per environment (dev/staging/prod)
4. Review secret access permissions
5. Delete secrets that are no longer needed
6. Use organization secrets for shared credentials

### ❌ DON'T:
1. Commit secrets to source control
2. Echo secrets in workflow logs
3. Store secrets in workflow files
4. Use secrets in pull requests from forks (disabled by default)
5. Share secrets via insecure channels

---

## Troubleshooting

### Secret Not Found

**Error**: `Secret AZURE_WEBAPP_PUBLISH_PROFILE is not set`

**Solution**:
1. Go to Settings → Secrets and variables → Actions
2. Verify secret name matches exactly (case-sensitive)
3. Check secret is in correct repository
4. Re-create secret if corrupted

### Deployment Fails with 401 Unauthorized

**Error**: `Error: Failed to deploy web app. Error: Failed to deploy web app. Error Code: Unauthorized`

**Solution**:
1. Publish profile might be expired
2. Download new publish profile from Azure Portal
3. Update GitHub secret with new profile
4. Retry deployment

### Connection String Invalid

**Error**: `A network-related or instance-specific error occurred while establishing a connection to SQL Server`

**Solution**:
1. Verify connection string format
2. Check SQL Server firewall allows GitHub Actions IPs
3. Add firewall rule: Allow Azure services
4. Test connection string locally first

### React Build Missing API URL

**Error**: `API calls fail with 404 or CORS errors`

**Solution**:
1. Set `VITE_API_URL` secret with production API URL
2. Or update hardcoded value in workflow file
3. Rebuild React app

---

## Current Status: What You Need NOW

### For Current CI/CD Workflows

**✅ No secrets required!**

Your current workflows will work immediately because they only:
- Build code
- Run tests
- Publish artifacts

### If You Want to Deploy to Azure

Add these 4 secrets:
1. ✅ `AZURE_WEBAPP_PUBLISH_PROFILE` - API deployment
2. ✅ `AZURE_FUNCTIONAPP_PUBLISH_PROFILE` - Function deployment
3. ✅ `AZURE_STORAGE_CONNECTION_STRING` - Frontend deployment
4. ✅ `AZURE_SQL_CONNECTION_STRING` - Database migrations

### If You Want Correct API URL in React Builds

Add this 1 secret:
1. ✅ `VITE_API_URL` - Your production API URL

---

## Quick Start Checklist

For most users, you can skip all secrets and just push:

```bash
# No secrets needed - just push and watch it build!
git push origin main

# View builds at:
# https://github.com/YOUR_USERNAME/allowance/actions
```

✅ Builds will succeed
✅ Tests will run
✅ Artifacts will be published
✅ No deployment (manual or separate process)

---

## Need Help?

- **GitHub Secrets Docs**: https://docs.github.com/en/actions/security-guides/encrypted-secrets
- **Azure Publish Profiles**: https://docs.microsoft.com/azure/app-service/deploy-github-actions
- **Troubleshooting Guide**: [docs/github-actions-ci.md](github-actions-ci.md)

---

## Summary

**Current workflows require ZERO secrets** - they build, test, and create artifacts.

**Optional secrets for deployment** - only if you want automated Azure deployment.

Most developers won't need any secrets configured! 🎉
