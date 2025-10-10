# Docker Setup Guide

## Prerequisites

- Docker installed and running
- Docker Compose installed

## Quick Start

### 1. Configure Environment Variables

Copy the example environment file and configure your secrets:

```bash
cp .env.example .env
```

Edit `.env` and set:
- `DB_PASSWORD`: A secure password for PostgreSQL
- `JWT_SECRET_KEY`: A secure random string (minimum 32 characters)

**Important**: Never commit `.env` to version control!

### 2. Start the Application

```bash
docker-compose up -d
```

This will:
- Build the ASP.NET Core application
- Start PostgreSQL database
- Create a persistent volume for database data

### 3. Access the Application

- **Web UI**: http://localhost:5000
- **API**: http://localhost:5000/api/v1
- **PostgreSQL**: localhost:5432

### 4. Run Migrations

On first startup, you'll need to run database migrations:

```bash
# Access the app container
docker-compose exec app /bin/bash

# Run migrations
dotnet ef database update

# Exit
exit
```

## Common Commands

### View Logs

```bash
# All services
docker-compose logs -f

# Just the app
docker-compose logs -f app

# Just the database
docker-compose logs -f db
```

### Restart Services

```bash
docker-compose restart
```

### Stop Services

```bash
docker-compose down
```

### Stop Services and Remove Data

```bash
docker-compose down -v
```

### Rebuild After Code Changes

```bash
docker-compose up -d --build
```

## Production Deployment

For production deployments:

1. **Generate Secure Secrets**:
   ```bash
   # Generate a secure JWT secret
   openssl rand -base64 48

   # Generate a secure database password
   openssl rand -base64 32
   ```

2. **Update Environment Variables**:
   - Set secure values in your `.env` file
   - Or use your platform's secret management (Railway, Azure, etc.)

3. **Use HTTPS**:
   - Configure SSL certificates
   - Update ports in docker-compose.yml
   - Set ASPNETCORE_URLS environment variable

4. **Database Backups**:
   ```bash
   # Backup
   docker-compose exec db pg_dump -U postgres allowance_tracker > backup.sql

   # Restore
   cat backup.sql | docker-compose exec -T db psql -U postgres allowance_tracker
   ```

## Troubleshooting

### Container won't start

Check logs:
```bash
docker-compose logs app
```

### Database connection errors

Ensure database is ready:
```bash
docker-compose exec db pg_isready -U postgres
```

### Port already in use

Change ports in `docker-compose.yml`:
```yaml
ports:
  - "5001:80"  # Change 5000 to 5001
```

### Reset everything

```bash
docker-compose down -v
docker-compose up -d --build
```

## Development Tips

### Hot Reload (Development Mode)

For development with hot reload, use volume mounts:

```yaml
# Add to docker-compose.yml under 'app' service
volumes:
  - ./src/AllowanceTracker:/src/AllowanceTracker
environment:
  - ASPNETCORE_ENVIRONMENT=Development
```

### Access Database Directly

```bash
docker-compose exec db psql -U postgres -d allowance_tracker
```

## Architecture

```
┌─────────────────────────────────────┐
│   Browser (localhost:5000)          │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Blazor Server + API (app)         │
│   - ASP.NET Core 8.0                │
│   - SignalR                         │
│   - JWT Authentication              │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   PostgreSQL 16 (db)                │
│   - Persistent volume               │
│   - Port 5432                       │
└─────────────────────────────────────┘
```

## Environment Variables Reference

| Variable | Description | Required | Default |
|----------|-------------|----------|---------|
| `DB_PASSWORD` | PostgreSQL password | Yes | - |
| `JWT_SECRET_KEY` | JWT signing key (min 32 chars) | Yes | - |
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | No | Production |
| `ConnectionStrings__DefaultConnection` | Full database connection string | Auto-configured | - |

## Next Steps

After successfully running with Docker:
- Configure authentication (ASP.NET Core Identity)
- Set up admin user accounts
- Review security settings for production
- Set up automated backups
- Configure monitoring and logging
