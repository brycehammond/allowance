# Weekly Allowance System

This document explains how the automated weekly allowance payment system works.

---

## How It Works

The weekly allowance system uses an **Azure Function with timer trigger** that runs daily. Here's the flow:

```
┌─────────────────────────────────────────────────────────┐
│     WeeklyAllowanceFunction (Azure Function)            │
│                                                           │
│  Timer Trigger: Runs daily at 10:00 AM UTC              │
│  Checks all children with WeeklyAllowance > 0            │
│  Pays if LastAllowanceDate is null or ≥7 days ago       │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│              AllowanceService                            │
│                                                           │
│  ProcessAllPendingAllowancesAsync()                      │
│    ├── Gets all children with WeeklyAllowance > 0       │
│    ├── Checks each child's LastAllowanceDate            │
│    ├── Calls PayWeeklyAllowanceAsync() for eligible     │
│    └── Logs results                                      │
│                                                           │
│  PayWeeklyAllowanceAsync(childId)                        │
│    ├── Validates child has allowance configured         │
│    ├── Checks not already paid this week                │
│    ├── Creates Credit transaction (Category: Allowance) │
│    ├── Processes automatic savings transfer (if enabled)│
│    ├── Updates LastAllowanceDate to now                 │
│    └── Saves to database                                 │
└─────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────┐
│           TransactionService                             │
│                                                           │
│  Creates transaction in database                         │
│  Updates child's CurrentBalance                          │
│  All wrapped in atomic transaction                       │
└─────────────────────────────────────────────────────────┘
```

---

## Azure Function Details

### Location
`src/AllowanceTracker.Functions/WeeklyAllowanceFunction.cs`

### How It Works
Azure Function with timer trigger that runs on a schedule:

**Timer Schedule (NCRONTAB format):**
```
"0 0 10 * * *"
```
- Runs **daily at 10:00 AM UTC**
- {second} {minute} {hour} {day} {month} {day-of-week}

