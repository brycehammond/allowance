# CI/CD Documentation

This directory contains all documentation and scripts for deploying the Allowance Tracker application to Azure using GitHub Actions.

## Quick Links

- **[One-Liner Commands](./one-liner-commands.md)** ⚡ - Fastest way: single commands
- **[Azure Quick Start Guide](./azure-quick-start.md)** 🚀 - Copy-paste commands to get deployed fast
- **[GitHub Secrets Reference](./github-secrets-azure.md)** 🔑 - All required secrets with examples
- **[Azure Deployment Setup](./azure-deployment-setup.md)** 📚 - Comprehensive deployment guide
- **[Service Principal Script](./create-service-principal.sh)** 🤖 - Interactive service principal creation

## Getting Started

### 1. Create Azure Resources

Follow the **[Azure Quick Start Guide](./azure-quick-start.md)** to create all necessary Azure resources with copy-paste commands.

```bash
# Estimated time: 10-15 minutes
# Resources created:
# - Resource Group
# - App Service Plan
# - App Service (API)
# - SQL Database
# - Function App
# - Storage Account (for React app)
# - CDN (optional)
```

### 2. Create Service Principal

Run the interactive script to create the service principal for GitHub Actions:

```bash
cd docs/ci-cd
./create-service-principal.sh
```

Or use the manual Azure CLI command from the [GitHub Secrets Reference](./github-secrets-azure.md).

### 3. Configure GitHub Secrets

Add these **8 secrets** to your GitHub repository (Settings → Secrets and variables → Actions):

| Secret | Required | Description |
|--------|----------|-------------|
| `AZURE_CREDENTIALS` | ✅ | Service principal JSON |
| `VITE_API_URL` | ✅ | Production API URL |
| `AZURE_WEBAPP_NAME` | ✅ | App Service name |
| `AZURE_FUNCTIONAPP_NAME` | ✅ | Function App name |
| `AZURE_STORAGE_ACCOUNT` | ✅ | Storage account name |
| `AZURE_RESOURCE_GROUP` | ✅ | Resource group name |
| `AZURE_CDN_PROFILE` | Optional | CDN profile name |
| `AZURE_CDN_ENDPOINT` | Optional | CDN endpoint name |

See **[GitHub Secrets Reference](./github-secrets-azure.md)** for how to get each value.

### 4. Deploy

Push to `main` branch or manually trigger workflows from GitHub Actions tab.

```bash
git add .
git commit -m "Configure Azure deployment"
git push origin main
```

## GitHub Actions Workflows

The repository includes three deployment workflows:

### API Deployment
- **File**: `.github/workflows/deploy-api.yml`
- **Triggers**: Push to `main` with changes to `src/**`
- **Deploys**: .NET API to Azure App Service
- **Steps**: Build → Test → Publish → Deploy → Health Check

### Function Deployment
- **File**: `.github/workflows/deploy-function.yml`
- **Triggers**: Push to `main` with changes to `src/AllowanceTracker.Functions/**`
- **Deploys**: Azure Function (weekly allowance job)
- **Steps**: Build → Test → Publish → Deploy

### Web Deployment
- **File**: `.github/workflows/deploy-web.yml`
- **Triggers**: Push to `main` with changes to `web/**`
- **Deploys**: React app to Azure Storage Static Website
- **Steps**: Build → Lint → Type Check → Deploy → Purge CDN

## Files in this Directory

### Documentation
- **[README.md](./README.md)** - This file
- **[azure-quick-start.md](./azure-quick-start.md)** - Fast deployment guide
- **[azure-deployment-setup.md](./azure-deployment-setup.md)** - Comprehensive setup guide
- **[github-secrets-azure.md](./github-secrets-azure.md)** - Secrets reference

### Scripts
- **[create-service-principal.sh](./create-service-principal.sh)** - Interactive script to create Azure service principal

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                     GitHub Actions                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────────┐  │
│  │ API CI   │  │ Func CI  │  │ Web CI               │  │
│  └────┬─────┘  └────┬─────┘  └────┬─────────────────┘  │
└───────┼─────────────┼─────────────┼─────────────────────┘
        │             │             │
        ▼             ▼             ▼
