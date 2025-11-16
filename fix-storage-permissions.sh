#!/bin/bash
# Fix Azure Storage permissions for service principal

# Set your variables (update these with your actual values)
STORAGE_ACCOUNT_NAME="your-storage-account-name"  # Get from vars.AZURE_STORAGE_ACCOUNT
RESOURCE_GROUP="your-resource-group"              # Get from vars.AZURE_RESOURCE_GROUP

# Get the service principal App ID from your existing credentials
# You can find this in your AZURE_CREDENTIALS secret (the "clientId" field)
SERVICE_PRINCIPAL_APP_ID="your-service-principal-app-id"

echo "Granting Storage Blob Data Contributor role to service principal..."

# Get the storage account resource ID
STORAGE_ACCOUNT_ID=$(az storage account show \
    --name $STORAGE_ACCOUNT_NAME \
    --resource-group $RESOURCE_GROUP \
    --query id \
    --output tsv)

echo "Storage Account ID: $STORAGE_ACCOUNT_ID"

# Assign the role
az role assignment create \
    --assignee $SERVICE_PRINCIPAL_APP_ID \
    --role "Storage Blob Data Contributor" \
    --scope $STORAGE_ACCOUNT_ID

echo "✅ Role assigned successfully!"

# Verify the assignment
echo ""
echo "Verifying role assignment..."
az role assignment list \
    --assignee $SERVICE_PRINCIPAL_APP_ID \
    --scope $STORAGE_ACCOUNT_ID \
    --output table

echo ""
echo "Done! Your service principal now has permissions to upload blobs to the storage account."
