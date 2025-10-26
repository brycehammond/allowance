#!/bin/bash

# Create Azure Service Principal for GitHub Actions
# This script creates a service principal with the necessary permissions
# to deploy to Azure resources via GitHub Actions

set -e  # Exit on error

echo "=========================================="
echo "Azure Service Principal Setup"
echo "=========================================="
echo ""

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo "Error: Azure CLI is not installed."
    echo "Please install it from: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

# Check if user is logged in
echo "Checking Azure login status..."
if ! az account show &> /dev/null; then
    echo "You are not logged in to Azure. Logging in now..."
    az login
fi

# Get current subscription
SUBSCRIPTION_ID=$(az account show --query id --output tsv)
SUBSCRIPTION_NAME=$(az account show --query name --output tsv)

echo ""
echo "Current Azure Subscription:"
echo "  Name: $SUBSCRIPTION_NAME"
echo "  ID: $SUBSCRIPTION_ID"
echo ""

# Prompt for resource group name
read -p "Enter your resource group name [allowancetracker-rg]: " RESOURCE_GROUP
RESOURCE_GROUP=${RESOURCE_GROUP:-allowancetracker-rg}

# Check if resource group exists
echo ""
echo "Checking if resource group '$RESOURCE_GROUP' exists..."
if az group show --name "$RESOURCE_GROUP" &> /dev/null; then
    echo "✓ Resource group '$RESOURCE_GROUP' found"
else
    echo "⚠ Resource group '$RESOURCE_GROUP' does not exist yet"
    read -p "Do you want to create it now? (y/n): " CREATE_RG
    if [[ "$CREATE_RG" =~ ^[Yy]$ ]]; then
        read -p "Enter location [eastus]: " LOCATION
        LOCATION=${LOCATION:-eastus}
        echo "Creating resource group '$RESOURCE_GROUP' in '$LOCATION'..."
        az group create --name "$RESOURCE_GROUP" --location "$LOCATION"
        echo "✓ Resource group created"
    else
        echo "Note: The service principal will be created, but deployments will fail"
        echo "      until you create the resource group."
    fi
fi

# Prompt for service principal name
read -p "Enter service principal name [allowancetracker-github-actions]: " SP_NAME
SP_NAME=${SP_NAME:-allowancetracker-github-actions}

echo ""
echo "=========================================="
echo "Creating Service Principal"
echo "=========================================="
echo "  Name: $SP_NAME"
echo "  Role: Contributor"
echo "  Scope: /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP"
echo ""

# Create service principal
echo "Creating service principal (this may take a moment)..."
CREDENTIALS=$(az ad sp create-for-rbac \
    --name "$SP_NAME" \
    --role contributor \
    --scopes /subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP \
    --sdk-auth)

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Service principal created successfully!"
    echo ""
    echo "=========================================="
    echo "GitHub Secret: AZURE_CREDENTIALS"
    echo "=========================================="
    echo ""
    echo "Copy the JSON below and add it as a GitHub secret named 'AZURE_CREDENTIALS':"
    echo ""
    echo "---BEGIN AZURE_CREDENTIALS---"
    echo "$CREDENTIALS"
    echo "---END AZURE_CREDENTIALS---"
    echo ""
    echo "=========================================="
    echo "Instructions"
    echo "=========================================="
    echo ""
    echo "1. Go to your GitHub repository"
    echo "2. Navigate to: Settings → Secrets and variables → Actions"
    echo "3. Click 'New repository secret'"
    echo "4. Name: AZURE_CREDENTIALS"
    echo "5. Value: Paste the JSON above (including the curly braces)"
    echo "6. Click 'Add secret'"
    echo ""
    echo "Additional secrets needed:"
    echo "  - VITE_API_URL"
    echo "  - AZURE_WEBAPP_NAME"
    echo "  - AZURE_FUNCTIONAPP_NAME"
    echo "  - AZURE_STORAGE_ACCOUNT"
    echo "  - AZURE_RESOURCE_GROUP"
    echo "  - AZURE_CDN_PROFILE (optional)"
    echo "  - AZURE_CDN_ENDPOINT (optional)"
    echo ""
    echo "See docs/ci-cd/github-secrets-azure.md for details on each secret."
    echo ""

    # Save to file
    CREDENTIALS_FILE="azure-credentials-$(date +%Y%m%d-%H%M%S).json"
    echo "$CREDENTIALS" > "$CREDENTIALS_FILE"
    echo "Credentials also saved to: $CREDENTIALS_FILE"
    echo "⚠ IMPORTANT: Keep this file secure and delete it after adding to GitHub!"
    echo ""
else
    echo ""
    echo "✗ Failed to create service principal"
    echo "Please check the error messages above and try again."
    exit 1
fi

echo "=========================================="
echo "Verification"
echo "=========================================="
echo ""
echo "To verify the service principal was created:"
echo "  az ad sp list --display-name \"$SP_NAME\" --output table"
echo ""
echo "To delete the service principal (if needed):"
echo "  APP_ID=\$(az ad sp list --display-name \"$SP_NAME\" --query '[0].appId' -o tsv)"
echo "  az ad sp delete --id \$APP_ID"
echo ""
echo "Done!"
