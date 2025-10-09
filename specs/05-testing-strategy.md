# Testing Strategy - .NET Test-Driven Development

## Core Philosophy: Test-First Development
**Every feature starts with a failing test**. We follow strict TDD methodology:
1. **Red**: Write a failing test that defines desired behavior
2. **Green**: Write minimum code to make the test pass
3. **Refactor**: Improve code while keeping tests green
4. **Repeat**: Continue for next feature

## TDD Principles
- **No production code without a failing test first**
- **One test, one assertion** - Keep tests focused
- **Test behavior, not implementation** - Tests should survive refactoring
- **Fast feedback loop** - Run tests continuously during development
- **Tests as living documentation** - Tests explain what code does

## Overview
Test-Driven Development approach using xUnit for .NET, ensuring quality through test-first methodology with a focus on performance.

## Testing Stack

### Core Tools
- **xUnit**: Main testing framework (preferred over MSTest/NUnit)
- **FluentAssertions**: Readable assertion syntax
- **Moq**: Mocking framework
- **Bogus**: Realistic test data generation (Faker for .NET)
- **TestContainers**: Database testing with containers
- **BenchmarkDotNet**: Performance testing
- **bUnit**: Blazor component testing
- **WebApplicationFactory**: Integration testing

### Package References
```xml
<ItemGroup>
  <!-- Testing Frameworks -->
  <PackageReference Include="xunit" Version="2.6.1" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />

  <!-- Assertion & Mocking -->
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="Moq" Version="4.20.69" />

  <!-- Test Data -->
  <PackageReference Include="Bogus" Version="35.3.0" />

  <!-- Integration Testing -->
  <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
  <PackageReference Include="Testcontainers.PostgreSql" Version="3.6.0" />

  <!-- Performance -->
  <PackageReference Include="BenchmarkDotNet" Version="0.13.10" />

  <!-- Blazor Testing -->
  <PackageReference Include="bunit" Version="1.24.10" />
</ItemGroup>
```

## Test Categories

### 1. Unit Tests (Domain/Services)
**Coverage Target: 80%**

#### User Model Tests
```csharp
public class ApplicationUserTests
{
    [Fact]
    public void User_Should_Have_Valid_Email()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "invalid-email"
        };

        // Act
        var validator = new UserValidator();
        var result = validator.Validate(user);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Parent_Should_Create_Family_On_Registration()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Email = "parent@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent
        };

        // Act
        var family = user.CreateFamily();

        // Assert
        family.Should().NotBeNull();
        family.Name.Should().Be("Doe Family");
        user.FamilyId.Should().Be(family.Id);
    }
}
```

#### Transaction Service Tests
```csharp
public class TransactionServiceTests
{
    private readonly Mock<IAllowanceContext> _mockContext;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _mockContext = new Mock<IAllowanceContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _service = new TransactionService(_mockContext.Object, _mockCurrentUser.Object);
    }

    [Fact]
    public async Task CreateTransaction_Should_Update_Balance()
    {
        // Arrange
        var child = new Child { Id = Guid.NewGuid(), CurrentBalance = 100m };
        var dto = new CreateTransactionDto(child.Id, 25m, TransactionType.Credit, "Test");

        _mockContext.Setup(x => x.Children.FindAsync(child.Id))
            .ReturnsAsync(child);

        // Act
        var result = await _service.CreateTransactionAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.BalanceAfter.Should().Be(125m);
        child.CurrentBalance.Should().Be(125m);
    }

    [Theory]
    [InlineData(100, 150, TransactionType.Debit)]
    [InlineData(50, 100, TransactionType.Debit)]
    public async Task CreateTransaction_Should_Prevent_Overdraft(
        decimal balance, decimal amount, TransactionType type)
    {
        // Arrange
        var child = new Child { CurrentBalance = balance };
        var dto = new CreateTransactionDto(Guid.NewGuid(), amount, type, "Test");

        // Act
        Func<Task> act = () => _service.CreateTransactionAsync(dto);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Insufficient funds");
    }
}
```

### 2. Integration Tests
**Coverage Target: Critical Paths**

