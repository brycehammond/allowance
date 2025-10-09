# Test-Driven Development Best Practices (.NET/C#)

## Core TDD Principles

### The Three Laws of TDD (Uncle Bob)
1. **You may not write production code until you have written a failing unit test**
2. **You may not write more of a unit test than is sufficient to fail**
3. **You may not write more production code than is sufficient to pass the currently failing test**

### Red-Green-Refactor Cycle
```
┌─────┐      ┌───────┐      ┌──────────┐
│ RED │ ───> │ GREEN │ ───> │ REFACTOR │
└─────┘      └───────┘      └──────────┘
   ↑                              │
   └──────────────────────────────┘
```

## Writing Good Tests

### Test Structure - AAA Pattern
```csharp
[Fact]
public async Task CreditTransaction_IncreasesBalance()
{
    // Arrange
    var child = await CreateTestChild(currentBalance: 100.00m);
    var dto = new CreateTransactionDto(
        child.Id,
        25.00m,
        TransactionType.Credit,
        "Test transaction"
    );

    // Act
    var transaction = await _transactionService.CreateTransactionAsync(dto);

    // Assert
    transaction.BalanceAfter.Should().Be(125.00m);
}
```

### Test Naming Conventions
```csharp
// Good - Describes behavior
[Fact]
public void DebitTransaction_PreventNegativeBalance()
[Fact]
public async Task AddAllowance_SendsNotificationEmail()
[Fact]
public void ExpiredToken_Returns401()

// Bad - Describes implementation
[Fact]
public void CallsCalculateBalanceMethod()
[Fact]
public void SetsBalanceVariable()
[Fact]
public void UsesTransactionClass()
```

### One Assertion Per Test
```csharp
// Good - Single assertion per test
[Fact]
public async Task ProcessPayment_CreatesTransactionRecord()
{
    // Act
    await _paymentService.ProcessPaymentAsync(paymentDto);

    // Assert
    var transactions = await _context.Transactions.CountAsync();
    transactions.Should().Be(1);
}

[Fact]
public async Task ProcessPayment_UpdatesBalance()
{
    // Arrange
    var child = await CreateTestChild(currentBalance: 100m);

    // Act
    await _paymentService.ProcessPaymentAsync(new PaymentDto(child.Id, 10m));

    // Assert
    child.CurrentBalance.Should().Be(90m);
}

// Bad - Multiple assertions in one test
[Fact]
public async Task ProcessPayment_WorksCorrectly()
{
    await _paymentService.ProcessPaymentAsync(paymentDto);

    _context.Transactions.Count().Should().Be(1);
    child.CurrentBalance.Should().Be(90m);
    _emailServiceMock.Verify(e => e.SendEmail(It.IsAny<Email>()), Times.Once);
    _logger.Messages.Should().Contain("Payment processed");
}
```

## TDD Patterns & Anti-Patterns

### Good Patterns ✅

#### 1. Test Behavior, Not Implementation
```csharp
// Good - Tests behavior
[Fact]
public async Task AddWeeklyAllowance_UpdatesBalanceCorrectly()
{
    // Arrange
    var child = new Child { WeeklyAllowance = 10.00m, CurrentBalance = 0m };

    // Act
    await _allowanceService.AddWeeklyAllowanceAsync(child.Id);

    // Assert
    var updatedChild = await _context.Children.FindAsync(child.Id);
    updatedChild.CurrentBalance.Should().Be(10.00m);
}

// Bad - Tests implementation details
[Fact]
public async Task AddWeeklyAllowance_CallsAddToBalance()
{
    // Mock setup to verify internal method call
    _mockChild.Setup(c => c.AddToBalance(10.00m)).Verifiable();

    await _allowanceService.AddWeeklyAllowanceAsync(childId);

    _mockChild.Verify();
}
```

