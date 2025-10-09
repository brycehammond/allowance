# Blazor UI Specification - Simple MVP

## Overview
Simple, real-time web interface using Blazor Server for immediate feedback and easy development. No JavaScript needed, full C# stack.

## Why Blazor Server for MVP?
- **Real-time by default**: SignalR built-in, instant UI updates
- **Simple deployment**: Single server application
- **Fast development**: All C#, no API/frontend separation
- **Performance**: Server-side rendering, minimal client resources
- **Perfect for family app**: Known user base, low latency requirements

## Component Architecture

### Layout Structure
```
Shared/
├── MainLayout.razor       # App shell with navigation
├── NavMenu.razor          # Side navigation menu
└── LoginDisplay.razor     # User info and logout

Pages/
├── Index.razor            # Landing/redirect page
├── Login.razor           # Authentication page
├── Dashboard.razor       # Parent/child dashboard
├── Children/
│   ├── List.razor       # Manage children (parents)
│   ├── Details.razor    # Child details and balance
│   └── Edit.razor       # Edit child settings
├── Transactions/
│   ├── Create.razor     # Add transaction form
│   └── History.razor    # Transaction list
└── WishList/
    ├── Index.razor      # Wish list items
    └── Add.razor        # Add wish list item

Components/
├── ChildCard.razor       # Reusable child info card
├── TransactionForm.razor # Transaction input component
├── BalanceDisplay.razor  # Real-time balance widget
├── QuickActions.razor    # Common action buttons
└── NotificationAlert.razor # Success/error messages
```

## Core Pages

### Dashboard (Dashboard.razor)
```razor
@page "/dashboard"
@attribute [Authorize]

<PageTitle>Dashboard - Allowance Tracker</PageTitle>

@if (IsParent)
{
    <h1>Family Dashboard</h1>
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        @foreach (var child in Children)
        {
            <ChildCard Child="@child" OnTransactionAdded="@RefreshData" />
        }
    </div>
    <QuickActions />
}
else
{
    <h1>My Allowance</h1>
    <BalanceDisplay Balance="@CurrentBalance" />
    <RecentTransactions Transactions="@Transactions" />
    <WishListSummary Items="@WishListItems" />
}

@code {
    [Inject] private IFamilyService FamilyService { get; set; }
    [Inject] private AuthenticationStateProvider AuthProvider { get; set; }

    private bool IsParent;
    private List<Child> Children = new();
    private decimal CurrentBalance;
    private List<Transaction> Transactions = new();
    private List<WishListItem> WishListItems = new();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthProvider.GetAuthenticationStateAsync();
        var user = await UserService.GetUserAsync(authState.User);
        IsParent = user.Role == UserRole.Parent;

        if (IsParent)
        {
            Children = await FamilyService.GetChildrenAsync();
        }
        else
        {
            var childData = await FamilyService.GetChildDataAsync(user.Id);
            CurrentBalance = childData.CurrentBalance;
            Transactions = childData.RecentTransactions;
            WishListItems = childData.WishListItems;
        }
    }

    private async Task RefreshData()
    {
        await OnInitializedAsync();
        StateHasChanged();
    }
}
```

### Child Card Component (Components/ChildCard.razor)
```razor
<div class="card bg-base-100 shadow-xl">
    <div class="card-body">
        <h2 class="card-title">@Child.FirstName</h2>

        <BalanceDisplay Balance="@Child.CurrentBalance" />

        <div class="text-sm text-gray-600">
            Weekly Allowance: @Child.WeeklyAllowance.ToString("C")
            <br />
            Next Allowance: @GetNextAllowanceDay()
        </div>

        <div class="card-actions justify-end mt-4">
            <button class="btn btn-sm btn-primary" @onclick="ShowTransactionForm">
                Add Transaction
            </button>
            <a href="/children/@Child.Id" class="btn btn-sm btn-outline">
                Details
            </a>
        </div>

        @if (ShowForm)
        {
            <TransactionForm ChildId="@Child.Id"
                           OnSaved="@HandleTransactionSaved"
                           OnCancelled="@(() => ShowForm = false)" />
        }
    </div>
</div>

@code {
    [Parameter] public Child Child { get; set; }
    [Parameter] public EventCallback OnTransactionAdded { get; set; }

    private bool ShowForm = false;

    private void ShowTransactionForm() => ShowForm = true;

    private async Task HandleTransactionSaved()
    {
        ShowForm = false;
        await OnTransactionAdded.InvokeAsync();
    }

    private string GetNextAllowanceDay()
    {
        var daysUntil = ((int)Child.AllowanceDay - (int)DateTime.Now.DayOfWeek + 7) % 7;
        return daysUntil == 0 ? "Today" : $"In {daysUntil} days";
    }
}
```

