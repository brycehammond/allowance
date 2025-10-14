# Adding Real-Time Updates with SignalR

Currently, the application does NOT have real-time updates. Changes are only reflected locally in the browser that made them. Other users must refresh the page to see updates.

This guide explains how to add SignalR for real-time synchronization across all connected clients.

---

## Why Add SignalR?

### Without SignalR (Current State)
- ❌ Parent A adds a transaction → Parent B must refresh to see it
- ❌ Child checks balance → Must refresh to see allowance payment
- ❌ Multiple devices don't sync automatically
- ✅ Simpler architecture
- ✅ Easier to deploy (no persistent connections)
- ✅ Lower server resources

### With SignalR (Real-Time)
- ✅ Parent A adds transaction → Parent B sees it instantly
- ✅ Weekly allowance pays → All users see updated balance
- ✅ Multiple devices stay in sync
- ✅ Better user experience for families
- ❌ More complex architecture
- ❌ Requires WebSocket support
- ❌ Higher server resources (persistent connections)

---

## Implementation Steps

### Step 1: Add SignalR to .NET API

#### 1.1 Install Package

```bash
cd src/AllowanceTracker
dotnet add package Microsoft.AspNetCore.SignalR
```

#### 1.2 Create Hub

Create `src/AllowanceTracker/Hubs/FamilyHub.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace AllowanceTracker.Hubs;

[Authorize]
public class FamilyHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        // Get user's family ID from claims
        var familyId = Context.User?.FindFirst("FamilyId")?.Value;

        if (!string.IsNullOrEmpty(familyId))
        {
            // Add to family group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"family_{familyId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var familyId = Context.User?.FindFirst("FamilyId")?.Value;

        if (!string.IsNullOrEmpty(familyId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"family_{familyId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

#### 1.3 Register SignalR in Program.cs

In `src/AllowanceTracker/Program.cs`:

```csharp
// Add SignalR
builder.Services.AddSignalR();

// After app.MapControllers():
app.MapHub<FamilyHub>("/hubs/family");
```

#### 1.4 Update CORS for SignalR

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Required for SignalR
    });
});
```

#### 1.5 Broadcast Events from Services

Update `TransactionService` to broadcast changes:

```csharp
using Microsoft.AspNetCore.SignalR;
using AllowanceTracker.Hubs;

public class TransactionService : ITransactionService
{
    private readonly AllowanceContext _context;
    private readonly IHubContext<FamilyHub> _hubContext;

    public TransactionService(
        AllowanceContext context,
        IHubContext<FamilyHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<TransactionDto> CreateTransactionAsync(
        CreateTransactionDto dto,
        Guid currentUserId)
    {
        // ... existing transaction creation code ...

        // Broadcast to family group
        await _hubContext.Clients
            .Group($"family_{child.FamilyId}")
            .SendAsync("TransactionCreated", new
            {
                ChildId = transaction.ChildId,
                Transaction = transaction,
                NewBalance = child.CurrentBalance
            });

        return transactionDto;
    }
}
```

### Step 2: Add SignalR to React Frontend

#### 2.1 Install Package

```bash
cd web
npm install @microsoft/signalr
```

#### 2.2 Create SignalR Service

Create `web/src/services/signalr.ts`:

```typescript
import * as signalR from '@microsoft/signalr';

class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private isConnecting = false;

  async start(token: string): Promise<void> {
    if (this.connection || this.isConnecting) {
      return;
    }

    this.isConnecting = true;

    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl(`${import.meta.env.VITE_API_URL}/hubs/family`, {
          accessTokenFactory: () => token,
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Handle reconnection
      this.connection.onreconnecting(() => {
        console.log('SignalR reconnecting...');
      });

      this.connection.onreconnected(() => {
        console.log('SignalR reconnected');
      });

      this.connection.onclose(() => {
        console.log('SignalR disconnected');
      });

      await this.connection.start();
      console.log('SignalR connected');
    } catch (error) {
      console.error('SignalR connection error:', error);
    } finally {
      this.isConnecting = false;
    }
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  on(eventName: string, callback: (...args: any[]) => void): void {
    this.connection?.on(eventName, callback);
  }

  off(eventName: string, callback?: (...args: any[]) => void): void {
    if (callback) {
      this.connection?.off(eventName, callback);
    } else {
      this.connection?.off(eventName);
    }
  }

  isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

export const signalRService = new SignalRService();
```

#### 2.3 Update AuthContext

Modify `web/src/contexts/AuthContext.tsx`:

```typescript
import { signalRService } from '../services/signalr';

// In login function:
const login = async (email: string, password: string) => {
  const response = await authApi.login({ email, password });
  localStorage.setItem('token', response.token);
  localStorage.setItem('user', JSON.stringify(response.user));
  setUser(response.user);

  // Start SignalR connection
  await signalRService.start(response.token);
};

// In logout function:
const logout = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('user');
  setUser(null);

  // Stop SignalR connection
  signalRService.stop();
};
```

#### 2.4 Listen for Events in Components

Update `web/src/pages/ChildDetail.tsx`:

```typescript
import { useEffect } from 'react';
import { signalRService } from '../services/signalr';

export const ChildDetail: React.FC = () => {
  // ... existing code ...

  useEffect(() => {
    // Listen for transaction events
    const handleTransactionCreated = (data: any) => {
      if (data.childId === childId) {
        // Refresh transactions
        loadTransactions();
        // Update balance
        setBalance(data.newBalance);
      }
    };

    signalRService.on('TransactionCreated', handleTransactionCreated);

    // Cleanup
    return () => {
      signalRService.off('TransactionCreated', handleTransactionCreated);
    };
  }, [childId]);

  // ... rest of component ...
};
```