#### 2. Use Builders/Factories for Test Data
```csharp
// Good - Using Builder pattern
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
    .WithWeeklyAllowance(15m)
    .Build();

// Bad - Manual setup in every test
var parent = new ApplicationUser
{
    Id = Guid.NewGuid(),
    Email = "test@example.com",
    FirstName = "John",
    LastName = "Doe",
    Role = UserRole.Parent
};
```

#### 3. Test Edge Cases
```csharp
public class BalanceValidationTests
{
    [Fact]
    public void Balance_AllowsZero()
    {
        var child = new Child { CurrentBalance = 0m };
        var validator = new ChildValidator();

        var result = validator.Validate(child);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Balance_PreventNegative()
    {
        var child = new Child { CurrentBalance = -1m };
        var validator = new ChildValidator();

        var result = validator.Validate(child);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CurrentBalance");
    }

    [Theory]
    [InlineData(999999.99)]
    [InlineData(decimal.MaxValue)]
    public void Balance_HandlesLargeValues(decimal amount)
    {
        var child = new Child { CurrentBalance = amount };
        var validator = new ChildValidator();

        var result = validator.Validate(child);

        result.IsValid.Should().BeTrue();
    }
}
```

### Anti-Patterns to Avoid ❌

#### 1. Testing Private Methods
```csharp
// Bad - Testing private methods directly
[Fact]
public void CalculateTax_ReturnsCorrectAmount()
{
    var order = new Order();
    var method = typeof(Order).GetMethod("CalculateTax", BindingFlags.NonPublic | BindingFlags.Instance);

    var tax = method.Invoke(order, new object[] { 100m });

    tax.Should().Be(5m);
}

// Good - Test through public interface
[Fact]
public void Total_IncludesTax()
{
    var order = new Order { Subtotal = 100m };

    var total = order.Total;

    total.Should().Be(105m); // $100 + $5 tax
}
```

#### 2. Over-Mocking
```csharp
// Bad - Too many mocks, lost connection to reality
[Fact]
public async Task ProcessOrder_CompletesSuccessfully()
{
    _paymentGatewayMock.Setup(p => p.ChargeAsync(It.IsAny<decimal>())).ReturnsAsync(true);
    _inventoryMock.Setup(i => i.ReduceAsync(It.IsAny<int>())).ReturnsAsync(true);
    _emailServiceMock.Setup(e => e.SendAsync(It.IsAny<Email>())).ReturnsAsync(true);
    _analyticsMock.Setup(a => a.TrackAsync(It.IsAny<Event>())).ReturnsAsync(true);

    await _orderService.ProcessAsync(order);

    // What are we really testing here?
}

// Good - Mock only external dependencies
[Fact]
public async Task ProcessOrder_ChargesPayment()
{
    // Only mock the external payment service
    _paymentGatewayMock.Setup(p => p.ChargeAsync(100m)).ReturnsAsync(true);

    await _orderService.ProcessAsync(order);

    var completedOrder = await _context.Orders.FindAsync(order.Id);
    completedOrder.Status.Should().Be(OrderStatus.Completed);
}
```

#### 3. Brittle Tests
```csharp
// Bad - Depends on exact string format
[Fact]
public async Task UserInfo_DisplaysCorrectFormat()
{
    var result = await _userService.GetUserInfoAsync(userId);

    result.Should().Be("John Doe (john@example.com) - Parent");
}

// Good - Tests presence of information
[Fact]
public async Task UserInfo_ContainsRequiredData()
{
    var result = await _userService.GetUserInfoAsync(userId);

    result.Should().Contain("John Doe");
    result.Should().Contain("john@example.com");
    result.Should().Contain("Parent");
}
```

## TDD for Different Layers

