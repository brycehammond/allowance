# GitHub Actions Deployment Guide

This guide explains how to deploy the Allowance Tracker application to Azure using **GitHub Actions** with a modern multi-workflow architecture:
- **.NET API** → Azure App Service (Linux)
- **Azure Function** → Azure Function App (Weekly Allowance Processing)
- **React Frontend** → Azure Storage Static Website
- **iOS App** → Built and tested on macOS runners

## Architecture Overview

```
┌─────────────────────────────────────────────┐
│       GitHub Actions Workflows              │
│  ┌──────────┐ ┌──────────┐ ┌──────┐ ┌─────┐│
│  │API Build │ │Web Build │ │ iOS  ││Azure││
│  │  (.NET)  │ │ (React)  │ │Build ││Func ││
│  └─────┬────┘ └─────┬────┘ └───┬──┘└──┬──┘│
└────────┼────────────┼──────────┼───────┼───┘
         │            │          │       │
         ▼            ▼          ▼       ▼
┌─────────────┐ ┌──────────┐ [iOS]  ┌────────┐
│App Service  │ │ Storage  │        │Function│
│   (API)     │ │ (React)  │        │  App   │
└──────┬──────┘ └──────────┘        └────┬───┘
       │                                  │
       └────────────┬─────────────────────┘
                    ▼
           ┌────────────────┐
           │  Azure SQL     │
           └────────────────┘
```

## Prerequisites

- GitHub repository
- Azure Subscription
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

## 2. GitHub Setup

### 2.1 Create Azure Service Principal

GitHub Actions needs credentials to deploy to Azure. Create a service principal:

```bash
# Get your subscription ID
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

# Create service principal with Contributor role on resource group
az ad sp create-for-rbac \
  --name "github-actions-allowancetracker" \
  --role contributor \
  --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/allowancetracker-rg \
  --sdk-auth
```

**Copy the entire JSON output** - you'll need it for GitHub Secrets.

Example output:
```json
{
  "clientId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "clientSecret": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
  "subscriptionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "tenantId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  ...
}
```

### 2.2 Configure GitHub Secrets

In your GitHub repository, go to **Settings** → **Secrets and variables** → **Actions** → **New repository secret**.

Add these secrets:

| Secret Name | Value | Description |
|------------|-------|-------------|
| `AZURE_CREDENTIALS` | *JSON from step 2.1* | Azure service principal credentials |
| `AZURE_SQL_CONNECTION_STRING` | *From step 1.2* | Database connection string |
| `JWT_SECRET_KEY` | *Generate 48-char key* | JWT signing key (see below) |
| `APPLICATION_INSIGHTS_CONNECTION_STRING` | *From step 1.5* | App Insights connection |

### 2.3 Configure GitHub Variables

Go to **Settings** → **Secrets and variables** → **Actions** → **Variables** → **New repository variable**.

Add these variables:

| Variable Name | Value |
|--------------|-------|
| `AZURE_WEBAPP_NAME` | `allowancetracker-api` |
| `AZURE_FUNCTION_APP_NAME` | `allowancetracker-function` |
| `AZURE_STORAGE_ACCOUNT` | `allowancetrackerweb` |
| `AZURE_RESOURCE_GROUP` | `allowancetracker-rg` |

### 2.4 Generate Secure JWT Key

**Bash/Linux/macOS:**
```bash
openssl rand -base64 48
```

**PowerShell:**
```powershell
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 48 | ForEach-Object {[char]$_})
```

---

## 3. GitHub Actions Workflows Explained

The project has three separate workflows that run independently:

### Workflow 1: `api.yml` - API CI/CD

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Only when files in `src/**` change

**Jobs:**
1. **build-and-test**
   - Restores .NET dependencies
   - Builds solution in Release mode
   - Runs all 213 tests with code coverage
   - Uploads test results and coverage artifacts

2. **code-quality**
   - Checks code formatting (`dotnet format`)
   - Builds with warnings as errors
   - Runs in parallel with build-and-test

3. **build-docker**
   - Builds Docker image for API
   - Only runs on push to `main`
   - Uses GitHub Actions cache for faster builds

**Environment:**
- Runs on: `ubuntu-latest`
- .NET version: 8.0.x

### Workflow 2: `web.yml` - React CI/CD

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Only when files in `web/**` change

