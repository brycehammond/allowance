# Implementation Phases - .NET TDD Approach (MVP)

## Overview
Practical TDD implementation plan for building the Allowance Tracker MVP in .NET 8. Each phase follows the Red-Green-Refactor cycle with clear deliverables.

## Phase 1: Project Setup & Authentication (Week 1)

### Day 1-2: Project Initialization
```bash
# Create solution and projects
dotnet new sln -n AllowanceTracker
dotnet new blazorserver -n AllowanceTracker.Web
dotnet sln add AllowanceTracker.Web

# Add test project
dotnet new xunit -n AllowanceTracker.Tests
dotnet sln add AllowanceTracker.Tests
dotnet add AllowanceTracker.Tests reference AllowanceTracker.Web

# Add packages to main project
cd AllowanceTracker.Web
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Swashbuckle.AspNetCore

# Add packages to test project
cd ../AllowanceTracker.Tests
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package FluentAssertions
dotnet add package Moq
```

### Day 3-4: User & Authentication Tests First

#### Test: User Registration
```csharp
[Fact]
public async Task Register_WithValidData_CreatesUserAndReturnsToken()
{
    // Arrange
    var client = _factory.CreateClient();
    var registerDto = new RegisterDto(
        "test@example.com",
        "Password123!",
        "John",
        "Doe",
        UserRole.Parent);

    // Act
    var response = await client.PostAsJsonAsync("/api/auth/register", registerDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
    content.Token.Should().NotBeNullOrEmpty();
    content.User.Email.Should().Be("test@example.com");
}

[Fact]
public async Task Register_Parent_CreatesFamilyAutomatically()
{
    // Arrange & Act
    var response = await RegisterTestUser(UserRole.Parent);

    // Assert
    using var scope = _factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AllowanceContext>();
    var user = await context.Users.FirstAsync(u => u.Email == "test@example.com");
    user.FamilyId.Should().NotBeNull();
    var family = await context.Families.FindAsync(user.FamilyId);
    family.Should().NotBeNull();
}
```

#### Implementation: Make Tests Pass
```csharp
// Models/ApplicationUser.cs
public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? FamilyId { get; set; }
    public virtual Family? Family { get; set; }
}

// Controllers/AuthController.cs
[HttpPost("register")]
public async Task<IActionResult> Register(RegisterDto dto)
{
    var user = new ApplicationUser
    {
        UserName = dto.Email,
        Email = dto.Email,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Role = dto.Role
    };

    var result = await _userManager.CreateAsync(user, dto.Password);
    if (!result.Succeeded)
        return BadRequest(result.Errors);

    if (dto.Role == UserRole.Parent)
    {
        var family = new Family { Name = $"{dto.LastName} Family" };
        _context.Families.Add(family);
        user.FamilyId = family.Id;
        await _context.SaveChangesAsync();
    }

    var token = GenerateJwtToken(user);
    return Ok(new AuthResponseDto(token, DateTime.UtcNow.AddDays(1), MapToDto(user)));
}
```