### Domain Model Tests (100% Coverage Required)
```csharp
public class UserTests
{
    [Fact]
    public void Email_IsRequired()
    {
        var user = new ApplicationUser { Email = null };
        var validator = new UserValidator();

        var result = validator.Validate(user);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ApplicationUser.Email));
    }

    [Fact]
    public void User_HasFamilyRelationship()
    {
        var user = new ApplicationUser();
        var family = new Family();

        user.Family = family;

        user.Family.Should().BeSameAs(family);
    }

    [Theory]
    [InlineData(UserRole.Parent, true)]
    [InlineData(UserRole.Child, false)]
    public void CanManageChildren_BasedOnRole(UserRole role, bool expected)
    {
        var user = new ApplicationUser { Role = role };

        var canManage = user.CanManageChildren();

        canManage.Should().Be(expected);
    }
}
```

### API Controller Tests (95% Coverage Required)
```csharp
public class TransactionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    [Fact]
    public async Task CreateTransaction_WithValidData_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateTransactionDto(childId, 10.00m, TransactionType.Credit, "Test");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<ApiResponse<TransactionDto>>();
        content.Data.Amount.Should().Be(10.00m);
    }

    [Fact]
    public async Task CreateTransaction_WithoutAuth_Returns401()
    {
        // Arrange
        var dto = new CreateTransactionDto(childId, 10.00m, TransactionType.Credit, "Test");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/transactions", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

### Blazor Component Tests (80% Coverage)
```csharp
public class ParentDashboardTests : TestContext
{
    [Fact]
    public void Dashboard_DisplaysChildrenCards()
    {
        // Arrange
        var children = new List<Child>
        {
            new Child { FirstName = "Alice", CurrentBalance = 100m },
            new Child { FirstName = "Bob", CurrentBalance = 50m }
        };

        // Act
        var component = RenderComponent<ParentDashboard>(parameters => parameters
            .Add(p => p.Children, children));

        // Assert
        component.FindAll(".child-card").Count.Should().Be(2);
        component.Find(".child-card:first-child").TextContent.Should().Contain("Alice");
        component.Find(".child-card:first-child").TextContent.Should().Contain("$100.00");
    }

    [Fact]
    public void AddTransaction_UpdatesBalance()
    {
        // Arrange
        var component = RenderComponent<TransactionForm>();

        // Act
        component.Find("#amount").Change("25.00");
        component.Find("#type").Change("debit");
        component.Find("#submit").Click();

        // Assert
        component.Find(".balance").TextContent.Should().Contain("$75.00");
    }
}
```

## TDD Workflow Commands

### Essential Commands
```bash
# Create test project
dotnet new xunit -n AllowanceTracker.Tests
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Microsoft.AspNetCore.Mvc.Testing

# Run tests
dotnet test                           # All tests
dotnet test --filter FullyQualifiedName~ModelTests  # Specific namespace
dotnet test --filter "Priority=Critical"             # By trait
dotnet test --no-build               # Skip build
dotnet test --collect:"XPlat Code Coverage"  # With coverage

# Watch mode (continuous testing)
dotnet watch test

# Coverage report
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
reportgenerator -reports:coverage.opencover.xml -targetdir:coveragereport
```

### Test Configuration for TDD
```json
// .vscode/settings.json
{
  "dotnet-test-explorer.testProjectPath": "**/*.Tests.csproj",
  "dotnet-test-explorer.runInParallel": true,
  "dotnet-test-explorer.showCodeLens": true,
  "dotnet-test-explorer.autoWatch": true
}
```

## TDD Metrics & Goals

### Coverage Requirements
| Layer | Minimum | Target | Priority |
|-------|---------|--------|----------|
| Domain Models | 95% | 100% | Critical |
| Services | 90% | 95% | Critical |
| API Controllers | 90% | 95% | High |
| Blazor Components | 75% | 85% | Medium |
| Background Jobs | 85% | 90% | High |
| Validators | 95% | 100% | Critical |
| Utilities | 80% | 90% | Low |

### Test Speed Goals
- Unit tests: < 5ms per test
- Integration tests: < 100ms per test
- E2E tests: < 1s per test
- Full suite: < 2 minutes

### Test Quality Metrics
- **Test-to-Code Ratio**: Aim for 1.2:1 or higher
- **Mutation Testing Score**: 80%+ (using Stryker.NET)
- **Flaky Tests**: 0 tolerance
- **Test Independence**: 100% (tests can run in any order)

## Common TDD Scenarios

### Scenario: Adding New Model Validation
```csharp
// 1. Write failing test
[Fact]
public void WeeklyAllowance_MustBePositive()
{
    // Arrange
    var child = new Child { WeeklyAllowance = -5m };
    var validator = new ChildValidator();

    // Act
    var result = validator.Validate(child);

    // Assert
    result.IsValid.Should().BeFalse();
    result.Errors.Should().Contain(e =>
        e.PropertyName == "WeeklyAllowance" &&
        e.ErrorMessage.Contains("greater than or equal to 0"));
}

