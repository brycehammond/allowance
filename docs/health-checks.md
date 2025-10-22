# Health Check Endpoints

The Allowance Tracker API includes health check endpoints for monitoring application status and readiness. These endpoints are used by Azure DevOps pipelines for deployment verification and can be integrated with monitoring tools like Application Insights or Azure Monitor.

## Available Endpoints

### 1. Full Health Check (with Database)

```
GET /health
```

**Description:** Returns the health status of the API including database connectivity check.

**Response (Healthy):**
```json
{
  "status": "Healthy",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": null,
      "duration": 45.2
    }
  ],
  "totalDuration": 45.2
}
```

**Response (Unhealthy):**
```json
{
  "status": "Unhealthy",
  "checks": [
    {
      "name": "database",
      "status": "Unhealthy",
      "description": "Exception during database check",
      "duration": 120.5
    }
  ],
  "totalDuration": 120.5
}
```

**HTTP Status Codes:**
- `200 OK` - All checks passed (Healthy)
- `503 Service Unavailable` - One or more checks failed (Unhealthy/Degraded)

**Use Cases:**
- Full application health monitoring
- Database connectivity verification
- Post-deployment validation (used by Azure DevOps)
- Load balancer health checks
- Application Insights availability tests

---

### 2. Readiness Check (Simple)

```
GET /health/ready
```

**Description:** Simple health check that verifies the API is running and ready to accept requests. Does NOT check database connectivity.

**Response:**
- `200 OK` with "Healthy" text response

**Use Cases:**
- Kubernetes readiness probes
- Fast health checks without database overhead
- Container orchestration readiness detection
- Quick deployment verification

---

## Health Check Details

### Database Check

The database health check performs the following:

1. **Connection Test** - Verifies database connection string is valid
2. **Query Execution** - Executes a simple query against the database
3. **Timeout Handling** - Returns unhealthy if database doesn't respond within timeout

**Dependencies Checked:**
- SQL Server connection
- Entity Framework Core DbContext
- Database availability and accessibility

**Configuration:**

The database health check uses the same connection string as the application:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:...;Database=allowancetracker-db;..."
  }
}
```

---

## Integration with Azure DevOps

The health checks are integrated into the Azure DevOps deployment pipeline:

```yaml
- task: PowerShell@2
  displayName: 'Verify Deployment Health'
  inputs:
    targetType: 'inline'
    script: |
      $appUrl = "https://$(ApiAppServiceName).azurewebsites.net/health"
      $response = Invoke-WebRequest -Uri $appUrl -Method Get

      if ($response.StatusCode -eq 200) {
        Write-Host "✅ Deployment health check passed"
      } else {
        Write-Host "❌ Deployment health check failed"
        exit 1
      }
```

This ensures the API and database are fully operational after deployment.

---

## Integration with Application Insights

You can configure Application Insights availability tests to monitor these endpoints:

1. Go to Application Insights → Availability
2. Click "Add Standard test" or "Add Custom TrackAvailability test"
3. Configure:
   - **URL:** `https://your-api.azurewebsites.net/health`
   - **Frequency:** 5 minutes
   - **Test locations:** Select multiple geographic regions
   - **Success criteria:** Status code equals 200

---

## Integration with Azure Monitor

Configure Azure Monitor alerts based on health check failures:

```bash
# Create alert rule for health check failures
az monitor metrics alert create \
  --name "API-Health-Check-Failed" \
  --resource-group allowancetracker-rg \
  --scopes "/subscriptions/.../resourceGroups/allowancetracker-rg/providers/Microsoft.Web/sites/allowancetracker-api" \
  --condition "count Http5xx > 5" \
  --window-size 5m \
  --evaluation-frequency 1m
```

---

## Local Development Testing

Test health checks during local development:

```bash
# Start the API
dotnet run --project src/AllowanceTracker

# Test full health check (with database)
curl http://localhost:5000/health

# Test readiness check (without database)
curl http://localhost:5000/health/ready
```

---

## Health Check Status Codes

ASP.NET Core Health Checks return the following statuses:

| Status | Description | HTTP Code |
|--------|-------------|-----------|
| `Healthy` | All checks passed | 200 OK |
| `Degraded` | Some checks passed with warnings | 200 OK |
| `Unhealthy` | One or more checks failed | 503 Service Unavailable |

---

## Troubleshooting

### Health Check Returns Unhealthy

**Possible causes:**
1. Database connection issues
2. SQL Server firewall rules blocking access
3. Invalid connection string
4. Database migrations not applied
5. SQL Server credentials incorrect

**Resolution:**
```bash
# Check database connectivity
dotnet ef database update --connection "YourConnectionString"

# Verify firewall rules
az sql server firewall-rule list \
  --resource-group allowancetracker-rg \
  --server allowancetracker-sql

# Test connection string
sqlcmd -S tcp:allowancetracker-sql.database.windows.net,1433 \
  -d allowancetracker-db -U sqladmin -P YourPassword -Q "SELECT 1"
```

### Health Check Times Out

**Possible causes:**
1. Network latency
2. Database performance issues
3. SQL queries taking too long

**Resolution:**
- Check database performance metrics in Azure Portal
- Review query execution plans
- Consider adding database indexes
- Scale up database tier if needed

---

## Best Practices

1. **Monitor Both Endpoints**
   - Use `/health` for comprehensive monitoring
   - Use `/health/ready` for high-frequency checks

2. **Set Appropriate Timeouts**
   - Health checks should complete within 5-10 seconds
   - Configure timeout in Application Insights availability tests

3. **Alert Configuration**
   - Don't alert on single failures (use thresholds)
   - Configure alerts for sustained failures (3+ consecutive)
   - Use different severity levels for different health checks

4. **Database Performance**
   - The database check executes a simple query
   - Should complete in under 100ms in healthy state
   - Monitor check duration trends over time

5. **Security Considerations**
   - Health checks don't require authentication
   - Don't expose sensitive information in health check responses
   - Use HTTPS in production

---

## Additional Resources

- [ASP.NET Core Health Checks Documentation](https://docs.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- [Azure Application Insights Availability Tests](https://docs.microsoft.com/azure/azure-monitor/app/monitor-web-app-availability)
- [Azure Monitor Alerts](https://docs.microsoft.com/azure/azure-monitor/alerts/alerts-overview)