**Jobs:**
1. **build-and-test**
   - Installs Node.js dependencies
   - Runs ESLint for code quality
   - Runs TypeScript type checking
   - Builds React app with Vite
   - Uploads build artifact

2. **lighthouse**
   - Downloads build artifact
   - Runs Lighthouse CI for performance testing
   - Only runs on pull requests
   - Provides performance feedback in PR

**Environment:**
- Runs on: `ubuntu-latest`
- Node.js version: 20.x

### Workflow 3: `ios.yml` - iOS CI/CD

**Triggers:**
- Push to `main` or `develop` branches
- Pull requests to `main` or `develop`
- Only when files in `ios/**` change

**Jobs:**
1. **build-and-test**
   - Selects Xcode 15.2
   - Installs Swift Package Manager dependencies
   - Builds iOS app for simulator
   - Runs tests on iPhone 15 simulator
   - Uploads test results

2. **swiftlint**
   - Installs SwiftLint via Homebrew
   - Runs SwiftLint for code quality
   - Runs in parallel with build-and-test

**Environment:**
- Runs on: `macos-14`
- Xcode version: 15.2

---

## 4. Workflow Benefits

### Efficiency
- **Path filtering**: Only relevant workflows run for each change
- **Parallel execution**: Jobs within each workflow run concurrently
- **Faster feedback**: Developers get results for only what changed

### Independence
- API changes don't trigger React builds
- React changes don't trigger API tests
- iOS changes are completely isolated

### Resource Optimization
- Less GitHub Actions minutes consumed
- Faster CI/CD pipeline overall
- More focused error messages

---

## 5. First Deployment

### 5.1 Push to GitHub

```bash
# Commit and push to main branch
git add .
git commit -m "Configure GitHub Actions workflows"
git push origin main
```

### 5.2 Monitor Workflows

1. Go to your GitHub repository
2. Click on **Actions** tab
3. Watch workflows execute in real-time
4. Each workflow shows detailed logs

### 5.3 Verify Deployment

**Check API:**
```bash
# Test health endpoint
curl https://allowancetracker-api.azurewebsites.net/api/health

# Open Swagger docs
open https://allowancetracker-api.azurewebsites.net/swagger
```

**Check Function:**
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

**Check Frontend:**
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

## 6. Common Workflows

### Development Workflow

1. **Create feature branch:**
   ```bash
   git checkout -b feature/new-feature
   ```

2. **Make changes to API:**
   ```bash
   # Edit files in src/
   git add src/
   git commit -m "Add new API feature"
   git push origin feature/new-feature
   ```

3. **Only API workflow runs** - Web and iOS workflows are skipped

4. **Create PR:**
   - GitHub Actions runs on PR
   - Code review and merge

### Multi-Component Changes

If you change files in multiple directories:

```bash
# Change API and React
git add src/ web/
git commit -m "Add feature across API and web"
git push
```

**Both API and Web workflows run** - iOS workflow is skipped

---

## 7. Extending Workflows

### Add Deployment Step to API Workflow

Edit `.github/workflows/api.yml`:

```yaml
jobs:
  # ... existing jobs ...

  deploy:
    name: Deploy to Azure
    runs-on: ubuntu-latest
    needs: build-and-test
    if: github.ref == 'refs/heads/main'

    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: api-build

      - name: Login to Azure
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ vars.AZURE_WEBAPP_NAME }}
          package: .
```

### Add Deployment Step to Web Workflow

Edit `.github/workflows/web.yml`:

```yaml
jobs:
  # ... existing jobs ...

  deploy:
    name: Deploy to Azure Storage
    runs-on: ubuntu-latest
    needs: build-and-test
    if: github.ref == 'refs/heads/main'

    steps:
      - name: Download artifact
        uses: actions/download-artifact@v4
        with:
          name: react-build
          path: ./dist

      - name: Login to Azure
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Upload to Azure Storage
        run: |
          az storage blob upload-batch \
            --account-name ${{ vars.AZURE_STORAGE_ACCOUNT }} \
            --destination '$web' \
            --source ./dist \
            --overwrite
```

---

## 8. Cost Optimization

### Monthly Cost Estimates