### Transaction Form Component (Components/TransactionForm.razor)
```razor
<EditForm Model="@Model" OnValidSubmit="@HandleSubmit" class="mt-4 p-4 border rounded">
    <DataAnnotationsValidator />

    <div class="form-control">
        <label class="label">
            <span class="label-text">Amount</span>
        </label>
        <InputNumber @bind-Value="Model.Amount" class="input input-bordered"
                     placeholder="0.00" step="0.01" />
        <ValidationMessage For="@(() => Model.Amount)" />
    </div>

    <div class="form-control mt-2">
        <label class="label">
            <span class="label-text">Type</span>
        </label>
        <InputSelect @bind-Value="Model.Type" class="select select-bordered">
            <option value="">Select type...</option>
            <option value="@TransactionType.Credit">Add Money</option>
            <option value="@TransactionType.Debit">Spend Money</option>
        </InputSelect>
        <ValidationMessage For="@(() => Model.Type)" />
    </div>

    <div class="form-control mt-2">
        <label class="label">
            <span class="label-text">Description</span>
        </label>
        <InputText @bind-Value="Model.Description" class="input input-bordered"
                   placeholder="What's this for?" />
        <ValidationMessage For="@(() => Model.Description)" />
    </div>

    <div class="mt-4 flex gap-2">
        <button type="submit" class="btn btn-primary" disabled="@IsProcessing">
            @if (IsProcessing)
            {
                <span class="loading loading-spinner"></span>
            }
            Save Transaction
        </button>
        <button type="button" class="btn btn-ghost" @onclick="OnCancelled">
            Cancel
        </button>
    </div>
</EditForm>

@code {
    [Parameter] public Guid ChildId { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }
    [Inject] private ITransactionService TransactionService { get; set; }
    [Inject] private ILogger<TransactionForm> Logger { get; set; }

    private TransactionFormModel Model = new();
    private bool IsProcessing = false;

    protected override void OnInitialized()
    {
        Model = new TransactionFormModel { ChildId = ChildId };
    }

    private async Task HandleSubmit()
    {
        IsProcessing = true;

        try
        {
            await TransactionService.CreateTransactionAsync(new CreateTransactionDto(
                Model.ChildId,
                Model.Amount,
                Model.Type,
                Model.Description
            ));

            await OnSaved.InvokeAsync();
        }
        catch (InsufficientFundsException ex)
        {
            // Show error notification
            Logger.LogWarning(ex, "Insufficient funds");
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private class TransactionFormModel
    {
        public Guid ChildId { get; set; }

        [Required]
        [Range(0.01, 1000)]
        public decimal Amount { get; set; }

        [Required]
        public TransactionType Type { get; set; }

        [Required]
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
    }
}
```

### Real-time Balance Display (Components/BalanceDisplay.razor)
```razor
@implements IDisposable
@inject NavigationManager Navigation

<div class="stat">
    <div class="stat-title">Current Balance</div>
    <div class="stat-value text-primary">@Balance.ToString("C")</div>
    @if (LastUpdated != null)
    {
        <div class="stat-desc">Updated @GetTimeAgo()</div>
    }
</div>

@code {
    [Parameter] public decimal Balance { get; set; }
    [Parameter] public Guid? ChildId { get; set; }
    [CascadingParameter] private HubConnection? HubConnection { get; set; }

    private DateTime? LastUpdated;
    private Timer? _timer;

    protected override async Task OnInitializedAsync()
    {
        if (HubConnection is not null && ChildId.HasValue)
        {
            HubConnection.On<Guid, decimal>($"BalanceUpdated", (childId, newBalance) =>
            {
                if (childId == ChildId)
                {
                    Balance = newBalance;
                    LastUpdated = DateTime.Now;
                    InvokeAsync(StateHasChanged);
                }
            });

            if (HubConnection.State == HubConnectionState.Disconnected)
            {
                await HubConnection.StartAsync();
            }

            await HubConnection.SendAsync("SubscribeToChild", ChildId.Value);
        }

        // Update relative time display
        _timer = new Timer(_ => InvokeAsync(StateHasChanged), null,
                          TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    private string GetTimeAgo()
    {
        if (LastUpdated == null) return "";
        var diff = DateTime.Now - LastUpdated.Value;

        return diff.TotalSeconds < 60 ? "just now" :
               diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes}m ago" :
               diff.TotalHours < 24 ? $"{(int)diff.TotalHours}h ago" :
               $"{(int)diff.TotalDays}d ago";
    }

    public void Dispose()
    {
        _timer?.Dispose();
        if (HubConnection is not null && ChildId.HasValue)
        {
            _ = HubConnection.SendAsync("UnsubscribeFromChild", ChildId.Value);
        }
    }
}
```

