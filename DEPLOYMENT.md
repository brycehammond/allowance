# Deployment Guide

This guide covers deploying the Allowance Tracker application to various platforms.

## Table of Contents

- [Azure App Service](#azure-app-service)
- [Docker (Self-Hosted)](#docker-self-hosted)
- [CI/CD Pipelines](#cicd-pipelines)

---

## Azure App Service

Azure App Service provides enterprise-grade hosting with easy scaling.

### Prerequisites

- Azure account
- Azure CLI installed

### Steps

1. **Create Resource Group**

   ```bash
   az group create --name allowance-tracker-rg --location eastus
   ```

2. **Create App Service Plan**

   ```bash
   az appservice plan create \
     --name allowance-tracker-plan \
     --resource-group allowance-tracker-rg \
     --sku B1 \
     --is-linux
   ```

3. **Create Web App**

   ```bash
   az webapp create \
     --resource-group allowance-tracker-rg \
     --plan allowance-tracker-plan \
     --name allowance-tracker-app \
     --runtime "DOTNET|8.0"
   ```

4. **Create PostgreSQL Database**

   ```bash
   az postgres flexible-server create \
     --resource-group allowance-tracker-rg \
     --name allowance-tracker-db \
     --location eastus \
     --admin-user dbadmin \
     --admin-password <secure-password> \
     --sku-name Standard_B1ms \
     --tier Burstable \
     --version 16
   ```

5. **Configure Connection String**

   ```bash
   az webapp config connection-string set \
     --resource-group allowance-tracker-rg \
     --name allowance-tracker-app \
     --settings DefaultConnection="Host=allowance-tracker-db.postgres.database.azure.com;Database=allowance_tracker;Username=dbadmin;Password=<password>;SSL Mode=Require" \
     --connection-string-type PostgreSQL
   ```

6. **Set Environment Variables**

   ```bash
   az webapp config appsettings set \
     --resource-group allowance-tracker-rg \
     --name allowance-tracker-app \
     --settings \
       ASPNETCORE_ENVIRONMENT=Production \
       Jwt__SecretKey=<secure-key> \
       Jwt__Issuer=AllowanceTracker \
       Jwt__Audience=AllowanceTracker
   ```

7. **Deploy**

   ```bash
   # Build and publish
   dotnet publish src/AllowanceTracker/AllowanceTracker.csproj -c Release -o ./publish

   # Create deployment package
   cd publish
   zip -r ../deploy.zip .
   cd ..

   # Deploy to Azure
   az webapp deployment source config-zip \
     --resource-group allowance-tracker-rg \
     --name allowance-tracker-app \
     --src deploy.zip
   ```

8. **Run Migrations**

   Use Azure Console or SSH:
   ```bash
   dotnet ef database update
   ```

### Cost

- Basic (B1): ~$13/month
- PostgreSQL: ~$12/month
- Total: ~$25/month

---

## Docker (Self-Hosted)

Deploy using Docker on your own server or cloud VM.

### Prerequisites

- Server with Docker and Docker Compose
- Domain name (optional)

### Steps

1. **Clone Repository**

   ```bash
   git clone <your-repo>
   cd allowance
   ```

2. **Configure Environment**

   ```bash
   cp .env.example .env
   # Edit .env with your values
   ```

3. **Start Services**

   ```bash
   docker-compose up -d
   ```

4. **Run Migrations**

   ```bash
   docker-compose exec app dotnet ef database update
   ```

5. **Access Application**

   - Local: http://localhost:5000
   - Production: Configure reverse proxy (nginx/Caddy)

### Reverse Proxy (nginx)

```nginx
server {
    listen 80;
    server_name allowancetracker.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

### SSL with Let's Encrypt

```bash
sudo certbot --nginx -d allowancetracker.com
```

See [DOCKER.md](./DOCKER.md) for detailed Docker instructions.

---

## CI/CD Pipelines

### GitHub Actions

The project uses three separate GitHub Actions workflows for efficient CI/CD:

#### 1. **API Workflow** (`.github/workflows/api.yml`)
   - Triggers on changes to `src/**`
   - Builds .NET API
   - Runs 213 tests with code coverage
   - Checks code formatting
   - Builds Docker image (on main branch)

#### 2. **Web Workflow** (`.github/workflows/web.yml`)
   - Triggers on changes to `web/**`
   - Builds React app with Vite
   - Runs ESLint and TypeScript checks
   - Creates production build
   - Runs Lighthouse CI on PRs

#### 3. **iOS Workflow** (`.github/workflows/ios.yml`)
   - Triggers on changes to `ios/**`
   - Builds iOS app with Xcode
   - Runs tests on iPhone 15 simulator
   - Runs SwiftLint for code quality

**Benefits of Separate Workflows**:
- Only relevant tests run for each change
- Faster feedback for developers
- Independent deployment pipelines
- More efficient CI/CD resource usage

**Setup**:

1. Configure GitHub Secrets in repository settings:
   - `AZURE_CREDENTIALS`: Azure service principal JSON
   - `AZURE_SQL_CONNECTION_STRING`: Database connection string
   - `JWT_SECRET_KEY`: JWT secret key (min 32 chars)
   - `APPLICATION_INSIGHTS_CONNECTION_STRING`: App Insights connection

2. Configure GitHub Variables:
   - `AZURE_WEBAPP_NAME`: API App Service name
   - `AZURE_FUNCTION_APP_NAME`: Function App name
   - `AZURE_STORAGE_ACCOUNT`: Storage account name
   - `AZURE_RESOURCE_GROUP`: Resource group name

3. Push to `main` or `develop` branches to trigger workflows

See **[GITHUB-ACTIONS-DEPLOYMENT.md](GITHUB-ACTIONS-DEPLOYMENT.md)** for detailed setup instructions.

---

## Environment Variables Reference

| Variable | Description | Required | Example |
|----------|-------------|----------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | Yes | `Production` |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection | Yes | See platform docs |
| `Jwt__SecretKey` | JWT signing key | Yes | 32+ char random string |
| `Jwt__Issuer` | JWT issuer | No | `AllowanceTracker` |
| `Jwt__Audience` | JWT audience | No | `AllowanceTracker` |
| `ASPNETCORE_URLS` | Listen URLs | Platform-specific | `http://0.0.0.0:80` |

---

## Generating Secure Secrets

### JWT Secret Key

```bash
# Generate 48-character base64 string
openssl rand -base64 48
```

### Database Password

```bash
# Generate 32-character base64 string
openssl rand -base64 32
```

---

## Database Migrations

### Manual Migration

```bash
# Install EF Core tools
dotnet tool install --global dotnet-ef

# Run migrations
dotnet ef database update --project src/AllowanceTracker
```

### Automatic Migration on Startup

Add to `Program.cs`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AllowanceContext>();
    context.Database.Migrate();
}
```

**Warning**: Only use in development or with proper backup strategy!

---

## Monitoring and Logging

### Application Insights (Azure)

Add to `Program.cs`:

```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Serilog

Add structured logging:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
```

```csharp
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));
```

---

## Security Checklist

- [ ] Change all default secrets
- [ ] Use HTTPS in production
- [ ] Enable CORS only for trusted domains
- [ ] Configure rate limiting
- [ ] Set up database backups
- [ ] Enable application logging
- [ ] Configure firewall rules
- [ ] Use secure connection strings
- [ ] Enable authentication
- [ ] Review ASP.NET Core security headers

---

## Troubleshooting

### Common Issues

**Connection String Format**

Azure PostgreSQL connection string format:
```
Host=host;Database=dbname;Username=user;Password=pass;SSL Mode=Require
```

**Port Binding**

Ensure app listens on the correct port:
```bash
ASPNETCORE_URLS=http://0.0.0.0:$PORT
```

**Database Migrations**

If migrations fail:
```bash
# Check connection
pg_isready -h <host>

# View migration status
dotnet ef migrations list
```

---

## Support

- [Azure App Service Docs](https://docs.microsoft.com/azure/app-service/)
- [Docker Docs](https://docs.docker.com/)
- [ASP.NET Core Deployment](https://docs.microsoft.com/aspnet/core/host-and-deploy/)
