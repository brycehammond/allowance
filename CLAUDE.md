# Allowance Tracker - .NET Development Guide for Claude

## 🎯 Project Context
A simple MVP allowance tracking application built with ASP.NET Core 8.0 Web API backend and React frontend. Helps parents manage children's allowances, track spending, and teach financial responsibility through modern web and mobile interfaces.

**License**: MIT License (see LICENSE file)

## 🏗️ Current Architecture (MVP Focus)

### Technology Stack
- **Backend**: ASP.NET Core 8.0 Web API
- **Frontend**: React (separate repository/deployment)
- **Database**: SQL Server with Entity Framework Core 8
- **Authentication**: ASP.NET Core Identity + JWT tokens
- **Background Jobs**: IHostedService (built-in, no external dependencies)
- **Testing**: xUnit, FluentAssertions, Moq
- **CI/CD**: GitHub Actions
- **Deployment**: Azure App Service (API) or Railway

### Key Design Decisions
1. **RESTful API**: Clean separation of concerns, supports multiple clients (web, iOS, Android)
2. **Entity Framework Core**: Code-first migrations, LINQ queries, good enough performance
3. **Built-in DI**: Microsoft.Extensions.DependencyInjection - simple and effective
4. **SQL Server**: Works great with EF Core, Azure integration
5. **Minimal dependencies**: Faster to build, easier to maintain

## 📁 Project Structure (API-Only)

```
AllowanceTracker/
├── specs/                        # Detailed specifications (READ THESE FIRST!)
│   ├── 01-overview.md           # System overview and goals
│   ├── 02-database-schema.md    # EF Core entity models
│   ├── 03-api-specification.md  # API endpoints documentation
│   ├── 04-implementation-phases.md # TDD development roadmap
│   ├── 05-testing-strategy.md   # xUnit testing approach
│   ├── 06-tdd-best-practices.md # TDD patterns for .NET
│   └── 08-ios-app-specification.md # iOS native app (SwiftUI)
├── Data/                        # EF Core DbContext
│   ├── AllowanceContext.cs
│   └── Migrations/
├── Models/                      # Domain entities
│   ├── ApplicationUser.cs
│   ├── Family.cs
│   ├── Child.cs
│   └── Transaction.cs
├── DTOs/                        # Data Transfer Objects
│   ├── Auth/
│   ├── Children/
│   ├── Transactions/
│   └── Analytics/
├── Services/                    # Business logic
│   ├── FamilyService.cs
│   ├── TransactionService.cs
│   ├── AllowanceService.cs
│   ├── JwtService.cs
│   └── AccountService.cs
├── Controllers/                 # API controllers
│   ├── AuthController.cs
│   ├── ChildrenController.cs
│   ├── TransactionsController.cs
│   ├── WishListController.cs
│   └── AnalyticsController.cs
├── Program.cs                  # Startup, DI, and middleware
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

# Check SQL Server is running (or use LocalDB)
# Windows: Check SQL Server service
# Mac: Docker container with SQL Server
docker ps | grep sql
```

### Initial Setup (if not done)
```bash
# Create new Web API project
dotnet new webapi -n AllowanceTracker
cd AllowanceTracker

# Add required packages
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore

# Create test project
dotnet new xunit -n AllowanceTracker.Tests
cd AllowanceTracker.Tests
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
cd ..

# Add test project to solution
dotnet sln add AllowanceTracker.Tests/AllowanceTracker.Tests.csproj

# Setup database
dotnet ef database update

# Run API
dotnet run
# API will be available at https://localhost:5001
# Swagger UI at https://localhost:5001/swagger
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
1. **API**: JWT tokens with 24-hour expiration
2. **Identity**: ASP.NET Core Identity for user management
3. **CORS**: Configured for React frontend (localhost:5173, localhost:3000)

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
- [x] Moved to API-only architecture with React frontend

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

### Phase 3: React Frontend (Separate Repository)
- [x] Moved to separate React repository
- [x] Backend now API-only with CORS support
- [x] Frontend communicates via REST API
- [x] JWT authentication from React app

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

### Phase 7: Reports & Analytics API ✅ COMPLETE
- [x] Review existing TransactionAnalyticsService (already implemented in earlier phases)
- [x] Verify all analytics DTOs exist (BalancePoint, TrendData, MonthlyComparison, etc.)
- [x] **Write AnalyticsController tests** (10 tests)
- [x] Implement AnalyticsController API endpoints
- [x] Analytics API endpoints: balance history, income vs spending, spending trends
- [x] Analytics API endpoints: savings rate, monthly comparison, transaction heatmap
- [x] Analytics API endpoints: spending breakdown by category
- [x] Frontend integration handled in React app

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

### Issue: CORS Errors
```csharp
// Ensure CORS is properly configured in Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// And applied before authentication
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
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
- `specs/08-ios-app-specification.md` - iOS native app (SwiftUI)
- `specs/10-future-features.md` - Future feature roadmap and ideas