// 2. Run test - RED ❌
// 3. Add validation rule
public class ChildValidator : AbstractValidator<Child>
{
    public ChildValidator()
    {
        RuleFor(c => c.WeeklyAllowance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Weekly allowance must be greater than or equal to 0");
    }
}

// 4. Run test - GREEN ✅
// 5. Refactor if needed
```

### Scenario: Adding API Endpoint
```csharp
// 1. Write request test first
[Fact]
public async Task GetBalance_ReturnsCurrentBalance()
{
    // Arrange
    var child = await CreateTestChild(currentBalance: 150.00m);
    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

    // Act
    var response = await _client.GetAsync($"/api/v1/children/{child.Id}/balance");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var content = await response.Content.ReadFromJsonAsync<BalanceResponse>();
    content.Balance.Should().Be(150.00m);
}

// 2. Add route to controller
// 3. Implement action method
// 4. Run test until GREEN
```

### Scenario: Refactoring with Confidence
```csharp
// Before refactoring - ensure all tests pass
dotnet test // All GREEN ✅

// Refactor the implementation
public class TransactionService
{
    // Old implementation
    // public async Task<decimal> CalculateBalance(Guid childId)
    // {
    //     var transactions = await _context.Transactions
    //         .Where(t => t.ChildId == childId)
    //         .ToListAsync();
    //     return transactions.Sum(t => t.Amount);
    // }

    // New implementation - more efficient
    public async Task<decimal> CalculateBalance(Guid childId)
    {
        return await _context.Transactions
            .Where(t => t.ChildId == childId)
            .SumAsync(t => t.Amount);
    }
}

// Tests still pass = Refactoring successful
dotnet test // Still all GREEN ✅
```

## TDD Checklist

Before writing ANY production code:
- [ ] Is there a failing test?
- [ ] Does the test describe behavior, not implementation?
- [ ] Is the test name clear and descriptive?
- [ ] Have I run the test to see it fail?

Before committing code:
- [ ] Are all tests passing?
- [ ] Is test coverage adequate (>90%)?
- [ ] Are tests independent?
- [ ] Can another developer understand the tests?
- [ ] Do tests serve as documentation?

## Resources

### Books
- "Test Driven Development: By Example" - Kent Beck
- "Growing Object-Oriented Software, Guided by Tests" - Freeman & Pryce
- "Unit Testing Principles, Practices, and Patterns" - Vladimir Khorikov
- "The Art of Unit Testing" - Roy Osherove

### Tools
- xUnit: https://xunit.net/
- FluentAssertions: https://fluentassertions.com/
- Moq: https://github.com/moq/moq4
- Bogus: https://github.com/bchavez/Bogus
- Stryker.NET: https://stryker-mutator.io/docs/stryker-net/
- Coverlet: https://github.com/coverlet-coverage/coverlet
- bUnit: https://bunit.dev/

### Best Practices
- .NET Testing Best Practices: https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices
- xUnit Patterns: http://xunitpatterns.com/
- Microsoft Testing Guidelines: https://github.com/dotnet/AspNetCore.Docs/tree/main/aspnetcore/test