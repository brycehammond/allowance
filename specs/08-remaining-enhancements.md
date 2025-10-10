# Remaining Enhancements - Post-MVP Features

## Overview
This document outlines the remaining features and enhancements to complete the full vision of the Allowance Tracker application. These build on the solid foundation established in Phases 1-4.

## Current Status (Completed)

### ✅ Phase 1: Foundation
- ASP.NET Core 8.0 Blazor Server project
- EF Core with PostgreSQL
- Identity authentication
- All domain models
- Initial database migration
- **24 tests passing**

### ✅ Phase 2: Transactions & Balance Management
- TransactionService with full CRUD
- Atomic database transactions
- Balance tracking & validation
- Audit trail
- **35 tests passing**

### ✅ Phase 4: Allowance Management & Background Jobs
- AllowanceService with weekly payments
- WeeklyAllowanceJob (IHostedService)
- Prevent double-payment
- Batch processing
- **45 tests passing**

### ✅ Phase 3: Blazor UI (Basic)
- FamilyService for data access
- Dashboard page
- bUnit testing infrastructure
- **45 tests passing**

## Remaining Enhancements

### Enhancement 1: JWT Authentication & API Controllers

**Why**: Enable mobile app integration and third-party access

#### 1.1 JWT Token Service (TDD)

**Test First:**
```csharp
[Fact]
public void GenerateToken_WithValidUser_ReturnsJwtToken()
{
    // Arrange
    var user = new ApplicationUser
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        FirstName = "John",
        LastName = "Doe",
        Role = UserRole.Parent
    };

    // Act
    var token = _jwtService.GenerateToken(user);

    // Assert
    token.Should().NotBeNullOrEmpty();
    var tokenHandler = new JwtSecurityTokenHandler();
    var jwtToken = tokenHandler.ReadJwtToken(token);
    jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == "test@example.com");
}

[Fact]
public void ValidateToken_WithValidToken_ReturnsTrue()
{
    // Arrange
    var user = CreateTestUser();
    var token = _jwtService.GenerateToken(user);

    // Act
    var isValid = _jwtService.ValidateToken(token);

    // Assert
    isValid.Should().BeTrue();
}

[Fact]
public void ValidateToken_WithExpiredToken_ReturnsFalse()
{
    // Arrange
    var expiredToken = GenerateExpiredToken();

    // Act
    var isValid = _jwtService.ValidateToken(expiredToken);

    // Assert
    isValid.Should().BeFalse();
}
```

**Implementation:**
```csharp
public interface IJwtService
{
    string GenerateToken(ApplicationUser user);
    bool ValidateToken(string token);
    ClaimsPrincipal? GetPrincipalFromToken(string token);
}

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT secret key not configured");
        _issuer = _configuration["Jwt:Issuer"] ?? "AllowanceTracker";
        _audience = _configuration["Jwt:Audience"] ?? "AllowanceTracker";
    }

    public string GenerateToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("FamilyId", user.FamilyId?.ToString() ?? "")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool ValidateToken(string token)
    {
        var principal = GetPrincipalFromToken(token);
        return principal != null;
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
```

**Configuration in appsettings.json:**
```json
{
  "Jwt": {
    "SecretKey": "your-secret-key-min-32-chars-long-for-hmac-sha256",
    "Issuer": "AllowanceTracker",
    "Audience": "AllowanceTracker",
    "ExpiryInDays": 1
  }
}
```

**Program.cs Configuration:**
```csharp
// Add JWT authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddScoped<IJwtService, JwtService>();
```

#### 1.2 API Controllers (TDD)