### Day 5: Database Setup & Migrations
```bash
# Add initial migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

## Phase 2: Family & Children Management (Week 1-2)

### Day 6-7: Family Tests & Implementation

#### Test: Add Child to Family
```csharp
[Fact]
public async Task AddChild_AsParent_CreatesChildProfile()
{
    // Arrange
    var parentToken = await GetParentToken();
    var client = _factory.WithWebHostBuilder(builder =>
    {
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication("Test")
                .AddScheme<TestAuthHandler>("Test", options => { });
        });
    }).CreateClient();

    var addChildDto = new AddChildDto
    {
        Email = "child@example.com",
        Password = "ChildPass123!",
        FirstName = "Jane",
        LastName = "Doe",
        WeeklyAllowance = 10.00m
    };

    // Act
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", parentToken);
    var response = await client.PostAsJsonAsync("/api/family/children", addChildDto);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var child = await response.Content.ReadFromJsonAsync<ChildDto>();
    child.WeeklyAllowance.Should().Be(10.00m);
    child.CurrentBalance.Should().Be(0);
}
```

#### Implementation: Family Service
```csharp
public class FamilyService : IFamilyService
{
    private readonly AllowanceContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public async Task<Child> AddChildAsync(Guid familyId, AddChildDto dto)
    {
        // Create user
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = UserRole.Child,
            FamilyId = familyId
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to create user");

        // Create child profile
        var child = new Child
        {
            UserId = user.Id,
            FamilyId = familyId,
            WeeklyAllowance = dto.WeeklyAllowance
        };

        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        return child;
    }
}
```

## Phase 3: Transactions & Balance Management (Week 2)

### Day 8-10: Transaction Tests & Core Logic

#### Test: Create Transaction Updates Balance
```csharp
[Fact]
public async Task CreateTransaction_Credit_IncreasesBalance()
{
    // Arrange
    var child = await CreateTestChild(balance: 50.00m);
    var dto = new CreateTransactionDto(
        child.Id,
        25.00m,
        TransactionType.Credit,
        "Test credit");

    // Act
    var transaction = await _transactionService.CreateTransactionAsync(dto);

    // Assert
    transaction.BalanceAfter.Should().Be(75.00m);

    var updatedChild = await _context.Children.FindAsync(child.Id);
    updatedChild.CurrentBalance.Should().Be(75.00m);
}

[Fact]
public async Task CreateTransaction_Debit_WithInsufficientFunds_ThrowsException()
{
    // Arrange
    var child = await CreateTestChild(balance: 10.00m);
    var dto = new CreateTransactionDto(
        child.Id,
        25.00m,
        TransactionType.Debit,
        "Test debit");

    // Act
    var act = () => _transactionService.CreateTransactionAsync(dto);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Insufficient funds");
}

[Fact]
public async Task Transaction_IsImmutable_AfterCreation()
{
    // Arrange
    var transaction = await CreateTestTransaction();
    var originalAmount = transaction.Amount;

    // Act - Try to modify
    transaction.Amount = 999.99m;
    _context.Entry(transaction).State = EntityState.Modified;
    var act = () => _context.SaveChangesAsync();

    // Assert - Should not allow modification
    await act.Should().ThrowAsync<InvalidOperationException>();
}
```

#### Implementation: Transaction Service
```csharp
public class TransactionService : ITransactionService
{
    private readonly AllowanceContext _context;
    private readonly ICurrentUserService _currentUser;