**Benefits:**
- ✅ Dedicated service (doesn't run in API process)
- ✅ Guaranteed execution by Azure scheduler
- ✅ No "Always On" requirement (runs on consumption plan)
- ✅ Independent scaling from API
- ✅ Automatic retry on failure
- ✅ Built-in monitoring with Application Insights
- ✅ Nearly free (well within consumption plan free tier)

### Deployment
The Function is deployed separately from the API:
- Built and deployed via Azure Pipelines
- Shares the same database as the API
- Uses the same `AllowanceService` for business logic
- Configured with connection string and Application Insights

---

## Payment Logic

### Eligibility Check

A child receives allowance if **ALL** of these are true:

1. ✅ `WeeklyAllowance > 0` (child has allowance configured)
2. ✅ `LastAllowanceDate` is `null` (never paid before) **OR**
3. ✅ `(DateTime.UtcNow - LastAllowanceDate).TotalDays >= 7` (7+ days since last payment)

### What Happens on Payment

1. **Create Transaction:**
   ```csharp
   Type: Credit
   Category: Allowance
   Amount: child.WeeklyAllowance
   Description: "Weekly Allowance - 2025-01-15"
   ```

2. **Update Balance:**
   - `child.CurrentBalance += child.WeeklyAllowance`

3. **Process Automatic Savings (if enabled):**
   - Checks if child has `SavingsAccountEnabled = true`
   - Transfers percentage or fixed amount to savings
   - Example: 10% of $10.00 allowance = $1.00 to savings

4. **Update Last Payment Date:**
   - `child.LastAllowanceDate = DateTime.UtcNow`

5. **Audit Trail:**
   - Transaction has `CreatedById` (system user)
   - Transaction has `CreatedAt` timestamp
   - Immutable record forever

### Prevention of Double Payment

The service checks:
```csharp
if (child.LastAllowanceDate.HasValue)
{
    var daysSinceLastPayment = (DateTime.UtcNow - child.LastAllowanceDate.Value).TotalDays;
    if (daysSinceLastPayment < 7)
        throw new InvalidOperationException("Allowance already paid this week");
}
```

This ensures a child **cannot receive allowance twice in the same week**, even if the background job runs multiple times or a manual trigger is added.

---

## Example Scenarios

### Scenario 1: New Child Added

```
Timeline:
Day 0:  Parent adds child with $10 weekly allowance
        LastAllowanceDate = null
        CurrentBalance = $0

Day 1:  Background job runs (24h after API started)
        ✅ Child is eligible (LastAllowanceDate is null)
        → Pays $10 allowance
        → LastAllowanceDate = 2025-01-15 10:00 AM
        → CurrentBalance = $10

Day 8:  Background job runs again
        ✅ Child is eligible (8 days since last payment)
        → Pays $10 allowance
        → LastAllowanceDate = 2025-01-22 10:00 AM
        → CurrentBalance = $20
```

### Scenario 2: Child with $0 Allowance

```
Timeline:
Day 0:  Parent adds child with $0 weekly allowance
        WeeklyAllowance = 0

Day 1:  Background job runs
        ❌ Child is NOT eligible (WeeklyAllowance = 0)
        → Skipped

Day 5:  Parent changes allowance to $5
        WeeklyAllowance = $5
        LastAllowanceDate = null

Day 6:  Background job runs
        ✅ Child is eligible (LastAllowanceDate is null)
        → Pays $5 allowance
        → CurrentBalance = $5
```

### Scenario 3: With Automatic Savings

```
Timeline:
Day 0:  Parent adds child:
        - WeeklyAllowance = $10
        - SavingsAccountEnabled = true
        - SavingsTransferType = Percentage
        - SavingsTransferPercentage = 20%

Day 1:  Background job runs
        → Pays $10 allowance to CurrentBalance
        → Automatically transfers $2 (20% of $10) to savings
        → CurrentBalance = $8
        → SavingsBalance = $2
        → Two transactions created:
          1. Credit $10 (Allowance)
          2. Debit $2 (Automatic Transfer to Savings)
```

---

## Monitoring and Logs

### Log Output

When the job runs, you'll see logs like this:

```
[2025-01-15 10:00:00] Information: Weekly Allowance Job started
[2025-01-15 10:00:00] Information: Starting weekly allowance processing
[2025-01-15 10:00:01] Information: Paid weekly allowance of 10.00 to child abc123
[2025-01-15 10:00:01] Information: Processed automatic savings transfer for child abc123
[2025-01-15 10:00:02] Information: Paid weekly allowance of 5.00 to child def456
[2025-01-15 10:00:02] Information: Processed 2 allowances with 0 errors
[2025-01-15 10:00:02] Information: Completed weekly allowance processing
```

### Viewing Logs Locally

**Windows (PowerShell):**
```powershell
cd src/AllowanceTracker
dotnet run | Select-String "Allowance"
```

**macOS/Linux (Bash):**
```bash
cd src/AllowanceTracker
dotnet run | grep "Allowance"
```

### Viewing Logs on Azure

```bash
# Stream logs
az webapp log tail \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api | grep "Allowance"

# Download logs
az webapp log download \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --log-file logs.zip
```

---

## Testing Locally

### Method 1: Use Azure Functions Core Tools (Recommended)

The Azure Functions Core Tools allow you to run the Function locally:

**Prerequisites:**
```bash
# Install Azure Functions Core Tools
# macOS:
brew tap azure/functions
brew install azure-functions-core-tools@4

# Windows (via npm):
npm install -g azure-functions-core-tools@4

# Or download from: https://docs.microsoft.com/azure/azure-functions/functions-run-local
```

**Steps:**
1. Ensure SQL Server is running (see LOCAL_DEVELOPMENT.md)
2. Start the API (optional, for testing alongside):
   ```bash
   cd src/AllowanceTracker
   dotnet run
   ```
3. In a new terminal, start the Function:
   ```bash
   cd src/AllowanceTracker.Functions
   func start
   ```
4. The Function will run on http://localhost:7071 (different from API)
5. **To test timer immediately**, use the manual trigger endpoint

**Manual HTTP Trigger (Built-in for Testing):**

The Function includes a manual trigger endpoint:
```bash
# Trigger allowance processing manually
curl -X POST http://localhost:7071/api/ProcessWeeklyAllowancesManual
```

This endpoint:
- Doesn't require authentication (Function-level auth in production)
- Runs the same logic as the timer trigger
- Returns immediate response with results
- Perfect for testing during development

### Method 2: Change Timer Schedule (For Testing)

Temporarily change the timer schedule to run every minute:

**Edit `WeeklyAllowanceFunction.cs`:**
```csharp
// Change this line:
[Function("ProcessWeeklyAllowances")]
public async Task Run([TimerTrigger("0 0 10 * * *")] TimerInfo timer)

// To this (for testing only):
[Function("ProcessWeeklyAllowances")]
public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo timer) // Every 1 minute
```

**⚠️ Important:** Change it back before deploying!

### Method 3: Test via Azure Portal (Production)

Once deployed to Azure, you can test the Function:

1. Go to Azure Portal → Your Function App
2. Click on "Functions" → "ProcessWeeklyAllowances"
3. Click "Code + Test"
4. Click "Test/Run"
5. Click "Run"
6. Check logs and execution history

**Or use the manual HTTP trigger:**
```bash
curl -X POST https://<your-function-app>.azurewebsites.net/api/ProcessWeeklyAllowancesManual \
  -H "x-functions-key: YOUR_FUNCTION_KEY"
```

---

## Frequently Asked Questions

### Q: When exactly does the allowance pay?

**A:** The background job checks every 24 hours. The exact time depends on when you start the API:
- If you start API at 10:00 AM → Job runs at 10:00 AM daily
- If you restart API at 3:00 PM → Job now runs at 3:00 PM daily

### Q: What if the API restarts?

**A:** The background job restarts immediately with the API. It will check all children again, but the 7-day rule prevents double payments.

Example:
```
Day 1, 10:00 AM: Job runs, pays allowances
Day 1, 2:00 PM:  API restarts (deployment/crash)
Day 1, 2:00 PM:  Job runs again, but skips (< 7 days since payment)
Day 2, 2:00 PM:  Job runs (24h later), still skips (< 7 days)
Day 8, 2:00 PM:  Job runs, pays allowances (≥ 7 days)
```

### Q: What timezone is used?

**A:** All dates use **UTC (Coordinated Universal Time)**:
```csharp
DateTime.UtcNow
```

This prevents issues with daylight saving time and works globally. Display times in the user's local timezone in the UI.

### Q: What if a child has no allowance configured?

**A:** Children with `WeeklyAllowance = 0` are skipped:
```csharp
.Where(c => c.WeeklyAllowance > 0)
```

### Q: Can I change a child's allowance amount?

**A:** Yes! Parent can update `WeeklyAllowance` at any time:
- Next payment will use the new amount
- Does not affect past payments
- Does not reset the `LastAllowanceDate`

Example:
```
Day 1:  Allowance is $5, gets paid $5
Day 5:  Parent changes allowance to $10
Day 8:  Gets paid $10 (new amount)
```

### Q: What if the database is down?

**A:** The job catches exceptions and logs errors:
```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to process allowance for child {ChildId}", child.Id);
    // Continue processing other children
}
```

Failed payments are **not retried** - the job continues to the next child. On the next run (24h later), it will try again.

### Q: Does the job block HTTP requests?

**A:** No! The background job runs on a separate thread and doesn't affect API performance. Users won't notice it running.

---

## Production Considerations

### High Availability

If you have **multiple API instances** (e.g., scaled out on Azure):
- Each instance runs its own background job
- Multiple jobs will attempt to pay allowances
- ✅ The 7-day check prevents double payments
- ⚠️ You might see "already paid" log warnings

**Better solution:** Use distributed locking:

```bash
dotnet add package DistributedLock.SqlServer
```

```csharp
using Medallion.Threading.Sql;

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var lockProvider = new SqlDistributedSynchronizationProvider(_connectionString);

    while (!stoppingToken.IsCancellationRequested)
    {
        await using (await lockProvider.AcquireLockAsync("WeeklyAllowanceJob"))
        {
            await ProcessAllowancesAsync();
        }

        await Task.Delay(_checkInterval, stoppingToken);
    }
}
```

### Azure App Service

The background job works perfectly on Azure App Service (Linux):
- ✅ Always On setting recommended (prevents app from sleeping)
- ✅ Logs available in Application Insights
- ✅ Automatic restart on failure

**Enable Always On:**
```bash
az webapp config set \
  --resource-group allowancetracker-rg \
  --name allowancetracker-api \
  --always-on true
```

### Monitoring with Application Insights

Track allowance processing metrics:

```csharp
_telemetryClient.TrackMetric("AllowancesPaid", processedCount);
_telemetryClient.TrackMetric("AllowanceErrors", errorCount);
```

Create alerts in Azure:
- Alert if no allowances paid for 7+ days (job might be stuck)
- Alert on high error count

---

## Troubleshooting

### Job Not Running

**Check logs for startup:**
```
[2025-01-15 10:00:00] Information: Weekly Allowance Job started
```

If you don't see this, check:
1. Is `AddHostedService<WeeklyAllowanceJob>()` in Program.cs?
2. Did the API start successfully?
3. Check for exceptions in logs

### Allowances Not Being Paid

**Check eligibility:**
```sql
SELECT
    Id,
    FirstName,
    WeeklyAllowance,
    LastAllowanceDate,
    DATEDIFF(day, LastAllowanceDate, GETUTCDATE()) AS DaysSinceLastPayment,
    CASE
        WHEN WeeklyAllowance <= 0 THEN 'No allowance configured'
        WHEN LastAllowanceDate IS NULL THEN 'Eligible (never paid)'
        WHEN DATEDIFF(day, LastAllowanceDate, GETUTCDATE()) >= 7 THEN 'Eligible'
        ELSE 'Not eligible (paid recently)'
    END AS Status
FROM Children;
```

### Job Running Too Often

If you see multiple "Starting weekly allowance processing" logs within 24 hours:
- Check `_checkInterval` value (should be 24 hours)
- Check if API is restarting frequently

### No Logs Appearing

Ensure logging is configured correctly in `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "AllowanceTracker.BackgroundServices": "Information"
    }
  }
}
```

---

## Summary

The weekly allowance system is:
- ✅ **Fully automatic** - No manual intervention needed
- ✅ **Reliable** - Prevents double payments
- ✅ **Resilient** - Continues on errors
- ✅ **Auditable** - Creates transaction records
- ✅ **Production-ready** - Works on Azure with monitoring

For development, add a manual trigger endpoint for easier testing. For production, the background job handles everything automatically! 🚀