**Authentication Controller Tests:**
```csharp
[Fact]
public async Task Login_WithValidCredentials_ReturnsToken()
{
    // Arrange
    var loginDto = new LoginDto("test@example.com", "Password123!");

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
    result.Should().NotBeNull();
    result!.Token.Should().NotBeNullOrEmpty();
    result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
}

[Fact]
public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
{
    // Arrange
    var loginDto = new LoginDto("test@example.com", "WrongPassword");

    // Act
    var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

**Transactions API Controller Tests:**
```csharp
[Fact]
public async Task GetTransactions_WithValidAuth_ReturnsTransactions()
{
    // Arrange
    var child = await CreateTestChild();
    await CreateTestTransactions(child.Id, 5);

    _client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", _parentToken);

    // Act
    var response = await _client.GetAsync($"/api/children/{child.Id}/transactions");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var transactions = await response.Content.ReadFromJsonAsync<List<TransactionDto>>();
    transactions.Should().HaveCount(5);
}

[Fact]
public async Task CreateTransaction_WithoutAuth_ReturnsUnauthorized()
{
    // Arrange
    var dto = new CreateTransactionDto(Guid.NewGuid(), 10m, TransactionType.Credit, "Test");

    // Act
    var response = await _client.PostAsJsonAsync("/api/transactions", dto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

**Implementation:**
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet("children/{childId}")]
    public async Task<ActionResult<List<Transaction>>> GetChildTransactions(
        Guid childId,
        [FromQuery] int limit = 20)
    {
        var transactions = await _transactionService.GetChildTransactionsAsync(childId, limit);
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<ActionResult<Transaction>> CreateTransaction(CreateTransactionDto dto)
    {
        var transaction = await _transactionService.CreateTransactionAsync(dto);
        return CreatedAtAction(nameof(GetChildTransactions),
            new { childId = dto.ChildId },
            transaction);
    }

    [HttpGet("children/{childId}/balance")]
    public async Task<ActionResult<decimal>> GetBalance(Guid childId)
    {
        var balance = await _transactionService.GetCurrentBalanceAsync(childId);
        return Ok(new { balance });
    }
}
```

### Enhancement 2: Advanced Blazor Components

#### 2.1 ChildCard Component (TDD with bUnit)

**Test First:**
```csharp
[Fact]
public void ChildCard_DisplaysChildInformation()
{
    // Arrange
    var child = new ChildDto(
        Guid.NewGuid(),
        "Jane",
        "Doe",
        WeeklyAllowance: 10.00m,
        CurrentBalance: 25.50m,
        LastAllowanceDate: DateTime.UtcNow.AddDays(-2));

    using var ctx = new TestContext();

    // Act
    var component = ctx.RenderComponent<ChildCard>(parameters => parameters
        .Add(p => p.Child, child));

    // Assert
    component.Find("h5").TextContent.Should().Contain("Jane Doe");
    component.Markup.Should().Contain("$25.50");
    component.Markup.Should().Contain("$10.00");
}

[Fact]
public void ChildCard_ShowTransactionButton_ClickOpensForm()
{
    // Arrange
    var child = CreateTestChild();
    using var ctx = new TestContext();
    var component = ctx.RenderComponent<ChildCard>(parameters => parameters
        .Add(p => p.Child, child));

    // Act
    var button = component.Find("button:contains('Add Transaction')");
    button.Click();

    // Assert
    component.Find(".transaction-form").Should().NotBeNull();
}
```

**Implementation:**
```razor
@using AllowanceTracker.DTOs

<div class="card mb-3">
    <div class="card-body">
        <h5 class="card-title">@Child.FirstName @Child.LastName</h5>

        <div class="balance-display">
            <strong>Balance:</strong>
            <span class="balance-amount">@Child.CurrentBalance.ToString("C")</span>
        </div>

        <div class="text-muted mt-2">
            <small>
                <strong>Weekly Allowance:</strong> @Child.WeeklyAllowance.ToString("C")<br />
                @if (Child.LastAllowanceDate.HasValue)
                {
                    <strong>Last Paid:</strong> @Child.LastAllowanceDate.Value.ToString("yyyy-MM-dd")<br />
                    <strong>Next Payment:</strong> @GetNextAllowanceDate()
                }
                else
                {
                    <em>First allowance pending</em>
                }
            </small>
        </div>

        <div class="mt-3">
            <button class="btn btn-primary btn-sm" @onclick="ToggleTransactionForm">
                Add Transaction
            </button>
            <a href="/children/@Child.Id" class="btn btn-outline-secondary btn-sm">
                Details
            </a>
        </div>

        @if (ShowTransactionForm)
        {
            <div class="transaction-form mt-3">
                <TransactionForm ChildId="@Child.Id"
                               OnSaved="@HandleTransactionSaved"
                               OnCancelled="@(() => ShowTransactionForm = false)" />
            </div>
        }
    </div>
</div>

@code {
    [Parameter, EditorRequired]
    public ChildDto Child { get; set; } = null!;

    [Parameter]
    public EventCallback OnTransactionAdded { get; set; }

    private bool ShowTransactionForm = false;

    private void ToggleTransactionForm()
    {
        ShowTransactionForm = !ShowTransactionForm;
    }

    private async Task HandleTransactionSaved()
    {
        ShowTransactionForm = false;
        await OnTransactionAdded.InvokeAsync();
    }

    private string GetNextAllowanceDate()
    {
        if (!Child.LastAllowanceDate.HasValue)
            return "Pending";

        var nextDate = Child.LastAllowanceDate.Value.AddDays(7);
        return nextDate.ToString("yyyy-MM-dd");
    }
}
```

#### 2.2 TransactionForm Component (TDD with bUnit)

**Test First:**
```csharp
[Fact]
public void TransactionForm_ValidatesRequiredFields()
{
    // Arrange
    using var ctx = new TestContext();
    var component = ctx.RenderComponent<TransactionForm>(parameters => parameters
        .Add(p => p.ChildId, Guid.NewGuid()));

    // Act
    var submitButton = component.Find("button[type='submit']");
    submitButton.Click();

    // Assert
    component.Markup.Should().Contain("Amount is required");
    component.Markup.Should().Contain("Description is required");
}

[Fact]
public async Task TransactionForm_ValidInput_CallsOnSaved()
{
    // Arrange
    var childId = Guid.NewGuid();
    var onSavedCalled = false;

    var mockTransactionService = new Mock<ITransactionService>();
    mockTransactionService
        .Setup(x => x.CreateTransactionAsync(It.IsAny<CreateTransactionDto>()))
        .ReturnsAsync(new Transaction());

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockTransactionService.Object);

    var component = ctx.RenderComponent<TransactionForm>(parameters => parameters
        .Add(p => p.ChildId, childId)
        .Add(p => p.OnSaved, EventCallback.Factory.Create(this, () => onSavedCalled = true)));

    // Act
    component.Find("input[name='amount']").Change("25.00");
    component.Find("select[name='type']").Change("Credit");
    component.Find("input[name='description']").Change("Allowance");
    component.Find("button[type='submit']").Click();

    // Assert (wait for async)
    await Task.Delay(100);
    onSavedCalled.Should().BeTrue();
}
```

**Implementation:**
```razor
@using AllowanceTracker.DTOs
@using AllowanceTracker.Models
@using AllowanceTracker.Services
@inject ITransactionService TransactionService

