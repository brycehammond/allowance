# Allowance Tracker - .NET Development Guide for Claude

## 🎯 Project Context
A simple MVP allowance tracking application built with ASP.NET Core 8.0 and Blazor Server. Helps parents manage children's allowances, track spending, and teach financial responsibility through real-time web interface and API for mobile apps.

**License**: MIT License (see LICENSE file)

## 🏗️ Current Architecture (MVP Focus)

### Technology Stack
- **Backend**: ASP.NET Core 8.0
- **Frontend**: Blazor Server (real-time UI, no JavaScript)
- **Database**: PostgreSQL with Entity Framework Core 8
- **Authentication**: ASP.NET Core Identity + JWT (for API)
- **Real-time**: SignalR (built into Blazor Server)
- **Background Jobs**: IHostedService (built-in, no external dependencies)
- **Testing**: xUnit, FluentAssertions, Moq, Bogus
- **Deployment**: Railway or Azure App Service

### Key Design Decisions
1. **Blazor Server**: Real-time by default, single language (C#), fast development
2. **Entity Framework Core**: Code-first migrations, LINQ queries, good enough performance
3. **Built-in DI**: Microsoft.Extensions.DependencyInjection - simple and effective
4. **PostgreSQL**: Free tier on Railway, works great with EF Core
5. **Minimal dependencies**: Faster to build, easier to maintain

## 📁 Project Structure (Simple MVP)

```
AllowanceTracker/
├── specs/                        # Detailed specifications (READ THESE FIRST!)
│   ├── 01-overview.md           # System overview and goals
│   ├── 02-database-schema.md    # EF Core entity models
│   ├── 03-api-specification.md  # API endpoints documentation
│   ├── 04-implementation-phases.md # TDD development roadmap
│   ├── 05-testing-strategy.md   # xUnit testing approach
│   ├── 06-tdd-best-practices.md # TDD patterns for .NET
│   └── 07-blazor-ui-specification.md # Blazor component architecture
├── Data/                        # EF Core DbContext
│   ├── AllowanceContext.cs
│   └── Migrations/
├── Models/                      # Domain entities
│   ├── ApplicationUser.cs
│   ├── Family.cs
│   ├── Child.cs
│   └── Transaction.cs
├── Services/                    # Business logic
│   ├── FamilyService.cs
│   ├── TransactionService.cs
│   └── AllowanceService.cs
├── Pages/                       # Blazor pages
│   ├── Index.razor
│   ├── Dashboard.razor
│   └── Children/
├── Shared/                      # Blazor components
│   ├── MainLayout.razor
│   └── Components/
├── Api/                        # API controllers
│   └── V1/
│       └── TransactionsController.cs
├── Program.cs                  # Startup and DI
└── AllowanceTracker.Tests/    # xUnit tests
    ├── Models/
    ├── Services/
    └── Api/
```

## 🚀 Quick Start

### Prerequisites Check
```bash
# Check .NET SDK version (should be 8.0+)
dotnet --version

# Check PostgreSQL is running
pg_isready

# Check Node.js for Tailwind CSS
node --version
```

### Initial Setup (if not done)
```bash
# Create new Blazor Server project
dotnet new blazorserver -n AllowanceTracker
cd AllowanceTracker

# Add required packages
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# Create test project
dotnet new xunit -n AllowanceTracker.Tests
cd AllowanceTracker.Tests
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Microsoft.AspNetCore.Mvc.Testing
cd ..

# Add test project to solution
dotnet sln add AllowanceTracker.Tests/AllowanceTracker.Tests.csproj

# Setup database
dotnet ef database update

# Run application
dotnet run
```

## 🔑 Key Models & Relationships

### Core Entities
- **ApplicationUser**: Extends IdentityUser with roles (Parent/Child)
- **Family**: Groups users together
- **Child**: Profile with allowance settings
- **Transaction**: Money in/out, immutable with balance snapshots
- **WishListItem**: Things children want to save for

### Entity Relationships
```csharp
// ApplicationUser
public virtual Family? Family { get; set; }
public virtual Child? ChildProfile { get; set; }

// Family
public virtual ICollection<ApplicationUser> Members { get; set; }
public virtual ICollection<Child> Children { get; set; }

// Child
public virtual ApplicationUser User { get; set; }
public virtual Family Family { get; set; }
public virtual ICollection<Transaction> Transactions { get; set; }
```

## 🛠️ Test-Driven Development Workflow

### ⚠️ IMPORTANT: We Follow Strict TDD
**No production code without a failing test first!**

### TDD Cycle: Red → Green → Refactor → Commit
1. **RED**: Write a failing test that describes what you want
2. **GREEN**: Write minimum code to make test pass
3. **REFACTOR**: Improve code while keeping tests green
4. **COMMIT**: Commit after each major feature is complete
5. **REPEAT**: Move to next test

### ⚠️ IMPORTANT: Commit After Each Major Feature
**Commit frequently to preserve progress and enable easy rollback!**

**When to Commit**:
- ✅ After completing a full TDD cycle (Red → Green → Refactor)
- ✅ After implementing a new API endpoint with tests
- ✅ After adding a new service with full test coverage
- ✅ After creating a new Blazor component with tests
- ✅ After completing a phase milestone
- ✅ After fixing a bug with regression tests
- ✅ After successful refactoring with all tests passing

**Commit Message Format**:
```bash
# Feature commits
git commit -m "Add [Feature]: [Brief description]"
# Example: "Add TransactionService: Create transaction with balance validation"

# Test commits (if committing tests separately)
git commit -m "Test [Feature]: [Test coverage description]"
# Example: "Test TransactionService: Validate balance calculations"

# Refactor commits
git commit -m "Refactor [Component]: [Improvement description]"
# Example: "Refactor ChildCard: Extract balance display logic"
```

**Before Committing - Checklist**:
```bash
# 1. Ensure all tests pass
dotnet test
# ✅ Expected: All tests GREEN

# 2. Check for compilation warnings
dotnet build
# ⚠️ Fix any warnings before committing

# 3. Stage and commit
git add .
git commit -m "Add [Feature]: [Description]"

# 4. Verify commit includes intended changes
git show HEAD
```

### Adding a New API Endpoint (TDD Way)
```csharp
// 1. Write test first in AllowanceTracker.Tests/Api/
[Fact]
public async Task GetBalance_ReturnsCurrentBalance()
{
    // Arrange
    var child = await CreateTestChild(balance: 150.00m);

    // Act
    var response = await _client.GetAsync($"/api/v1/children/{child.Id}/balance");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadFromJsonAsync<BalanceResponse>();
    content.Balance.Should().Be(150.00m);
}

// 2. Run test - watch it fail (RED)
// 3. Add controller action
// 4. Implement until test passes (GREEN)
// 5. Refactor if needed
```

### Adding a Blazor Component (TDD Way)
```csharp
// 1. Write component test first
[Fact]
public void ChildCard_DisplaysBalance()
{
    // Arrange
    var child = new Child { FirstName = "Alice", CurrentBalance = 100m };

    // Act
    var component = RenderComponent<ChildCard>(parameters => parameters
        .Add(p => p.Child, child));

    // Assert
    component.Find(".balance").TextContent.Should().Contain("$100.00");
}

// 2. Run test - RED
// 3. Create component
// 4. Implement until GREEN
```

### Adding a Service (TDD Way)
```csharp
// 1. Write service test first
[Fact]
public async Task CreateTransaction_IncreasesBalance()
{
    // Arrange
    var child = await CreateChild(balance: 50m);
    var dto = new CreateTransactionDto(child.Id, 25m, TransactionType.Credit, "Test");

    // Act
    var result = await _transactionService.CreateTransactionAsync(dto);

    // Assert
    result.BalanceAfter.Should().Be(75m);
}
```

### Working with Transactions
```csharp
// ALWAYS use database transactions for money operations
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    var trans = new Transaction
    {
        ChildId = childId,
        Amount = amount,
        Type = type,
        Description = description,
        BalanceAfter = child.CurrentBalance + amount,
        CreatedById = currentUserId
    };

    _context.Transactions.Add(trans);
    child.CurrentBalance = trans.BalanceAfter;

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## 🔐 Security Considerations

### Authentication Flow
1. **Blazor**: ASP.NET Core Identity handles authentication
2. **API**: JWT tokens with 24-hour expiration
3. **Real-time**: SignalR authentication via Blazor

### Authorization Rules
- Parents can manage all children in their family
- Children can only view their own data
- Children cannot create transactions
- Wish list approval requires parent role

### Data Integrity
- Never allow negative balances
- All financial operations in database transactions
- Audit trail via CreatedBy on all operations
- Immutable transaction records

## 🧪 Testing Guidelines - TDD MANDATORY

### TDD Is Not Optional!
**EVERY feature must start with a failing test**

### TDD Workflow Example
```bash
# 1. Write test FIRST
# Create test in AllowanceTracker.Tests/

# 2. Run test - should FAIL (Red)
dotnet test --filter "FullyQualifiedName~NewFeature"
# Expected: FAILURE ❌

# 3. Write minimum code to pass

# 4. Run test - should PASS (Green)
dotnet test --filter "FullyQualifiedName~NewFeature"
# Expected: SUCCESS ✅

# 5. Refactor if needed

# 6. Run all tests
dotnet test
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test file
dotnet test --filter "FullyQualifiedName~TransactionTests"

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Watch mode (continuous testing)
dotnet watch test

# Run only specific category
dotnet test --filter "Category=Unit"
```

### Test Setup Patterns
```csharp
// Use builder pattern for test data
public class ChildBuilder
{
    private decimal _balance = 0;
    private decimal _weeklyAllowance = 10m;

    public ChildBuilder WithBalance(decimal balance)
    {
        _balance = balance;
        return this;
    }

    public Child Build() => new Child
    {
        CurrentBalance = _balance,
        WeeklyAllowance = _weeklyAllowance
    };
}

// Usage
var child = new ChildBuilder()
    .WithBalance(100m)
    .Build();
```

### Critical Test Coverage Requirements
Must have >90% test coverage for:
- Authentication (Identity/JWT)
- Transaction creation and balance updates
- Weekly allowance processing
- Authorization rules
- Balance integrity
- Transaction immutability

## 🚢 Deployment

### Railway Deployment
```bash
# Add Railway.json
{
  "build": {
    "builder": "NIXPACKS"
  },
  "deploy": {
    "startCommand": "dotnet AllowanceTracker.dll",
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}

# Environment variables
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=${{Postgres.DATABASE_URL}}
```

### Azure Deployment
```bash
# Create App Service
az webapp create --name allowancetracker --plan MyPlan --runtime "DOTNET|8.0"

# Deploy
dotnet publish -c Release
az webapp deployment source config-zip --src publish.zip
```

## 📝 Implementation Status

### Completed ✅
- [x] Specifications migrated to .NET
- [x] Database schema (EF Core)
- [x] API specification (ASP.NET Core)
- [x] Implementation phases (TDD with xUnit)
- [x] Testing strategy (.NET focused)
- [x] TDD best practices (.NET/C#)
- [x] Blazor UI specification

### Phase 1: Foundation (Week 1) ✅ COMPLETE
- [x] Initialize ASP.NET Core project
- [x] Setup Entity Framework Core
- [x] Configure PostgreSQL
- [x] **Write ApplicationUser tests FIRST** (12 tests)
- [x] Implement ApplicationUser with Identity
- [x] **Write Family model tests FIRST** (11 tests)
- [x] Implement Family model
- [x] Create all domain models (Child, Transaction, WishListItem)
- [x] Setup AllowanceContext with full EF Core configuration
- [x] Create initial database migration
- [x] **Total: 24 tests passing**

### Phase 2: Transactions (Week 2) ✅ COMPLETE
- [x] **Write TransactionService tests FIRST** (11 tests)
- [x] Implement Transaction model (already done in Phase 1)
- [x] Create TransactionService with TDD
- [x] Add balance tracking with database transactions
- [x] Implement audit trail (CreatedBy, CreatedAt)
- [x] Atomic transaction operations
- [x] Balance validation (insufficient funds check)
- [x] **Total: 35 tests passing** (24 from Phase 1 + 11 new)

### Phase 3: Blazor UI ✅ COMPLETE
- [x] Setup Blazor Server (already done in Phase 1)
- [x] Create MainLayout (already done in Phase 1)
- [x] Add bUnit for Blazor component testing
- [x] Create FamilyService for data access
- [x] Create ChildDto for data transfer
- [x] Implement Dashboard page with real-time updates
- [x] Display children with balances in responsive grid
- [x] **Write ChildCard component tests** (6 tests)
- [x] Create ChildCard reusable component
- [x] **Write TransactionForm component tests** (10 tests)
- [x] Create TransactionForm component with validation
- [x] **Write Dashboard component tests** (7 tests)
- [x] Implement FamilyHub for SignalR group management
- [x] Add real-time SignalR updates (TransactionCreated events)
- [x] Graceful SignalR connection handling (works in tests)
- [x] **Total: 133 tests passing** (123 from Phases 1-5 + 10 new Phase 3 UI tests)

### Phase 4: Allowance Management & Background Jobs (Week 2-3) ✅ COMPLETE
- [x] **Write AllowanceService tests FIRST** (10 tests)
- [x] Implement AllowanceService with TDD
- [x] Pay weekly allowance functionality
- [x] Prevent double-payment same week
- [x] Process all pending allowances
- [x] Error handling and logging
- [x] Setup IHostedService background job
- [x] WeeklyAllowanceJob with 24-hour interval
- [x] **Total: 45 tests passing** (24 from Phase 1 + 11 from Phase 2 + 10 new)

### Phase 6: Wish List Features ✅ COMPLETE
- [x] Examine WishListItem model (already existed)
- [x] **Create WishList DTOs** (CreateWishListItemDto, UpdateWishListItemDto, WishListItemDto)
- [x] **Write WishListService tests** (12 tests)
- [x] Implement IWishListService interface
- [x] Implement WishListService with full CRUD operations
- [x] Add MarkAsPurchased/MarkAsUnpurchased functionality
- [x] Calculate CanAfford based on child's current balance
- [x] **Write WishListController tests** (11 tests)
- [x] Implement WishListController API endpoints
- [x] Parent-only authorization for purchase operations
- [x] Register WishListService in DI container
- [x] **Total: 156 tests passing** (145 from Phases 1-5 + 11 new Phase 6 tests)

### Phase 5: API & Authentication ✅ COMPLETE
- [x] **Write JWT authentication tests** (10 tests)
- [x] Implement JwtService with token generation & validation
- [x] **Write AccountService tests** (9 tests)
- [x] Implement AccountService with registration & login
- [x] **Write TransactionsController tests** (5 tests)
- [x] Implement TransactionsController API endpoints
- [x] **Write ChildrenController tests** (10 tests)
- [x] Implement ChildrenController API endpoints
- [x] **Write AuthController tests** (9 tests)
- [x] Implement AuthController with JWT integration
- [x] Configure JWT authentication middleware in Program.cs
- [x] **Total: 123 tests passing** (45 from Phases 1-4 + 78 new Phase 5 tests)

### Phase 7: Reports & Analytics UI ✅ COMPLETE
- [x] Review existing TransactionAnalyticsService (already implemented in earlier phases)
- [x] Verify all analytics DTOs exist (BalancePoint, TrendData, MonthlyComparison, etc.)
- [x] **Write AnalyticsController tests** (10 tests)
- [x] Implement AnalyticsController API endpoints
- [x] Analytics API endpoints: balance history, income vs spending, spending trends
- [x] Analytics API endpoints: savings rate, monthly comparison, transaction heatmap
- [x] Analytics API endpoints: spending breakdown by category
- [x] **Write Analytics Blazor page tests** (9 tests)
- [x] Implement Analytics.razor page with comprehensive data visualization
- [x] Child selector dropdown with auto-selection for single child
- [x] Balance history table with transaction descriptions
- [x] Income vs Spending summary card with savings rate
- [x] Spending breakdown by category with percentages
- [x] Monthly comparison table with income, spending, and ending balances
- [x] Parallel data loading for optimal performance
- [x] **Total: 175 tests passing** (166 from previous phases + 19 new Phase 7 tests)

## 🐛 Common Issues & Solutions

### Issue: EF Core Migrations
```bash
# Add migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# Remove last migration
dotnet ef migrations remove
```

### Issue: Blazor SignalR Disconnection
```csharp
// Add reconnection logic
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/allowancehub"))
            .WithAutomaticReconnect()
            .Build();

        await hubConnection.StartAsync();
    }
}
```

### Issue: JWT Token Invalid
```csharp
// Check configuration
builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });
```

## 📚 Key Files Reference

### Specifications (Read First!)
- `specs/01-overview.md` - MVP system overview
- `specs/02-database-schema.md` - EF Core models
- `specs/03-api-specification.md` - API documentation
- `specs/04-implementation-phases.md` - TDD roadmap
- `specs/05-testing-strategy.md` - xUnit approach
- `specs/06-tdd-best-practices.md` - .NET TDD patterns
- `specs/07-blazor-ui-specification.md` - UI components
- `specs/08-ios-app-specification.md` - iOS native app (SwiftUI)

### Core Implementation Files
- `Data/AllowanceContext.cs` - EF Core DbContext
- `Models/ApplicationUser.cs` - User authentication
- `Services/TransactionService.cs` - Business logic
- `Pages/Dashboard.razor` - Main UI
- `Api/V1/TransactionsController.cs` - API endpoints

## 🎯 Next Actions

When working on this project:

1. **Read specs first**: All specifications in `/specs` folder
2. **Follow TDD strictly**: Write test first, always!
3. **Check current phase**: See Implementation Status
4. **Run tests**: Ensure nothing broken before starting
5. **Use patterns**: Follow established code patterns
6. **Commit after major features**: Save progress frequently (see TDD Workflow section)
7. **Update this file**: Keep current with progress

### Quick Commands
```bash
# Run app
dotnet run