## Form Patterns

### Validation
```csharp
// Use data annotations on models
public class CreateChildModel
{
    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "Name too long")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Range(0, 1000, ErrorMessage = "Allowance must be between $0 and $1000")]
    public decimal WeeklyAllowance { get; set; }

    [Required]
    [Range(0, 6, ErrorMessage = "Please select a day")]
    public DayOfWeek AllowanceDay { get; set; }
}
```

### Form Handling
```razor
<EditForm Model="@Model" OnValidSubmit="@HandleValidSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <!-- Form fields -->

    <button type="submit" disabled="@IsProcessing">
        @if (IsProcessing)
        {
            <span class="loading loading-spinner"></span>
            <span>Saving...</span>
        }
        else
        {
            <span>Save</span>
        }
    </button>
</EditForm>

@code {
    private bool IsProcessing;

    private async Task HandleValidSubmit()
    {
        IsProcessing = true;
        StateHasChanged(); // Show loading state immediately

        try
        {
            await SaveDataAsync();
            NavigationManager.NavigateTo("/success");
        }
        catch (Exception ex)
        {
            // Show error
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
```

## Real-time Features

### SignalR Hub Setup
```csharp
public class AllowanceHub : Hub
{
    private readonly IFamilyService _familyService;

    public async Task SubscribeToChild(Guid childId)
    {
        var userId = Context.UserIdentifier;
        if (await _familyService.UserCanAccessChild(userId, childId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"child-{childId}");
        }
    }

    public async Task UnsubscribeFromChild(Guid childId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"child-{childId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Cleanup
        await base.OnDisconnectedAsync(exception);
    }
}
```

### Notification Service
```csharp
public interface INotificationService
{
    Task NotifyBalanceUpdate(Guid childId, decimal newBalance);
    Task NotifyTransactionAdded(Guid childId, Transaction transaction);
    Task NotifyAllowanceProcessed(Guid familyId);
}

public class SignalRNotificationService : INotificationService
{
    private readonly IHubContext<AllowanceHub> _hubContext;

    public async Task NotifyBalanceUpdate(Guid childId, decimal newBalance)
    {
        await _hubContext.Clients.Group($"child-{childId}")
            .SendAsync("BalanceUpdated", childId, newBalance);
    }
}
```

## Styling with Tailwind CSS

### Configuration (tailwind.config.js)
```javascript
module.exports = {
  content: ['./**/*.{razor,html,cshtml}'],
  theme: {
    extend: {
      colors: {
        'primary': '#4F46E5',
        'secondary': '#10B981',
        'accent': '#F59E0B',
      }
    }
  },
  plugins: [require("daisyui")],
  daisyui: {
    themes: ["light", "dark", "cupcake"],
  }
}
```

### Common Components
```css
/* app.css */
@layer components {
  .card {
    @apply bg-white rounded-lg shadow-md p-6 mb-4;
  }

  .btn-primary {
    @apply bg-primary text-white px-4 py-2 rounded hover:bg-primary-dark;
  }

  .form-control {
    @apply mb-4;
  }

  .input {
    @apply w-full px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-primary;
  }

  .label {
    @apply block text-sm font-medium text-gray-700 mb-1;
  }
}
```

## State Management

### Cascading Authentication State
```razor
<!-- App.razor -->
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData"
                               DefaultLayout="@typeof(MainLayout)">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
                <Authorizing>
                    <LoadingSpinner />
                </Authorizing>
            </AuthorizeRouteView>
        </Found>
        <NotFound>
            <PageTitle>Not found</PageTitle>
            <LayoutView Layout="@typeof(MainLayout)">
                <p>Sorry, there's nothing at this address.</p>
            </LayoutView>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

### Scoped Services
```csharp
// Program.cs
builder.Services.AddScoped<IFamilyService, FamilyService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IWishListService, WishListService>();