<EditForm Model="@Model" OnValidSubmit="@HandleSubmit">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <div class="mb-3">
        <label class="form-label">Amount</label>
        <InputNumber class="form-control" @bind-Value="Model.Amount" name="amount" />
        <ValidationMessage For="@(() => Model.Amount)" />
    </div>

    <div class="mb-3">
        <label class="form-label">Type</label>
        <InputSelect class="form-select" @bind-Value="Model.Type" name="type">
            <option value="@TransactionType.Credit">Add Money (Credit)</option>
            <option value="@TransactionType.Debit">Spend Money (Debit)</option>
        </InputSelect>
    </div>

    <div class="mb-3">
        <label class="form-label">Description</label>
        <InputText class="form-control" @bind-Value="Model.Description" name="description" />
        <ValidationMessage For="@(() => Model.Description)" />
    </div>

    <div class="d-flex gap-2">
        <button type="submit" class="btn btn-primary" disabled="@IsSaving">
            @(IsSaving ? "Saving..." : "Save")
        </button>
        <button type="button" class="btn btn-secondary" @onclick="Cancel">
            Cancel
        </button>
    </div>
</EditForm>

@code {
    [Parameter, EditorRequired]
    public Guid ChildId { get; set; }

    [Parameter]
    public EventCallback OnSaved { get; set; }

    [Parameter]
    public EventCallback OnCancelled { get; set; }

    private TransactionFormModel Model = new();
    private bool IsSaving = false;

    private async Task HandleSubmit()
    {
        IsSaving = true;
        try
        {
            var dto = new CreateTransactionDto(
                ChildId,
                Model.Amount,
                Model.Type,
                Model.Description);

            await TransactionService.CreateTransactionAsync(dto);
            await OnSaved.InvokeAsync();
        }
        finally
        {
            IsSaving = false;
        }
    }

    private async Task Cancel()
    {
        await OnCancelled.InvokeAsync();
    }

    public class TransactionFormModel
    {
        [Required]
        [Range(0.01, 10000, ErrorMessage = "Amount must be between $0.01 and $10,000")]
        public decimal Amount { get; set; }

        public TransactionType Type { get; set; } = TransactionType.Credit;

        [Required]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Description must be between 3 and 500 characters")]
        public string Description { get; set; } = string.Empty;
    }
}
```

### Enhancement 3: Real-Time SignalR Updates

#### 3.1 FamilyHub for Real-Time Communication

**Test First:**
```csharp
[Fact]
public async Task TransactionCreated_BroadcastsToFamilyMembers()
{
    // Arrange
    var hubConnection = new HubConnectionBuilder()
        .WithUrl($"{_server.BaseAddress}familyHub", options =>
        {
            options.HttpMessageHandlerFactory = _ => _server.CreateHandler();
            options.AccessTokenProvider = () => Task.FromResult(_parentToken)!;
        })
        .Build();

    var receivedTransaction = false;
    hubConnection.On<Guid>("TransactionCreated", (childId) =>
    {
        receivedTransaction = true;
    });

    await hubConnection.StartAsync();

    // Act
    var child = await CreateTestChild();
    var dto = new CreateTransactionDto(child.Id, 10m, TransactionType.Credit, "Test");
    await _transactionService.CreateTransactionAsync(dto);

    // Assert
    await Task.Delay(500); // Wait for SignalR broadcast
    receivedTransaction.Should().BeTrue();
}
```

**Implementation:**
```csharp
public class FamilyHub : Hub
{
    private readonly ICurrentUserService _currentUser;
    private readonly AllowanceContext _context;