### Core Implementation Files
- `Data/AllowanceContext.cs` - EF Core DbContext
- `Models/ApplicationUser.cs` - User authentication
- `Services/TransactionService.cs` - Business logic
- `Controllers/TransactionsController.cs` - API endpoints
- `Controllers/AuthController.cs` - JWT authentication

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

### API Best Practices
1. **RESTful design** - Use proper HTTP verbs and status codes
2. **DTOs for all endpoints** - Never expose domain models directly
3. **Consistent error handling** - Use ProblemDetails for errors
4. **API versioning** - Maintain backward compatibility
5. **Swagger documentation** - Keep API docs up to date

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

### iOS App Best Practices (SwiftUI)

The iOS app targets iOS 17+ and uses the modern **Observable framework** (not the legacy ObservableObject pattern).

#### Observable Framework Pattern
```swift
// ViewModels use @Observable macro (NOT ObservableObject)
@Observable
@MainActor
final class MyViewModel {
    var items: [Item] = []           // No @Published needed
    private(set) var isLoading = false
    var errorMessage: String?

    private let apiService: APIServiceProtocol

    init(apiService: APIServiceProtocol = APIService()) {
        self.apiService = apiService
    }
}

// Views use @State for owned view models (NOT @StateObject)
struct MyView: View {
    @State private var viewModel = MyViewModel()

    var body: some View {
        // viewModel changes automatically trigger view updates
    }
}

// Views use @Environment for shared view models (NOT @EnvironmentObject)
struct ChildView: View {
    @Environment(AuthViewModel.self) private var authViewModel
}

// Pass view models through environment (NOT .environmentObject)
ContentView()
    .environment(authViewModel)
```

#### Key Migration Rules
| Old (ObservableObject) | New (Observable) |
|------------------------|------------------|
| `class VM: ObservableObject` | `@Observable class VM` |
| `@Published var` | `var` (no wrapper needed) |
| `@StateObject` | `@State` |
| `@ObservedObject` | Remove wrapper entirely |
| `@EnvironmentObject var vm: T` | `@Environment(T.self) var vm` |
| `.environmentObject(vm)` | `.environment(vm)` |

#### SwiftUI Coding Standards
1. **Use @Observable** - For all view models (iOS 17+)
2. **@MainActor** - All view models for thread safety
3. **Dependency injection** - Pass services via initializer
4. **private(set)** - For read-only computed state
5. **async/await** - For all network operations

## 🔗 Useful Resources

### .NET & C#
- [.NET Documentation](https://docs.microsoft.com/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/dotnet/csharp/)
- [ASP.NET Core Docs](https://docs.microsoft.com/aspnet/core/)

### Web API
- [Web API Documentation](https://docs.microsoft.com/aspnet/core/web-api/)
- [REST API Guidelines](https://github.com/microsoft/api-guidelines)
- [Swagger/OpenAPI](https://swagger.io/docs/)

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

### API Controller Pattern
```csharp
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpGet("{childId}")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(Guid childId)
    {
        var transactions = await _transactionService.GetTransactionsAsync(childId);
        return Ok(transactions);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionDto>> CreateTransaction(CreateTransactionDto dto)
    {
        var transaction = await _transactionService.CreateTransactionAsync(dto);
        return CreatedAtAction(nameof(GetTransactions), new { childId = transaction.ChildId }, transaction);
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

## Remember: Read specs first, write tests first, API-first architecture, keep it simple for MVP!
- Keep a log of what are the next steaps