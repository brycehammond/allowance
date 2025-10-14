# Testing the Azure Function Locally

Quick guide for testing the Weekly Allowance Function on your local machine.

## Prerequisites

1. **Azure Functions Core Tools** installed:
   ```bash
   # macOS
   brew tap azure/functions
   brew install azure-functions-core-tools@4

   # Windows (npm)
   npm install -g azure-functions-core-tools@4
   ```

2. **SQL Server** running locally (see LOCAL_DEVELOPMENT.md)

3. **API** running (optional, for end-to-end testing)

## Quick Test

### 1. Build the Function

```bash
cd src/AllowanceTracker.Functions
dotnet build
```

### 2. Run the Function

```bash
func start
```

You should see:
```
Azure Functions Core Tools
Core Tools Version: 4.x.x
Function Runtime Version: 4.x.x

Functions:
  ProcessWeeklyAllowances: timer
  ProcessWeeklyAllowancesManual: [POST] http://localhost:7071/api/ProcessWeeklyAllowancesManual
```

### 3. Test with Manual Trigger

In another terminal:

```bash
# Trigger allowance processing
curl -X POST http://localhost:7071/api/ProcessWeeklyAllowancesManual

# Expected response:
{
  "message": "Weekly allowances processed successfully",
  "timestamp": "2025-10-13T23:00:00Z"
}
```

### 4. Verify in Database

```bash
# Check transactions were created
sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -d AllowanceTracker -Q \
  "SELECT TOP 10 * FROM Transactions ORDER BY CreatedAt DESC"
```

## Timer Trigger Testing

The timer is set to run daily at 10:00 AM UTC. To test immediately:

1. Edit `WeeklyAllowanceFunction.cs`
2. Change the cron expression:
   ```csharp
   // From:
   [Function("ProcessWeeklyAllowances")]
   public async Task Run([TimerTrigger("0 0 10 * * *")] TimerInfo timer)

   // To (runs every minute):
   [Function("ProcessWeeklyAllowances")]
   public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo timer)
   ```
3. Rebuild and run: `func start`
4. Watch logs - should execute every minute
5. **Remember to change it back!**

## Troubleshooting

### Function won't start

**Error: "Connection string not found"**

Check `local.settings.json`:
```json
{
  "Values": {
    "ConnectionStrings__DefaultConnection": "Server=localhost,1433;Database=AllowanceTracker;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;MultipleActiveResultSets=true"
  }
}
```

### Manual trigger returns error

**Check logs:**
- Look for stack traces in the terminal
- Common issues:
  - Database not running
  - Connection string incorrect
  - No children with allowances configured

**Create test data:**
```bash
# Use the API to create a child with weekly allowance
curl -X POST http://localhost:7071/api/v1/children \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "child@example.com",
    "password": "Test123!",
    "firstName": "Test",
    "lastName": "Child",
    "weeklyAllowance": 10.00
  }'
```

## What Gets Processed?

The Function:
1. Finds all children with `WeeklyAllowance > 0`
2. Checks if `LastAllowanceDate` is null or ≥7 days ago
3. Creates a Credit transaction for the allowance amount
4. Processes automatic savings transfer (if enabled)
5. Updates `LastAllowanceDate` to prevent double payment

## Production Testing

Once deployed to Azure, test via:

1. **Azure Portal**:
   - Navigate to Function App
   - Click "Functions" → "ProcessWeeklyAllowances"
   - Click "Code + Test" → "Test/Run"

2. **Manual HTTP Trigger**:
   ```bash
   curl -X POST \
     "https://allowancetracker-function.azurewebsites.net/api/ProcessWeeklyAllowancesManual" \
     -H "x-functions-key: YOUR_FUNCTION_KEY"
   ```

3. **Monitor Logs**:
   ```bash
   az functionapp log tail \
     --name allowancetracker-function \
     --resource-group allowancetracker-rg
   ```

## Next Steps

- See [WEEKLY_ALLOWANCE.md](WEEKLY_ALLOWANCE.md) for detailed explanation
- See [GITHUB-ACTIONS-DEPLOYMENT.md](GITHUB-ACTIONS-DEPLOYMENT.md) for deployment