// Circuit-scoped for SignalR
builder.Services.AddScoped<HubConnection>(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    return new HubConnectionBuilder()
        .WithUrl(navigationManager.ToAbsoluteUri("/allowancehub"))
        .WithAutomaticReconnect()
        .Build();
});
```

## Performance Optimizations

### Component Virtualization
```razor
<!-- For long lists -->
<Virtualize Items="@Transactions" Context="transaction">
    <ItemContent>
        <TransactionRow Transaction="@transaction" />
    </ItemContent>
    <Placeholder>
        <div class="skeleton h-12 w-full mb-2"></div>
    </Placeholder>
</Virtualize>
```

### Lazy Loading
```razor
@if (ShowDetails)
{
    <ChildDetails ChildId="@ChildId" />
}
else
{
    <button @onclick="() => ShowDetails = true">Show Details</button>
}
```

### Prerendering Control
```razor
<!-- Disable prerendering for interactive components -->
<component type="typeof(TransactionForm)" render-mode="Server" />

<!-- Enable prerendering for static content -->
<component type="typeof(AboutPage)" render-mode="ServerPrerendered" />
```

## Error Handling

### Global Error Boundary
```razor
<!-- Shared/ErrorBoundary.razor -->
<CascadingValue Value="this">
    @ChildContent
</CascadingValue>

@code {
    [Parameter] public RenderFragment ChildContent { get; set; }

    public void HandleError(Exception ex)
    {
        Logger.LogError(ex, "An error occurred");
        // Show user-friendly error message
    }
}
```

### Component Error Handling
```razor
@try
{
    @if (Data != null)
    {
        <DataDisplay Data="@Data" />
    }
}
catch (Exception ex)
{
    <div class="alert alert-error">
        <span>Something went wrong. Please try again.</span>
    </div>
    Logger.LogError(ex, "Error in component");
}
```

## Mobile Responsiveness

### Responsive Grid
```razor
<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
    @foreach (var item in Items)
    {
        <ItemCard Item="@item" />
    }
</div>
```

### Mobile Navigation
```razor
<!-- NavMenu.razor -->
<div class="drawer lg:drawer-open">
    <input id="drawer-toggle" type="checkbox" class="drawer-toggle" />
    <div class="drawer-content">
        <!-- Page content -->
        @Body
    </div>
    <div class="drawer-side">
        <label for="drawer-toggle" class="drawer-overlay"></label>
        <ul class="menu p-4 w-80 min-h-full bg-base-200">
            <!-- Navigation items -->
        </ul>
    </div>
</div>
```

## Testing Blazor Components

### Component Test Example
```csharp
[Fact]
public void ChildCard_DisplaysCorrectBalance()
{
    // Arrange
    using var ctx = new TestContext();
    var child = new Child
    {
        FirstName = "Alice",
        CurrentBalance = 125.50m
    };

    // Act
    var component = ctx.RenderComponent<ChildCard>(parameters => parameters
        .Add(p => p.Child, child));

    // Assert
    component.Find(".balance").TextContent.Should().Contain("$125.50");
    component.Find(".card-title").TextContent.Should().Contain("Alice");
}
```

## Deployment Considerations

### Configuration
```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapBlazorHub();
app.MapHub<AllowanceHub>("/allowancehub");
app.MapFallbackToPage("/_Host");
```

### Connection Resilience
```javascript
// Blazor reconnection UI
<script>
    Blazor.start({
        reconnectionHandler: {
            onConnectionDown: (options, error) => console.log("Connection down"),
            onConnectionUp: () => console.log("Connection up")
        }
    });
</script>
```

## Security Considerations

### Authorization
```razor
@page "/admin"
@attribute [Authorize(Roles = "Parent")]

<!-- Component code -->
```

### Input Sanitization
```csharp
// Always validate and sanitize user input
public string SanitizeInput(string input)
{
    return System.Web.HttpUtility.HtmlEncode(input);
}
```

### CSRF Protection
Built into Blazor Server by default through SignalR tokens.

## Next Steps

1. Create shared component library
2. Implement theme switching (light/dark)
3. Add PWA capabilities
4. Optimize for slow connections
5. Add offline support (future)