# Run tests
dotnet test

# Add package
dotnet add package PackageName

# EF migrations
dotnet ef migrations add MigrationName
dotnet ef database update

# Watch mode
dotnet watch run
dotnet watch test
```

## 💡 Best Practices

### C# Coding Standards
1. **Use nullable reference types** - Enable in project
2. **Async all the way** - Use async/await consistently
3. **LINQ for queries** - Leverage LINQ instead of loops
4. **Records for DTOs** - Use record types for immutable data
5. **Dependency injection** - Constructor injection everywhere

### Blazor Best Practices
1. **Component isolation** - Keep components focused
2. **State management** - Use cascading values wisely
3. **Virtualization** - For long lists
4. **Error boundaries** - Handle component errors
5. **Dispose properly** - Implement IDisposable

### EF Core Best Practices
1. **AsNoTracking()** - For read-only queries
2. **Include() carefully** - Avoid N+1 queries
3. **Transactions** - For multi-table updates
4. **Compiled queries** - For hot paths
5. **Migrations** - Never edit, always add new

### Testing Best Practices
1. **AAA pattern** - Arrange, Act, Assert
2. **One assertion** - Per test method
3. **Test behavior** - Not implementation
4. **Mock sparingly** - Only external dependencies
5. **Test data builders** - For complex objects

## 🔗 Useful Resources

### .NET & C#
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/dotnet/csharp/)
- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core/)

### Blazor
- [Blazor Documentation](https://docs.microsoft.com/aspnet/core/blazor/)
- [Blazor University](https://blazor-university.com/)
- [bUnit Testing](https://bunit.dev/)

### Entity Framework Core
- [EF Core Documentation](https://docs.microsoft.com/ef/core/)
- [EF Core Tutorial](https://www.entityframeworktutorial.net/efcore/)

### Testing
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [Moq Quick Start](https://github.com/moq/moq4/wiki/Quickstart)

### Deployment
- [Railway Docs](https://docs.railway.app/)
- [Azure App Service](https://docs.microsoft.com/azure/app-service/)

## 📋 Common Patterns

### Money Handling
```csharp
// Always use decimal for money
public decimal Amount { get; set; }

