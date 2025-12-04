# Multi-Cloud Serverless Deployment Guide

This directory contains Terraform configurations for deploying the Allowance Tracker application to Azure Functions, AWS Lambda, and Google Cloud Functions.

## Architecture Overview

The application uses a **cloud-agnostic core** with cloud-specific adapters:

```
┌─────────────────────────────────────────────────────────────┐
│                   AllowanceTracker.Core                      │
│              (Business Logic - Cloud Agnostic)               │
│                                                              │
│  ├── Models        ├── Services      ├── Handlers           │
│  ├── DTOs          └── Data          └── AuthHandler        │
│                                          ChildrenHandler     │
└─────────────────────────────────────────────────────────────┘
                              │
           ┌──────────────────┼──────────────────┐
           │                  │                  │
           ▼                  ▼                  ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│  Azure Adapter  │  │   AWS Adapter   │  │   GCP Adapter   │
│                 │  │                 │  │                 │
│ ├─ HTTP         │  │ ├─ HTTP         │  │ ├─ HTTP         │
│ ├─ Config       │  │ ├─ Config       │  │ ├─ Config       │
│ └─ SQL Server   │  │ └─ PostgreSQL   │  │ └─ PostgreSQL   │
└─────────────────┘  └─────────────────┘  └─────────────────┘
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ Azure Functions │  │  AWS Lambda     │  │ GCP Cloud Funcs │
│                 │  │                 │  │                 │
│ ├─ AuthFunctions│  │ ├─ AuthFunctions│  │ ├─ AuthFunctions│
│ └─ ChildrenFuncs│  │ └─ ChildrenFuncs│  │ └─ ChildrenFuncs│
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

## Prerequisites

### All Platforms
- [Terraform](https://www.terraform.io/downloads.html) >= 1.0
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Git

### Azure
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- Azure subscription

### AWS
- [AWS CLI](https://aws.amazon.com/cli/)
- AWS account with appropriate permissions

### GCP
- [Google Cloud SDK](https://cloud.google.com/sdk/docs/install)
- GCP project

## Cost Estimates

### Azure (Y1 Consumption Plan)
- **Azure Functions**: ~$0-10/month (first 1M executions free)
- **Azure SQL Basic**: ~$5/month
- **Application Insights**: ~$0-5/month
- **Storage Account**: ~$0.50/month
- **Total**: ~$5-15/month

### AWS (Free Tier)
- **Lambda**: Free (1M requests/month free)
- **RDS db.t4g.micro**: ~$12/month
- **API Gateway**: ~$3.50/month (1M requests)
- **CloudWatch**: ~$1/month
- **Total**: ~$15-20/month

### GCP
- **Cloud Functions**: ~$2/month (2M invocations free)
- **Cloud SQL db-f1-micro**: ~$7/month
- **Cloud Monitoring**: Free tier
- **Total**: ~$7-12/month

## Deployment Steps

### 1. Build the Application

First, build the application for your target cloud:

#### For Azure Functions

```bash
cd src/AllowanceTracker.Functions
dotnet publish -c Release
```

#### For AWS Lambda

```bash
cd src/AllowanceTracker.Lambda
dotnet publish -c Release

# Create deployment package
cd bin/Release/net8.0/publish
zip -r ../../../../../terraform/lambda-deployment.zip .
```

### 2. Configure Secrets

Create a `terraform.tfvars` file in the appropriate directory (DO NOT commit this file):

#### Azure (`terraform/azure/terraform.tfvars`)

```hcl
subscription_id     = "your-azure-subscription-id"
sql_admin_username              = "sqladmin"
sql_admin_password              = "YourSecureP@ssw0rd123!"
jwt_secret_key                  = "your-32-character-or-longer-secret-key-here-123456"
sendgrid_api_key                = "SG.your-sendgrid-api-key"
sendgrid_from_email             = "noreply@allowancetracker.com"

# Optional overrides
project_name        = "allowancetracker"
environment         = "dev"
location            = "East US"
```

#### AWS (`terraform/aws/terraform.tfvars`)

```hcl
aws_region          = "us-east-1"
db_username                     = "dbadmin"
db_password                     = "YourSecureP@ssw0rd123!"
jwt_secret_key                  = "your-32-character-or-longer-secret-key-here-123456"
sendgrid_api_key                = "SG.your-sendgrid-api-key"
sendgrid_from_email             = "noreply@allowancetracker.com"

# Optional overrides
project_name        = "allowancetracker"
environment         = "dev"
```

### 3. Deploy Infrastructure

#### Azure Deployment

```bash
# Navigate to Azure terraform directory
cd terraform/azure

# Login to Azure
az login

# Initialize Terraform
terraform init

# Review the plan
terraform plan

# Deploy infrastructure
terraform apply

# Get outputs
terraform output function_app_url
terraform output connection_string
```

#### Azure Functions Deployment

After infrastructure is deployed:

```bash
# Get the function app name from Terraform
FUNC_APP_NAME=$(terraform output -raw function_app_name)

# Deploy using Azure Functions Core Tools
cd ../../src/AllowanceTracker.Functions
func azure functionapp publish $FUNC_APP_NAME
```

#### AWS Deployment

```bash
# Navigate to AWS terraform directory
cd terraform/aws