    public FamilyHub(ICurrentUserService currentUser, AllowanceContext context)
    {
        _currentUser = currentUser;
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var user = await _context.Users.FindAsync(_currentUser.UserId);

        if (user?.FamilyId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"family-{user.FamilyId}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var user = await _context.Users.FindAsync(_currentUser.UserId);

        if (user?.FamilyId != null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"family-{user.FamilyId}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

**Update TransactionService to broadcast:**
```csharp
public class TransactionService : ITransactionService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IHubContext<FamilyHub>? _hubContext;

    public TransactionService(
        AllowanceContext context,
        ICurrentUserService currentUser,
        IHubContext<FamilyHub>? hubContext = null)
    {
        _context = context;
        _currentUser = currentUser;
        _hubContext = hubContext;
    }

    public async Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto)
    {
        // ... existing code ...

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        // Broadcast to family members
        if (_hubContext != null)
        {
            var child = await _context.Children.Include(c => c.Family).FirstAsync(c => c.Id == dto.ChildId);
            await _hubContext.Clients
                .Group($"family-{child.FamilyId}")
                .SendAsync("TransactionCreated", dto.ChildId);
        }

        return transaction;
    }
}
```

**Dashboard Component with Real-Time Updates:**
```razor
@page "/dashboard"
@using Microsoft.AspNetCore.SignalR.Client
@inject NavigationManager Navigation
@implements IAsyncDisposable

<h1>Family Dashboard</h1>

@* ... existing dashboard code ... *@

@code {
    private HubConnection? _hubConnection;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Setup SignalR connection
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/familyHub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<Guid>("TransactionCreated", async (childId) =>
        {
            // Refresh data when transaction created
            await RefreshData();
        });

        await _hubConnection.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }
    }
}
```

### Enhancement 4: Deployment Configuration

#### 4.1 Docker Support

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/AllowanceTracker/AllowanceTracker.csproj", "AllowanceTracker/"]
RUN dotnet restore "AllowanceTracker/AllowanceTracker.csproj"
COPY src/AllowanceTracker/. AllowanceTracker/
WORKDIR "/src/AllowanceTracker"
RUN dotnet build "AllowanceTracker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AllowanceTracker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AllowanceTracker.dll"]
```

**docker-compose.yml:**
```yaml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "5000:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Host=db;Database=allowance_tracker;Username=postgres;Password=${DB_PASSWORD}
      - Jwt__SecretKey=${JWT_SECRET_KEY}
    depends_on:
      - db

  db:
    image: postgres:16
    environment:
      - POSTGRES_DB=allowance_tracker
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

#### 4.2 Railway Deployment

**railway.json:**
```json
{
  "build": {
    "builder": "NIXPACKS",
    "buildCommand": "dotnet restore && dotnet publish -c Release -o out"
  },
  "deploy": {
    "startCommand": "cd out && dotnet AllowanceTracker.dll",
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
```

**Environment Variables:**
```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
Jwt__SecretKey=${{JWT_SECRET_KEY}}
ASPNETCORE_URLS=http://0.0.0.0:${{PORT}}
```

#### 4.3 GitHub Actions CI/CD

**.github/workflows/ci.yml:**
```yaml
name: CI/CD

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x

    - name: Restore dependencies
      run: dotnet restore

    - name: Build
      run: dotnet build --no-restore

    - name: Test
      run: dotnet test --no-build --verbosity normal

  deploy:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'

    steps:
    - uses: actions/checkout@v3

    - name: Deploy to Railway
      uses: bervProject/railway-deploy@main
      with:
        railway_token: ${{ secrets.RAILWAY_TOKEN }}
        service: allowance-tracker
```

## Implementation Priority

1. **High Priority** (Core functionality):
   - JWT Authentication & API Controllers
   - TransactionForm component
   - ChildCard component

2. **Medium Priority** (Enhanced UX):
   - Real-time SignalR updates
   - Comprehensive bUnit tests
   - Additional Blazor components

3. **Low Priority** (DevOps):
   - Docker configuration
   - Railway deployment
   - GitHub Actions CI/CD

## Testing Strategy for Enhancements

All enhancements should follow strict TDD:
1. Write failing tests first (RED)
2. Implement minimal code to pass (GREEN)
3. Refactor and improve (REFACTOR)
4. Target >90% code coverage for critical paths

## Success Criteria

- [ ] JWT authentication with token validation
- [ ] REST API endpoints for mobile access
- [ ] Interactive transaction form with validation
- [ ] Reusable child card component
- [ ] Real-time balance updates via SignalR
- [ ] All new features have comprehensive tests
- [ ] Docker containerization working
- [ ] One-click deployment to Railway
- [ ] CI/CD pipeline with automated testing
- [ ] All tests passing (target: >55 tests)

## Estimated Effort

- Enhancement 1 (JWT & API): 8-12 hours
- Enhancement 2 (Blazor Components): 6-8 hours
- Enhancement 3 (SignalR): 4-6 hours
- Enhancement 4 (Deployment): 3-4 hours

**Total: 21-30 hours** for complete implementation