// Format for display
amount.ToString("C") // $123.45

// Validation
[Range(0.01, 10000)]
public decimal Amount { get; set; }
```

### Async Service Pattern
```csharp
public interface ITransactionService
{
    Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto);
    Task<decimal> GetBalanceAsync(Guid childId);
}

public class TransactionService : ITransactionService
{
    private readonly AllowanceContext _context;

    public async Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto)
    {
        // Implementation
        await _context.SaveChangesAsync();
        return transaction;
    }
}
```

### Blazor Component Pattern
```razor
@page "/dashboard"
@inject IFamilyService FamilyService

<h1>Dashboard</h1>

@if (IsLoading)
{
    <LoadingSpinner />
}
else if (Children.Any())
{
    @foreach (var child in Children)
    {
        <ChildCard Child="@child" />
    }
}

@code {
    private bool IsLoading = true;
    private List<Child> Children = new();

    protected override async Task OnInitializedAsync()
    {
        Children = await FamilyService.GetChildrenAsync();
        IsLoading = false;
    }
}
```

## 🔄 TDD Examples

### Model Test
```csharp
[Fact]
public void Child_RequiresPositiveAllowance()
{
    // Arrange
    var child = new Child { WeeklyAllowance = -5m };
    var validator = new ChildValidator();

    // Act
    var result = validator.Validate(child);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e => e.PropertyName == "WeeklyAllowance");
}
```

### Service Test
```csharp
[Fact]
public async Task ProcessAllowance_AddsCorrectAmount()
{
    // Arrange
    var child = CreateChild(weeklyAllowance: 10m, balance: 50m);

    // Act
    await _allowanceService.ProcessWeeklyAllowanceAsync(child.Id);

    // Assert
    var updated = await _context.Children.FindAsync(child.Id);
    updated.CurrentBalance.Should().Be(60m);
}
```

### API Test
```csharp
[Fact]
public async Task CreateTransaction_RequiresAuthentication()
{
    // Arrange
    var dto = new CreateTransactionDto(Guid.NewGuid(), 10m, TransactionType.Credit, "Test");

    // Act
    var response = await _client.PostAsJsonAsync("/api/v1/transactions", dto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
}
```

## Remember: Read specs first, write tests first, keep it simple for MVP!