┌─────────────────────────────────────────────────────────┐
│                   Azure Cloud                            │
│                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │ App Service  │  │  Function    │  │   Storage    │  │
│  │ .NET API     │  │  Weekly Job  │  │  React App   │  │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  │
│         │                  │                  │          │
│         └─────────┬────────┴──────────────────┘          │
│                   ▼                                      │
│            ┌──────────────┐                              │
│            │  SQL Server  │                              │
│            │   Database   │                              │
│            └──────────────┘                              │
│                                                          │
│  Optional: Azure CDN for global distribution            │
└─────────────────────────────────────────────────────────┘
```

## Deployment Flow

### On Push to `main`:

1. **GitHub Actions** triggers appropriate workflow based on changed files
2. **Build job** compiles and tests the application
3. **Upload artifact** saves build output
4. **Deploy job** downloads artifact and authenticates to Azure
5. **Azure CLI/Actions** deploy to respective Azure services
6. **Verification** confirms successful deployment

### Manual Deployment:

1. Go to **GitHub → Actions** tab
2. Select workflow (e.g., "Deploy API to Azure App Service")
3. Click **"Run workflow"**
4. Select branch: `main`
5. Click **"Run workflow"**

## Monitoring

### View Logs

```bash
# API logs
az webapp log tail --name allowancetracker-api --resource-group allowancetracker-rg

# Function logs
az functionapp log tail --name allowancetracker-func --resource-group allowancetracker-rg
```

### Check Deployment Status

- **GitHub Actions**: https://github.com/YOUR_USERNAME/YOUR_REPO/actions
- **Azure Portal**: https://portal.azure.com

## Troubleshooting

### Common Issues

**GitHub Actions fails with "Login failed"**
```bash
# Solution: Verify AZURE_CREDENTIALS secret
# Recreate service principal:
cd docs/ci-cd
./create-service-principal.sh
```

**API returns 500 error**
```bash
# Check connection string
az webapp config connection-string list \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg

# View live logs
az webapp log tail \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg
```

**CORS errors in React app**
```bash
# Add CORS origin
az webapp cors add \
  --name allowancetracker-api \
  --resource-group allowancetracker-rg \
  --allowed-origins "https://your-cdn-url.azureedge.net"
```

See the **[Azure Quick Start Guide](./azure-quick-start.md#troubleshooting)** for more solutions.

## Cost Management

### Current Configuration Cost
- App Service Plan (B1): ~$13/month
- Azure Function (Consumption): ~$0-5/month
- Azure SQL (S0): ~$15/month
- Azure Storage: ~$1/month
- Azure CDN: ~$5-10/month

**Total: ~$34-44/month**

### Cost Optimization Tips

**Development/Testing:**
- Use Free tier App Service Plan
- Use Serverless SQL database
- Skip CDN (use Storage URL directly)

**Production:**
- Consider Reserved Instances for App Service (30-40% savings)
- Use Azure Hybrid Benefit if you have licenses
- Enable auto-pause for SQL database during non-peak hours

## Security

### Best Practices

✅ **Service Principal** scoped to resource group only
✅ **Secrets** stored in GitHub (encrypted at rest)
✅ **HTTPS only** enforced on all services
✅ **Managed Identity** recommended for production
✅ **Connection strings** stored in Azure App Settings (encrypted)

### Rotate Credentials

```bash
# Rotate service principal credentials annually
az ad sp credential reset \
  --name allowancetracker-github-actions \
  --sdk-auth
```

Update `AZURE_CREDENTIALS` secret in GitHub with new JSON.

## Support

### Documentation
- [Azure App Service](https://docs.microsoft.com/azure/app-service/)
- [Azure Functions](https://docs.microsoft.com/azure/azure-functions/)
- [Azure Storage](https://docs.microsoft.com/azure/storage/)
- [GitHub Actions for Azure](https://github.com/Azure/actions)

### Quick Commands

```bash
# List all resources
az resource list --resource-group allowancetracker-rg --output table

# Restart API
az webapp restart --name allowancetracker-api --resource-group allowancetracker-rg

# Restart Function
az functionapp restart --name allowancetracker-func --resource-group allowancetracker-rg

# Get API URL
az webapp show --name allowancetracker-api --resource-group allowancetracker-rg \
  --query defaultHostName --output tsv
```

## Next Steps

After successful deployment:

- [ ] Configure custom domain
- [ ] Enable Application Insights monitoring
- [ ] Set up automated backups
- [ ] Configure staging slots for zero-downtime deployments
- [ ] Set up Azure Monitor alerts
- [ ] Review security recommendations in Azure Security Center

## Contributing

When updating deployment configuration:

1. Test changes in a separate resource group first
2. Update documentation in this directory
3. Create PR with deployment changes
4. Verify workflows pass in PR before merging
5. Monitor first deployment to production

## License

This deployment configuration is part of the Allowance Tracker project.
See the main [LICENSE](../../LICENSE) file for details.
