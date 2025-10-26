# Azure Deployment - One-Liner Commands

Quick commands for creating Azure service principal and getting resource information.

## Create Service Principal (One Command)

```bash
az ad sp create-for-rbac \
  --name "allowancetracker-github-actions" \
  --role contributor \
  --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/allowancetracker-rg \
  --sdk-auth
```

**Output**: Copy the entire JSON and add as `AZURE_CREDENTIALS` secret in GitHub.

---

## Get All Secret Values (One Command)

Run this after creating your Azure resources to get all the values you need for GitHub secrets:

```bash
echo "========================================" && \
echo "GitHub Secrets for Azure Deployment" && \
echo "========================================" && \
echo "" && \
echo "AZURE_CREDENTIALS:" && \
echo "  Run: az ad sp create-for-rbac --name allowancetracker-github-actions --role contributor --scopes /subscriptions/$(az account show --query id -o tsv)/resourceGroups/allowancetracker-rg --sdk-auth" && \
echo "" && \
echo "VITE_API_URL:" && \
az webapp show --name allowancetracker-api --resource-group allowancetracker-rg --query "defaultHostName" -o tsv 2>/dev/null | sed 's/^/  https:\/\//' || echo "  (App Service not created yet)" && \
echo "" && \
echo "AZURE_WEBAPP_NAME:" && \
az webapp list --resource-group allowancetracker-rg --query "[0].name" -o tsv 2>/dev/null | sed 's/^/  /' || echo "  (App Service not created yet)" && \
echo "" && \
echo "AZURE_FUNCTIONAPP_NAME:" && \
az functionapp list --resource-group allowancetracker-rg --query "[0].name" -o tsv 2>/dev/null | sed 's/^/  /' || echo "  (Function App not created yet)" && \
echo "" && \
echo "AZURE_STORAGE_ACCOUNT:" && \
az storage account list --resource-group allowancetracker-rg --query "[?kind=='StorageV2'].name" -o tsv 2>/dev/null | sed 's/^/  /' || echo "  (Storage Account not created yet)" && \
echo "" && \
echo "AZURE_RESOURCE_GROUP:" && \
echo "  allowancetracker-rg" && \
echo "" && \
echo "AZURE_CDN_PROFILE (optional):" && \
az cdn profile list --resource-group allowancetracker-rg --query "[0].name" -o tsv 2>/dev/null | sed 's/^/  /' || echo "  (CDN not created yet or not using CDN)" && \
echo "" && \
echo "AZURE_CDN_ENDPOINT (optional):" && \
az cdn endpoint list --resource-group allowancetracker-rg --profile-name allowancetracker-cdn --query "[0].name" -o tsv 2>/dev/null | sed 's/^/  /' || echo "  (CDN not created yet or not using CDN)" && \
echo "" && \
echo "========================================"
```

---

## Individual Secret Value Commands

### VITE_API_URL
```bash
echo "https://$(az webapp show --name allowancetracker-api --resource-group allowancetracker-rg --query defaultHostName -o tsv)"
```

### AZURE_WEBAPP_NAME
```bash
az webapp list --resource-group allowancetracker-rg --query "[0].name" -o tsv
```

### AZURE_FUNCTIONAPP_NAME
```bash
az functionapp list --resource-group allowancetracker-rg --query "[0].name" -o tsv
```

### AZURE_STORAGE_ACCOUNT
```bash
az storage account list --resource-group allowancetracker-rg --query "[?kind=='StorageV2'].name" -o tsv
```

### AZURE_RESOURCE_GROUP
```bash
echo "allowancetracker-rg"
```

### AZURE_CDN_PROFILE
```bash
az cdn profile list --resource-group allowancetracker-rg --query "[0].name" -o tsv
```

### AZURE_CDN_ENDPOINT
```bash
az cdn endpoint list --resource-group allowancetracker-rg --profile-name allowancetracker-cdn --query "[0].name" -o tsv
```

---

## Quick Resource Creation (All-in-One)

**⚠️ Warning**: This creates ~$35-45/month in Azure resources. Review before running.

```bash
# Set variables
export RESOURCE_GROUP="allowancetracker-rg" && \
export LOCATION="eastus" && \
export APP_SERVICE_PLAN="allowancetracker-plan" && \
export WEB_APP_NAME="allowancetracker-api" && \
export FUNCTION_APP_NAME="allowancetracker-func" && \
export STORAGE_ACCOUNT_FUNC="allowancetrackerfunc" && \
export STORAGE_ACCOUNT_WEB="allowancetrackerapp" && \
export SQL_SERVER_NAME="allowancetracker-sql" && \
export SQL_ADMIN_PASSWORD="YourSecurePassword123!" && \
export DATABASE_NAME="allowancetracker" && \

# Create all resources
az group create --name $RESOURCE_GROUP --location $LOCATION && \
az appservice plan create --name $APP_SERVICE_PLAN --resource-group $RESOURCE_GROUP --sku B1 --is-linux && \
az webapp create --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --plan $APP_SERVICE_PLAN --runtime "DOTNET|8.0" && \
az webapp config set --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --always-on true && \
az webapp update --name $WEB_APP_NAME --resource-group $RESOURCE_GROUP --https-only true && \
az sql server create --name $SQL_SERVER_NAME --resource-group $RESOURCE_GROUP --location $LOCATION --admin-user sqladmin --admin-password $SQL_ADMIN_PASSWORD && \
az sql server firewall-rule create --name AllowAzureServices --resource-group $RESOURCE_GROUP --server $SQL_SERVER_NAME --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0 && \
az sql db create --name $DATABASE_NAME --resource-group $RESOURCE_GROUP --server $SQL_SERVER_NAME --service-objective S0 && \
az storage account create --name $STORAGE_ACCOUNT_FUNC --resource-group $RESOURCE_GROUP --location $LOCATION --sku Standard_LRS && \
az functionapp create --name $FUNCTION_APP_NAME --resource-group $RESOURCE_GROUP --consumption-plan-location $LOCATION --runtime dotnet-isolated --runtime-version 8 --functions-version 4 --storage-account $STORAGE_ACCOUNT_FUNC --os-type Linux && \
az storage account create --name $STORAGE_ACCOUNT_WEB --resource-group $RESOURCE_GROUP --location $LOCATION --sku Standard_LRS --kind StorageV2 && \
az storage blob service-properties update --account-name $STORAGE_ACCOUNT_WEB --static-website --index-document index.html --404-document index.html && \
echo "✅ All resources created successfully!"
```

**Time to complete**: ~5-10 minutes

---

## Verify Deployment

```bash
# Quick health check
curl https://$(az webapp show --name allowancetracker-api --resource-group allowancetracker-rg --query defaultHostName -o tsv)/health
```

---

## Clean Up (Delete Everything)

**⚠️ Warning**: This deletes ALL resources in the resource group. Cannot be undone.

```bash
az group delete --name allowancetracker-rg --yes --no-wait
```

---

## Quick Links

- **Interactive Script**: [create-service-principal.sh](./create-service-principal.sh)
- **Step-by-Step Guide**: [azure-quick-start.md](./azure-quick-start.md)
- **Secrets Reference**: [github-secrets-azure.md](./github-secrets-azure.md)
- **Full Documentation**: [azure-deployment-setup.md](./azure-deployment-setup.md)