**Azure Resources:**
- App Service B1 (API): ~$13/month
- Function App (Consumption): **~$0/month** (free tier)
- SQL Database S0: ~$15/month
- Storage (React + Function): ~$1/month
- Application Insights: ~$2/month
- **Total: ~$31/month** 💰

**GitHub Actions:**
- Public repositories: **Free unlimited**
- Private repositories: 2,000 minutes/month free (Linux), 1,000 minutes/month (macOS)
- Additional minutes: $0.008/minute (Linux), $0.08/minute (macOS)

**Savings with Separate Workflows:**
- Only run necessary builds
- Reduced minutes consumption
- Faster feedback = more productivity

---

## 9. Monitoring & Debugging

### View Workflow Runs

```bash
# Install GitHub CLI
brew install gh

# List recent workflow runs
gh run list

# View specific run
gh run view <run-id>

# View logs
gh run view <run-id> --log
```

### Common Issues

**1. Authentication Failed**
```
Error: Login failed with Error: Azure login failed
```
**Fix:** Verify `AZURE_CREDENTIALS` secret is correct

**2. Path Filter Not Working**
```
Workflow runs for all changes
```
**Fix:** Check that `paths` in workflow trigger matches your directory structure

**3. Docker Build Failed**
```
Error: Cannot find Dockerfile
```
**Fix:** Ensure Dockerfile is in repository root

**4. iOS Build Failed - Xcode Version**
```
Error: Xcode 15.2 not found
```
**Fix:** Update `XCODE_VERSION` in `.github/workflows/ios.yml` to available version on `macos-14`

---

## 10. Security Best Practices

### Secrets Management
- Never commit secrets to repository
- Rotate secrets every 90 days
- Use minimal scope for service principal
- Enable secret scanning on GitHub

### Service Principal Permissions
```bash
# List current permissions
az role assignment list \
  --assignee <client-id> \
  --output table

# Remove unnecessary permissions
az role assignment delete \
  --assignee <client-id> \
  --role Contributor \
  --scope /subscriptions/<subscription-id>
```

### Branch Protection
1. Go to **Settings** → **Branches**
2. Add branch protection rule for `main`:
   - Require pull request reviews
   - Require status checks to pass (all workflows)
   - Require branches to be up to date
   - Enforce for administrators

---

## 11. Troubleshooting

### Workflow Not Triggering

**Check path filters:**
```yaml
on:
  push:
    paths:
      - 'src/**'  # Make sure this matches your directory
```

**Test locally:**
```bash
# Install act (GitHub Actions locally)
brew install act

# Run workflow locally
act -W .github/workflows/api.yml
```

### Test Failures

```bash
# Run tests locally
cd src/AllowanceTracker.Tests
dotnet test --logger "console;verbosity=detailed"
```

### Deployment Failures

**Check Azure logs:**
```bash
# API logs
az webapp log tail --name allowancetracker-api --resource-group allowancetracker-rg

# Function logs
az functionapp log tail --name allowancetracker-function --resource-group allowancetracker-rg
```

---

## 12. Quick Reference

### Useful Commands

```bash
# View all Azure resources
az resource list --resource-group allowancetracker-rg --output table

# Restart API
az webapp restart --name allowancetracker-api --resource-group allowancetracker-rg

# Restart Function
az functionapp restart --name allowancetracker-function --resource-group allowancetracker-rg

# View GitHub Actions runs
gh run list --workflow=api.yml

# View workflow file
cat .github/workflows/api.yml

# Trigger workflow manually
gh workflow run api.yml

# Delete everything (CAREFUL!)
az group delete --name allowancetracker-rg --yes --no-wait
```

### Workflow URLs

- **API Workflow**: `https://github.com/<owner>/<repo>/actions/workflows/api.yml`
- **Web Workflow**: `https://github.com/<owner>/<repo>/actions/workflows/web.yml`
- **iOS Workflow**: `https://github.com/<owner>/<repo>/actions/workflows/ios.yml`

---

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/actions)
- [Azure CLI Documentation](https://docs.microsoft.com/cli/azure/)
- [Azure Web Apps Deployment](https://docs.microsoft.com/azure/app-service/deploy-github-actions)
- [GitHub Actions for Azure](https://github.com/Azure/actions)

---

**Built with GitHub Actions** 🚀
**Three Efficient Workflows** ⚡
**Modern CI/CD** 🎯