#### API Integration Tests
```csharp
public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_Should_Create_User_And_Return_Token()
    {
        // Arrange
        var registerDto = new RegisterDto(
            "test@example.com",
            "Password123!",
            "John",
            "Doe",
            UserRole.Parent);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        content.Should().NotBeNull();
        content.Token.Should().NotBeNullOrEmpty();
        content.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Protected_Endpoint_Should_Require_Authentication()
    {
        // Act
        var response = await _client.GetAsync("/api/family/children");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

#### Database Integration Tests with TestContainers
```csharp
public class DatabaseIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgres;
    private AllowanceContext _context;

    public async Task InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder()
            .WithDatabase("test_db")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new AllowanceContext(options);
        await _context.Database.MigrateAsync();
    }

    [Fact]
    public async Task Should_Persist_Transaction_With_Balance_Snapshot()
    {
        // Arrange
        var child = new Child
        {
            UserId = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            CurrentBalance = 100m
        };

        _context.Children.Add(child);
        await _context.SaveChangesAsync();

        // Act
        var transaction = new Transaction
        {
            ChildId = child.Id,
            Amount = 25m,
            Type = TransactionType.Credit,
            Description = "Test",
            BalanceAfter = 125m,
            CreatedById = Guid.NewGuid()
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _context.Transactions.FindAsync(transaction.Id);
        saved.Should().NotBeNull();
        saved.BalanceAfter.Should().Be(125m);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
```

### 3. Blazor Component Tests
**Coverage Target: UI Logic**

```csharp
public class DashboardComponentTests : TestContext
{
    [Fact]
    public void Dashboard_Should_Display_Children()
    {
        // Arrange
        var mockFamilyService = new Mock<IFamilyService>();
        var children = new List<ChildDto>
        {
            new(Guid.NewGuid(), "Jane Doe", 10m, 25.50m, null),
            new(Guid.NewGuid(), "John Doe", 15m, 50m, null)
        };

        mockFamilyService.Setup(x => x.GetChildrenAsync())
            .ReturnsAsync(children);

        Services.AddSingleton(mockFamilyService.Object);

        // Act
        var component = RenderComponent<Dashboard>();

        // Assert
        component.Find("h1").TextContent.Should().Contain("Family Dashboard");

        var childCards = component.FindAll(".child-card");
        childCards.Count.Should().Be(2);

        childCards[0].TextContent.Should().Contain("Jane Doe");
        childCards[0].TextContent.Should().Contain("$25.50");
    }

    [Fact]
    public void TransactionForm_Should_Validate_Input()
    {
        // Arrange & Act
        var component = RenderComponent<TransactionForm>();
        var form = component.Find("form");

        // Submit without filling fields
        form.Submit();

        // Assert
        var validationMessages = component.FindAll(".validation-message");
        validationMessages.Should().NotBeEmpty();
        validationMessages.Should().Contain(m => m.TextContent.Contains("Amount is required"));
    }
}
```

### 4. Performance Tests
**Target: Sub-200ms response times**

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class TransactionBenchmarks
{
    private TransactionService _service;
    private CreateTransactionDto _dto;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddDbContext<AllowanceContext>(options =>
            options.UseInMemoryDatabase("bench"));
        services.AddScoped<ITransactionService, TransactionService>();

        var provider = services.BuildServiceProvider();
        _service = provider.GetRequiredService<TransactionService>();

        _dto = new CreateTransactionDto(
            Guid.NewGuid(), 10m, TransactionType.Credit, "Benchmark");
    }

    [Benchmark]
    public async Task CreateSingleTransaction()
    {
        await _service.CreateTransactionAsync(_dto);
    }

    [Benchmark]
    public async Task Create100TransactionsConcurrently()
    {
        var tasks = Enumerable.Range(0, 100)
            .Select(_ => _service.CreateTransactionAsync(_dto));

        await Task.WhenAll(tasks);
    }
}
```

### 5. End-to-End Tests
**Coverage: Critical User Journeys**

```csharp
public class UserJourneyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    [Fact]
    public async Task Complete_Parent_Child_Flow()
    {
        // 1. Register parent
        var parentEmail = $"parent_{Guid.NewGuid()}@test.com";
        var registerResponse = await RegisterUser(parentEmail, UserRole.Parent);
        var parentToken = registerResponse.Token;

        // 2. Add child to family
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", parentToken);

        var childDto = new AddChildDto
        {
            Email = $"child_{Guid.NewGuid()}@test.com",
            Password = "ChildPass123!",
            FirstName = "Jane",
            LastName = "Doe",
            WeeklyAllowance = 20m
        };

        var childResponse = await _client.PostAsJsonAsync("/api/family/children", childDto);
        childResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var child = await childResponse.Content.ReadFromJsonAsync<ChildDto>();

        // 3. Add transaction
        var transactionDto = new CreateTransactionDto(
            child.Id, 10m, TransactionType.Credit, "Test allowance");

        var transResponse = await _client.PostAsJsonAsync(
            $"/api/children/{child.Id}/transactions", transactionDto);

        transResponse.IsSuccessStatusCode.Should().BeTrue();

        // 4. Verify balance
        var balanceResponse = await _client.GetAsync($"/api/children/{child.Id}");
        var updatedChild = await balanceResponse.Content.ReadFromJsonAsync<ChildDto>();

        updatedChild.CurrentBalance.Should().Be(10m);
    }
}
```

## Test Data Generation

### Using Bogus for Realistic Data
```csharp
public class TestDataGenerator
{
    private readonly Faker<ApplicationUser> _userFaker;
    private readonly Faker<Child> _childFaker;

    public TestDataGenerator()
    {
        _userFaker = new Faker<ApplicationUser>()
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Role, f => f.PickRandom<UserRole>());

        _childFaker = new Faker<Child>()
            .RuleFor(c => c.Id, f => Guid.NewGuid())
            .RuleFor(c => c.WeeklyAllowance, f => f.Random.Decimal(5, 50))
            .RuleFor(c => c.CurrentBalance, f => f.Random.Decimal(0, 100))
            .RuleFor(c => c.UserId, f => Guid.NewGuid())
            .RuleFor(c => c.FamilyId, f => Guid.NewGuid());
    }

    public ApplicationUser GenerateUser() => _userFaker.Generate();
    public Child GenerateChild() => _childFaker.Generate();
    public List<Child> GenerateChildren(int count) => _childFaker.Generate(count);
}
```

## Test Coverage Goals

### Minimum Coverage Requirements
- **Overall**: 70% (MVP target)
- **Domain Models**: 80%
- **Services**: 80%
- **Controllers**: 70%
- **Critical Paths**: 100%

### Critical Path Coverage
These paths must have 100% coverage:
- Authentication flow
- Transaction creation and balance updates
- Weekly allowance processing
- Balance integrity checks
- JWT token generation and validation

## Performance Testing Strategy

### Response Time Benchmarks
```csharp
[Fact]
public async Task Api_Should_Respond_Within_200ms()
{
    var stopwatch = Stopwatch.StartNew();

    var response = await _client.GetAsync("/api/family/children");

    stopwatch.Stop();
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(200);
}
```

### Load Testing
```csharp
[Fact]
public async Task Should_Handle_Concurrent_Transactions()
{
    // Arrange
    var tasks = new List<Task<HttpResponseMessage>>();

    // Act - 100 concurrent requests
    for (int i = 0; i < 100; i++)
    {
        tasks.Add(_client.PostAsJsonAsync("/api/transactions", new TransactionDto()));
    }

    var responses = await Task.WhenAll(tasks);

    // Assert
    responses.Should().AllSatisfy(r => r.IsSuccessStatusCode.Should().BeTrue());
}
```

## Security Testing

```csharp
public class SecurityTests
{
    [Theory]
    [InlineData("admin' OR '1'='1")]
    [InlineData("'; DROP TABLE Users; --")]
    public async Task Should_Prevent_SQL_Injection(string maliciousInput)
    {
        // Arrange
        var loginDto = new LoginDto { Email = maliciousInput, Password = "test" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Should_Reject_Expired_Tokens()
    {
        // Arrange
        var expiredToken = GenerateExpiredToken();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _client.GetAsync("/api/protected");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

## Continuous Integration

### GitHub Actions Configuration
```yaml
name: .NET CI/CD

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:15
        env:
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

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
      env:
        ConnectionStrings__DefaultConnection: "Host=localhost;Database=test;Username=postgres;Password=postgres"

    - name: Upload coverage to Codecov
      uses: codecov/codecov-action@v3
      with:
        files: ./coverage.cobertura.xml

    - name: Run benchmarks
      run: dotnet run -c Release --project AllowanceTracker.Benchmarks
```

## Test Execution Commands

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test AllowanceTracker.Tests

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test
dotnet test --filter "FullyQualifiedName~TransactionServiceTests"

# Run by category
dotnet test --filter "Category=Integration"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Watch mode
dotnet watch test

# Run benchmarks
dotnet run -c Release --project AllowanceTracker.Benchmarks
```

## Testing Best Practices

1. **AAA Pattern**: Arrange, Act, Assert
2. **Test Isolation**: Each test independent
3. **Descriptive Names**: Should_ExpectedBehavior_When_StateUnderTest
4. **Fast Tests**: Mock external dependencies
5. **Deterministic**: Same result every time
6. **No Logic in Tests**: Keep tests simple
7. **Test One Thing**: Single assertion per test
8. **Use Test Data Builders**: Reduce test setup duplication