    public async Task<Transaction> CreateTransactionAsync(CreateTransactionDto dto)
    {
        // Use transaction for consistency
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var child = await _context.Children.FindAsync(dto.ChildId)
                ?? throw new InvalidOperationException("Child not found");

            // Validate balance for debits
            if (dto.Type == TransactionType.Debit && child.CurrentBalance < dto.Amount)
                throw new InvalidOperationException("Insufficient funds");

            // Update balance
            if (dto.Type == TransactionType.Credit)
                child.CurrentBalance += dto.Amount;
            else
                child.CurrentBalance -= dto.Amount;

            // Create immutable transaction record
            var trans = new Transaction
            {
                ChildId = dto.ChildId,
                Amount = dto.Amount,
                Type = dto.Type,
                Description = dto.Description,
                BalanceAfter = child.CurrentBalance,
                CreatedById = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Transactions.Add(trans);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return trans;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
```

### Day 11-12: Real-time Updates with SignalR

#### Test: Balance Updates Broadcast to Family
```csharp
[Fact]
public async Task TransactionCreated_BroadcastsToFamilyMembers()
{
    // Arrange
    var hubConnection = new HubConnectionBuilder()
        .WithUrl("https://localhost/familyHub", options =>
        {
            options.AccessTokenProvider = () => Task.FromResult(_parentToken);
        })
        .Build();

    var receivedUpdate = false;
    hubConnection.On<TransactionDto>("TransactionCreated", dto =>
    {
        receivedUpdate = true;
    });

    await hubConnection.StartAsync();

    // Act
    await CreateTransaction(childId, 10.00m, TransactionType.Credit);

    // Assert
    await Task.Delay(500); // Wait for broadcast
    receivedUpdate.Should().BeTrue();
}
```

#### Implementation: SignalR Hub
```csharp
public class FamilyHub : Hub
{
    private readonly ICurrentUserService _currentUser;

    public override async Task OnConnectedAsync()
    {
        if (_currentUser.FamilyId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId,
                $"family-{_currentUser.FamilyId}");
        }
        await base.OnConnectedAsync();
    }
}
```

## Phase 4: Allowance Management (Week 2-3)

### Day 13-14: Allowance Payment Tests

#### Test: Pay Weekly Allowance
```csharp
[Fact]
public async Task PayAllowance_CreatesTransactionAndUpdatesDate()
{
    // Arrange
    var child = await CreateTestChild(weeklyAllowance: 15.00m, balance: 10.00m);

    // Act
    await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

    // Assert
    var updatedChild = await _context.Children
        .Include(c => c.Transactions)
        .FirstAsync(c => c.Id == child.Id);

    updatedChild.CurrentBalance.Should().Be(25.00m);
    updatedChild.LastAllowanceDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

    var transaction = updatedChild.Transactions.Last();
    transaction.Amount.Should().Be(15.00m);
    transaction.Description.Should().Contain("Weekly Allowance");
}

[Fact]
public async Task PayAllowance_PreventsDoublePay_SameWeek()
{
    // Arrange
    var child = await CreateTestChild(weeklyAllowance: 15.00m);
    await _allowanceService.PayWeeklyAllowanceAsync(child.Id);

    // Act
    var act = () => _allowanceService.PayWeeklyAllowanceAsync(child.Id);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("Allowance already paid this week");
}
```

## Phase 5: Blazor UI Components (Week 3)

### Day 15-17: Component Tests with bUnit

#### Test: Dashboard Component
```csharp
[Fact]
public void Dashboard_DisplaysChildrenWithBalances()
{
    // Arrange
    var children = new List<ChildDto>
    {
        new(Guid.NewGuid(), "Jane Doe", 10.00m, 25.50m, null),
        new(Guid.NewGuid(), "John Doe", 15.00m, 50.00m, null)
    };

    var mockService = new Mock<IFamilyService>();
    mockService.Setup(s => s.GetChildrenAsync()).ReturnsAsync(children);

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockService.Object);

    // Act
    var component = ctx.RenderComponent<Dashboard>();

    // Assert
    component.Find("h1").TextContent.Should().Contain("Family Dashboard");
    var childCards = component.FindAll(".child-card");
    childCards.Count.Should().Be(2);
    childCards[0].TextContent.Should().Contain("Jane Doe");
    childCards[0].TextContent.Should().Contain("$25.50");
}

[Fact]
public void TransactionForm_ValidatesInput()
{
    // Arrange
    using var ctx = new TestContext();
    var component = ctx.RenderComponent<TransactionForm>();

    // Act - Submit empty form
    var form = component.Find("form");
    form.Submit();

    // Assert
    component.FindAll(".validation-message").Count.Should().BeGreaterThan(0);
}
```

### Day 18-19: Blazor Pages Implementation

#### Dashboard.razor
```razor
@page "/dashboard"
@attribute [Authorize]
@inject IFamilyService FamilyService
@inject ITransactionService TransactionService

<h1>Family Dashboard</h1>

@if (children == null)
{
    <p>Loading...</p>
}
else if (!children.Any())
{
    <p>No children added yet. <a href="/family/add-child">Add a child</a></p>
}
else
{
    <div class="row">
        @foreach (var child in children)
        {
            <div class="col-md-4">
                <ChildCard Child="@child" OnTransactionAdded="@RefreshData" />
            </div>
        }
    </div>
}

@code {
    private List<ChildDto>? children;

    protected override async Task OnInitializedAsync()
    {
        await LoadChildren();
    }

    private async Task LoadChildren()
    {
        children = await FamilyService.GetChildrenAsync();
    }

    private async Task RefreshData()
    {
        await LoadChildren();
        StateHasChanged();
    }
}
```

#### ChildCard.razor Component
```razor
@inject ITransactionService TransactionService

<div class="card child-card">
    <div class="card-body">
        <h5 class="card-title">@Child.Name</h5>
        <p class="card-text">
            Balance: <strong>@Child.CurrentBalance.ToString("C")</strong><br />
            Weekly Allowance: @Child.WeeklyAllowance.ToString("C")
        </p>