# Configure AWS credentials
aws configure

# Initialize Terraform
terraform init

# Review the plan
terraform plan

# Deploy infrastructure
terraform apply

# Get outputs
terraform output api_gateway_url
terraform output connection_string
```

### 4. Run Database Migrations

After deployment, you need to run EF Core migrations to set up the database:

#### For Azure

```bash
# Set connection string from Terraform output
export ConnectionStrings__DefaultConnection=$(terraform output -raw connection_string)

# Run migrations
cd ../../src/AllowanceTracker.Core
dotnet ef database update
```

#### For AWS

```bash
# Set connection string from Terraform output
export DATABASE_CONNECTION_STRING=$(terraform output -raw connection_string)

# Run migrations (using PostgreSQL provider)
cd ../../src/AllowanceTracker.Core
dotnet ef database update --context AllowanceContext
```

### 5. Test Deployment

Test the API endpoints:

```bash
# Azure
curl https://your-func-app.azurewebsites.net/api/v1/auth/register/parent \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!","firstName":"John","lastName":"Doe","familyName":"Doe Family"}'

# AWS
curl https://your-api-id.execute-api.us-east-1.amazonaws.com/api/v1/auth/register/parent \
  -X POST \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!","firstName":"John","lastName":"Doe","familyName":"Doe Family"}'
```

## Environment Variables

### Azure Functions

Set in `terraform/azure/main.tf` under `azurerm_linux_function_app.app_settings`:

- `SqlConnectionString` - Database connection string
- `Jwt__SecretKey` - JWT secret key
- `SendGrid__ApiKey` - SendGrid API key
- `SendGrid__FromEmail` - Sender email address
- `SendGrid__FromName` - Sender display name

### AWS Lambda

Set in `terraform/aws/main.tf` under `aws_lambda_function.environment.variables`:

- `DATABASE_CONNECTION_STRING` - PostgreSQL connection string
- `JWT_SECRET_KEY` - JWT secret key
- `SENDGRID_API_KEY` - SendGrid API key
- `SENDGRID_FROM_EMAIL` - Sender email address
- `SENDGRID_FROM_NAME` - Sender display name

## Monitoring

### Azure

View logs and metrics in:
- Azure Portal > Function App > Monitor
- Application Insights dashboard
- Log Analytics workspace

```bash
# Stream logs
func azure functionapp logstream $FUNC_APP_NAME
```

### AWS

View logs and metrics in:
- CloudWatch Logs: `/aws/lambda/function-name`
- CloudWatch Metrics: Lambda and API Gateway dashboards
- X-Ray for distributed tracing

```bash
# View recent logs
aws logs tail /aws/lambda/allowancetracker-dev-register-parent --follow
```

## Scaling

### Azure Functions (Consumption Plan)
- Auto-scales 0 to 200 instances
- No configuration needed
- Upgrade to Premium (EP1) for:
  - Faster cold starts
  - VNet integration
  - Unlimited execution time

### AWS Lambda
- Auto-scales up to 1000 concurrent executions (default)
- Request limit increase via AWS Support
- Consider provisioned concurrency for critical endpoints

## Cleanup

To destroy all resources:

```bash
# Azure
cd terraform/azure
terraform destroy

# AWS
cd terraform/aws
terraform destroy
```

## Troubleshooting

### Azure Functions Not Starting
```bash
# Check logs
func azure functionapp logstream $FUNC_APP_NAME

# Verify configuration
az functionapp config appsettings list --name $FUNC_APP_NAME --resource-group $RESOURCE_GROUP
```

### AWS Lambda Cold Starts
- Consider provisioned concurrency
- Optimize deployment package size
- Use ReadyToRun compilation

### Database Connection Issues
```bash
# Azure: Check firewall rules
az sql server firewall-rule list --server $SQL_SERVER --resource-group $RESOURCE_GROUP

# AWS: Check security groups
aws ec2 describe-security-groups --group-ids $SECURITY_GROUP_ID
```

## Security Best Practices

1. **Never commit `terraform.tfvars`** - Add to `.gitignore`
2. **Use Azure Key Vault / AWS Secrets Manager** for production secrets
3. **Enable Application Insights / CloudWatch** for monitoring
4. **Implement API rate limiting** using API Management / API Gateway
5. **Use managed identities** where possible (Azure) or IAM roles (AWS)
6. **Enable SSL/TLS** for all endpoints
7. **Rotate JWT secrets** regularly
8. **Use separate environments** (dev, staging, prod) with different secrets

## CI/CD Integration

### GitHub Actions Example

Create `.github/workflows/deploy-azure.yml`:

```yaml
name: Deploy to Azure

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Build
        run: dotnet publish src/AllowanceTracker.Functions -c Release

      - name: Deploy to Azure Functions
        uses: Azure/functions-action@v1
        with:
          app-name: ${{ secrets.AZURE_FUNCTION_APP_NAME }}
          package: src/AllowanceTracker.Functions/bin/Release/net8.0/publish
          publish-profile: ${{ secrets.AZURE_FUNCTIONAPP_PUBLISH_PROFILE }}
```

## Support

For issues or questions:
- Check the main project README
- Review CloudWatch/Application Insights logs
- Open an issue on GitHub

## License

MIT License - see LICENSE file for details