---

## Event Types to Broadcast

Here are the key events you should broadcast:

### 1. TransactionCreated
```typescript
{
  childId: string;
  transaction: Transaction;
  newBalance: number;
}
```

**When:** After creating a transaction
**Who receives:** All family members

### 2. AllowancePaid
```typescript
{
  childId: string;
  amount: number;
  newBalance: number;
}
```

**When:** Background job pays weekly allowance
**Who receives:** All family members

### 3. WishListItemAdded
```typescript
{
  childId: string;
  item: WishListItem;
}
```

**When:** Child adds wish list item
**Who receives:** All family members

### 4. WishListItemPurchased
```typescript
{
  childId: string;
  itemId: string;
  purchasedBy: string;
}
```

**When:** Parent marks item as purchased
**Who receives:** All family members

### 5. SavingsTransactionCreated
```typescript
{
  childId: string;
  transaction: SavingsTransaction;
  newSavingsBalance: number;
}
```

**When:** Deposit/withdrawal to savings
**Who receives:** All family members

---

## Testing SignalR Locally

### 1. Test Connection

Open browser console when logged in:
```javascript
// Check if connected
console.log('SignalR connected:', signalRService.isConnected());
```

### 2. Test with Multiple Browsers

1. Open http://localhost:5173 in **Chrome**
2. Login as Parent
3. Open http://localhost:5173 in **Firefox** (private/incognito)
4. Login as same Parent
5. Add transaction in Chrome
6. Should see update in Firefox **without refresh**

### 3. Test Reconnection

1. Stop API (`Ctrl+C`)
2. Should see "reconnecting" in console
3. Start API again
4. Should see "reconnected" in console

---

## Production Considerations

### Azure App Service

SignalR works on Azure App Service but requires:

1. **Enable Web Sockets:**
   ```bash
   az webapp config set \
     --resource-group allowancetracker-rg \
     --name allowancetracker-api \
     --web-sockets-enabled true
   ```

2. **Use Sticky Sessions (ARR Affinity):**
   ```bash
   az webapp update \
     --resource-group allowancetracker-rg \
     --name allowancetracker-api \
     --client-affinity-enabled true
   ```

### Azure SignalR Service (Recommended for Scale)

For production with multiple servers, use Azure SignalR Service:

```bash
# Create Azure SignalR Service
az signalr create \
  --name allowancetracker-signalr \
  --resource-group allowancetracker-rg \
  --sku Standard_S1 \
  --service-mode Default

# Get connection string
az signalr key list \
  --name allowancetracker-signalr \
  --resource-group allowancetracker-rg \
  --query primaryConnectionString \
  --output tsv
```

**Update appsettings.json:**
```json
{
  "Azure": {
    "SignalR": {
      "ConnectionString": "<connection-string>"
    }
  }
}
```

**Update Program.cs:**
```csharp
builder.Services.AddSignalR()
    .AddAzureSignalR(options =>
    {
        options.ConnectionString = builder.Configuration["Azure:SignalR:ConnectionString"];
    });
```

**Benefits:**
- ✅ Scales across multiple App Service instances
- ✅ Handles connection management
- ✅ Better reliability
- ✅ Built-in metrics and monitoring
- 💰 Cost: ~$50/month (Standard tier)

---

## Performance Impact

### Without SignalR (Current)
- **Memory per user:** ~5 MB
- **CPU per request:** Low
- **Network:** HTTP only
- **Concurrent users:** 1000s

### With SignalR
- **Memory per connection:** ~10-15 MB
- **CPU per connection:** Medium (for message broadcasting)
- **Network:** WebSocket (persistent)
- **Concurrent users:** 100s (per server, thousands with Azure SignalR)

### Recommendations
- For **< 100 concurrent users**: Self-hosted SignalR on App Service is fine
- For **> 100 concurrent users**: Use Azure SignalR Service
- For **> 1000 concurrent users**: Use Azure SignalR Service with Premium tier

---

## Alternative: Polling

If SignalR is too complex, you can implement simple polling:

```typescript
// In your component
useEffect(() => {
  const interval = setInterval(() => {
    // Refresh data every 30 seconds
    loadTransactions();
    loadBalance();
  }, 30000);

  return () => clearInterval(interval);
}, []);
```

**Pros:**
- ✅ Simple to implement
- ✅ Works everywhere (no WebSocket requirement)
- ✅ Lower server resources

**Cons:**
- ❌ Not truly real-time (30 second delay)
- ❌ More API calls
- ❌ Wasted requests when no changes

---

## Decision Matrix

| Requirement | Recommendation |
|-------------|----------------|
| Single user at a time | **No SignalR needed** |
| Small family (2-4 users) | **Polling (30s interval)** |
| Multiple active users | **SignalR (self-hosted)** |
| High traffic (100+ concurrent) | **Azure SignalR Service** |
| Budget-conscious | **Polling or no real-time** |
| Best UX | **SignalR** |

---

## Summary

The application currently works great **without real-time updates** for:
- Single user scenarios
- Low-frequency updates
- Budget-conscious deployments

Add SignalR if you need:
- Instant synchronization across devices
- Multiple family members active simultaneously
- Best possible user experience

The architecture supports both approaches - you can add SignalR later without major refactoring!