        <button class="btn btn-sm btn-success" @onclick="ShowAddMoney">
            Add Money
        </button>
        <button class="btn btn-sm btn-warning" @onclick="ShowSubtractMoney">
            Subtract Money
        </button>

        @if (showTransactionForm)
        {
            <QuickTransactionForm ChildId="@Child.Id"
                                  TransactionType="@transactionType"
                                  OnComplete="@CompleteTransaction" />
        }
    </div>
</div>

@code {
    [Parameter] public ChildDto Child { get; set; } = null!;
    [Parameter] public EventCallback OnTransactionAdded { get; set; }

    private bool showTransactionForm;
    private TransactionType transactionType;

    private void ShowAddMoney()
    {
        showTransactionForm = true;
        transactionType = TransactionType.Credit;
    }

    private void ShowSubtractMoney()
    {
        showTransactionForm = true;
        transactionType = TransactionType.Debit;
    }

    private async Task CompleteTransaction()
    {
        showTransactionForm = false;
        await OnTransactionAdded.InvokeAsync();
    }
}
```

## Phase 6: Wish List Feature (Week 3-4)

### Day 20-21: Wish List Tests & Implementation

#### Test: Wish List Item Management
```csharp
[Fact]
public async Task AddWishListItem_CalculatesAffordability()
{
    // Arrange
    var child = await CreateTestChild(balance: 50.00m);
    var item1 = new CreateWishListItemDto("Toy", 25.00m, null, null);
    var item2 = new CreateWishListItemDto("Game", 75.00m, null, null);

    // Act
    var affordable = await _wishListService.AddItemAsync(child.Id, item1);
    var notAffordable = await _wishListService.AddItemAsync(child.Id, item2);

    // Assert
    affordable.CanAfford.Should().BeTrue();
    notAffordable.CanAfford.Should().BeFalse();
}
```

## Phase 7: Performance & Polish (Week 4)

### Day 22-23: Performance Tests

#### Performance Test with BenchmarkDotNet
```csharp
[MemoryDiagnoser]
public class TransactionBenchmark
{
    private TransactionService _service;
    private Guid _childId;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        // Setup services
        var provider = services.BuildServiceProvider();
        _service = provider.GetRequiredService<TransactionService>();
        _childId = Guid.NewGuid();
    }

    [Benchmark]
    public async Task CreateSingleTransaction()
    {
        await _service.CreateTransactionAsync(new CreateTransactionDto(
            _childId, 10.00m, TransactionType.Credit, "Test"));
    }

    [Benchmark]
    public async Task CreateBulkTransactions()
    {
        var tasks = new List<Task>();
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(_service.CreateTransactionAsync(new CreateTransactionDto(
                _childId, 1.00m, TransactionType.Credit, $"Test {i}")));
        }
        await Task.WhenAll(tasks);
    }
}
```

### Day 24-25: Integration Tests

#### Full Flow Integration Test
```csharp
[Fact]
public async Task FullUserJourney_ParentAndChild_Works()
{
    // 1. Register parent
    var parentToken = await RegisterAndLogin("parent@test.com", UserRole.Parent);

    // 2. Add child
    var childDto = await AddChild(parentToken, "child@test.com", 20.00m);

    // 3. Add transaction
    await AddTransaction(parentToken, childDto.Id, 20.00m, TransactionType.Credit);

    // 4. Child logs in and views balance
    var childToken = await Login("child@test.com", "ChildPass123!");
    var balance = await GetBalance(childToken, childDto.Id);
    balance.Should().Be(20.00m);

    // 5. Child adds wish list item
    await AddWishListItem(childToken, childDto.Id, "Toy", 15.00m);

    // 6. Parent pays allowance
    await PayAllowance(parentToken, childDto.Id);

    // 7. Verify final balance
    var finalBalance = await GetBalance(childToken, childDto.Id);
    finalBalance.Should().Be(40.00m); // 20 initial + 20 allowance
}
```

## Phase 8: Deployment Preparation (Week 4)

### Day 26-27: Docker & CI/CD

#### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AllowanceTracker.Web/AllowanceTracker.Web.csproj", "AllowanceTracker.Web/"]
RUN dotnet restore "AllowanceTracker.Web/AllowanceTracker.Web.csproj"
COPY . .
WORKDIR "/src/AllowanceTracker.Web"
RUN dotnet build "AllowanceTracker.Web.csproj" -c Release -o /app/build

FROM build AS test
WORKDIR "/src/AllowanceTracker.Tests"
RUN dotnet test "AllowanceTracker.Tests.csproj" -c Release --logger:trx

FROM build AS publish
RUN dotnet publish "AllowanceTracker.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AllowanceTracker.Web.dll"]
```

#### GitHub Actions CI/CD
```yaml
name: Build and Deploy

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
      run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

    - name: Upload coverage
      uses: codecov/codecov-action@v3

  deploy:
    needs: test
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/main'

    steps:
    - uses: actions/checkout@v3

    - name: Deploy to Railway
      uses: railway/deploy-action@v1
      with:
        railway_token: ${{ secrets.RAILWAY_TOKEN }}
```

## TDD Best Practices for .NET

### 1. Test Organization
```
AllowanceTracker.Tests/
├── Unit/
│   ├── Services/
│   ├── Models/
│   └── Validators/
├── Integration/
│   ├── Controllers/
│   └── Database/
├── Performance/
└── TestUtilities/
    ├── Fixtures/
    └── Builders/
```

### 2. Test Data Builders
```csharp
public class ChildBuilder
{
    private Guid _id = Guid.NewGuid();
    private decimal _balance = 0;
    private decimal _weeklyAllowance = 10;

    public ChildBuilder WithBalance(decimal balance)
    {
        _balance = balance;
        return this;
    }

    public ChildBuilder WithAllowance(decimal allowance)
    {
        _weeklyAllowance = allowance;
        return this;
    }

    public Child Build()
    {
        return new Child
        {
            Id = _id,
            CurrentBalance = _balance,
            WeeklyAllowance = _weeklyAllowance,
            FamilyId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
    }
}

// Usage in tests
var child = new ChildBuilder()
    .WithBalance(50.00m)
    .WithAllowance(15.00m)
    .Build();
```

### 3. Custom Assertions
```csharp
public static class TransactionAssertions
{
    public static void ShouldBeValidTransaction(this Transaction transaction,
        decimal expectedAmount,
        TransactionType expectedType)
    {
        transaction.Should().NotBeNull();
        transaction.Amount.Should().Be(expectedAmount);
        transaction.Type.Should().Be(expectedType);
        transaction.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        transaction.Id.Should().NotBeEmpty();
    }
}
```

## Success Metrics

### Test Coverage Goals
- **Unit Tests**: 80% minimum
- **Integration Tests**: Critical paths only
- **Performance**: Sub-200ms response times

### Definition of Done
- [ ] Tests written and passing
- [ ] Code reviewed
- [ ] Documentation updated
- [ ] No compiler warnings
- [ ] Performance benchmarks met
- [ ] Deployed to staging