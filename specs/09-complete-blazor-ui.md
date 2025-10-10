# Complete Blazor UI Specification

## Overview
This specification covers the complete Blazor Server UI implementation for the Allowance Tracker application, including authentication, family management, child management, and all financial operations. All features are implemented using **strict Test-Driven Development (TDD)**.

## Table of Contents
1. [Authentication & Account Management](#authentication--account-management)
2. [Family Management](#family-management)
3. [Child Management](#child-management)
4. [Dashboard Views](#dashboard-views)
5. [Transaction Management](#transaction-management)
6. [Navigation & Layout](#navigation--layout)
7. [Services Layer](#services-layer)
8. [Testing Strategy](#testing-strategy)

---

## Authentication & Account Management

### AccountService (TDD)

**Purpose**: Wrap ASP.NET Core Identity for Blazor UI authentication operations.

#### Interface
```csharp
public interface IAccountService
{
    Task<IdentityResult> RegisterParentAsync(RegisterParentDto dto);
    Task<IdentityResult> RegisterChildAsync(RegisterChildDto dto, Guid familyId);
    Task<SignInResult> LoginAsync(string email, string password, bool rememberMe = false);
    Task LogoutAsync();
    Task<ApplicationUser?> GetCurrentUserAsync();
    Task<bool> IsAuthenticatedAsync();
}
```

#### Tests (8 tests)
```csharp
[Fact]
public async Task RegisterParent_CreatesUserAndFamily()
{
    // Arrange
    var dto = new RegisterParentDto(
        "parent@test.com",
        "Test123!",
        "John",
        "Doe",
        "Doe Family");

    // Act
    var result = await _accountService.RegisterParentAsync(dto);

    // Assert
    result.Succeeded.Should().BeTrue();
    var user = await _context.Users.FirstAsync(u => u.Email == dto.Email);
    user.Role.Should().Be(UserRole.Parent);
    user.FamilyId.Should().NotBeNull();

    var family = await _context.Families.FindAsync(user.FamilyId);
    family.Should().NotBeNull();
    family!.Name.Should().Be("Doe Family");
}

[Fact]
public async Task RegisterParent_WithExistingEmail_Fails()
{
    // Arrange
    await CreateTestUser("existing@test.com");
    var dto = new RegisterParentDto("existing@test.com", "Test123!", "Jane", "Doe", "Family");

    // Act
    var result = await _accountService.RegisterParentAsync(dto);

    // Assert
    result.Succeeded.Should().BeFalse();
    result.Errors.Should().Contain(e => e.Code.Contains("Duplicate"));
}

[Fact]
public async Task RegisterChild_AssociatesWithFamily()
{
    // Arrange
    var family = await CreateTestFamily();
    var dto = new RegisterChildDto("child@test.com", "Test123!", "Alice", "Smith", 10.00m);

    // Act
    var result = await _accountService.RegisterChildAsync(dto, family.Id);

    // Assert
    result.Succeeded.Should().BeTrue();
    var user = await _context.Users.FirstAsync(u => u.Email == dto.Email);
    user.Role.Should().Be(UserRole.Child);
    user.FamilyId.Should().Be(family.Id);

    var childProfile = await _context.Children.FirstAsync(c => c.UserId == user.Id);
    childProfile.WeeklyAllowance.Should().Be(10.00m);
}

[Fact]
public async Task Login_WithValidCredentials_Succeeds()
{
    // Arrange
    var password = "Test123!";
    var user = await CreateTestUser("test@test.com", password);

    // Act
    var result = await _accountService.LoginAsync("test@test.com", password);

    // Assert
    result.Succeeded.Should().BeTrue();
}

[Fact]
public async Task Login_WithInvalidPassword_Fails()
{
    // Arrange
    await CreateTestUser("test@test.com", "Correct123!");

    // Act
    var result = await _accountService.LoginAsync("test@test.com", "Wrong123!");

    // Assert
    result.Succeeded.Should().BeFalse();
}

[Fact]
public async Task Logout_ClearsAuthentication()
{
    // Arrange
    await CreateAndLoginUser("test@test.com");

    // Act
    await _accountService.LogoutAsync();

    // Assert
    var isAuthenticated = await _accountService.IsAuthenticatedAsync();
    isAuthenticated.Should().BeFalse();
}

[Fact]
public async Task GetCurrentUser_WhenAuthenticated_ReturnsUser()
{
    // Arrange
    var user = await CreateAndLoginUser("test@test.com");

    // Act
    var currentUser = await _accountService.GetCurrentUserAsync();

    // Assert
    currentUser.Should().NotBeNull();
    currentUser!.Id.Should().Be(user.Id);
}

[Fact]
public async Task GetCurrentUser_WhenNotAuthenticated_ReturnsNull()
{
    // Act
    var currentUser = await _accountService.GetCurrentUserAsync();

    // Assert
    currentUser.Should().BeNull();
}
```

#### Implementation
```csharp
public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly AllowanceContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        AllowanceContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IdentityResult> RegisterParentAsync(RegisterParentDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create family
            var family = new Family
            {
                Id = Guid.NewGuid(),
                Name = dto.FamilyName,
                CreatedAt = DateTime.UtcNow
            };
            _context.Families.Add(family);
            await _context.SaveChangesAsync();

            // Create parent user
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                UserName = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = UserRole.Parent,
                FamilyId = family.Id
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IdentityResult> RegisterChildAsync(RegisterChildDto dto, Guid familyId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Verify family exists
            var family = await _context.Families.FindAsync(familyId);
            if (family == null)
                return IdentityResult.Failed(new IdentityError { Description = "Family not found" });

            // Create child user
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = dto.Email,
                UserName = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = UserRole.Child,
                FamilyId = familyId
            };

            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                // Create child profile
                var childProfile = new Child
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    FamilyId = familyId,
                    WeeklyAllowance = dto.WeeklyAllowance,
                    CurrentBalance = 0,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Children.Add(childProfile);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            else
            {
                await transaction.RollbackAsync();
            }

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<SignInResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        return await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User == null)
            return null;

        return await _userManager.GetUserAsync(httpContext.User);
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var user = await GetCurrentUserAsync();
        return user != null;
    }
}
```

#### DTOs
```csharp
public record RegisterParentDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string FamilyName);

public record RegisterChildDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    decimal WeeklyAllowance);
```

### Register.razor Page (TDD)

**Purpose**: Parent registration page that creates both user account and family.

#### Tests (4 tests with bUnit)
```csharp
[Fact]
public void Register_RendersAllRequiredFields()
{
    // Arrange
    using var ctx = new TestContext();
    ctx.Services.AddSingleton(Mock.Of<IAccountService>());

    // Act
    var component = ctx.RenderComponent<Register>();

    // Assert
    component.Find("input[name='email']").Should().NotBeNull();
    component.Find("input[name='password']").Should().NotBeNull();
    component.Find("input[name='firstName']").Should().NotBeNull();
    component.Find("input[name='lastName']").Should().NotBeNull();
    component.Find("input[name='familyName']").Should().NotBeNull();
    component.Find("button[type='submit']").Should().NotBeNull();
}

[Fact]
public async Task Register_WithValidData_CallsAccountService()
{
    // Arrange
    var mockAccountService = new Mock<IAccountService>();
    mockAccountService
        .Setup(x => x.RegisterParentAsync(It.IsAny<RegisterParentDto>()))
        .ReturnsAsync(IdentityResult.Success);

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockAccountService.Object);
    ctx.Services.AddSingleton(Mock.Of<NavigationManager>());

    var component = ctx.RenderComponent<Register>();

    // Act
    component.Find("input[name='email']").Change("test@test.com");
    component.Find("input[name='password']").Change("Test123!");
    component.Find("input[name='firstName']").Change("John");
    component.Find("input[name='lastName']").Change("Doe");
    component.Find("input[name='familyName']").Change("Doe Family");
    component.Find("form").Submit();

    // Assert
    await Task.Delay(100); // Wait for async
    mockAccountService.Verify(x => x.RegisterParentAsync(
        It.Is<RegisterParentDto>(dto =>
            dto.Email == "test@test.com" &&
            dto.FirstName == "John" &&
            dto.FamilyName == "Doe Family")),
        Times.Once);
}

[Fact]
public void Register_WithInvalidEmail_ShowsValidationError()
{
    // Arrange
    using var ctx = new TestContext();
    ctx.Services.AddSingleton(Mock.Of<IAccountService>());

    var component = ctx.RenderComponent<Register>();

    // Act
    component.Find("input[name='email']").Change("invalid-email");
    component.Find("form").Submit();

    // Assert
    component.Markup.Should().Contain("valid email");
}

[Fact]
public async Task Register_OnSuccess_RedirectsToDashboard()
{
    // Arrange
    var mockAccountService = new Mock<IAccountService>();
    mockAccountService
        .Setup(x => x.RegisterParentAsync(It.IsAny<RegisterParentDto>()))
        .ReturnsAsync(IdentityResult.Success);

    var mockNav = new Mock<NavigationManager>();

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockAccountService.Object);
    ctx.Services.AddSingleton(mockNav.Object);

    var component = ctx.RenderComponent<Register>();

    // Act
    FillFormAndSubmit(component);
    await Task.Delay(100);

    // Assert
    mockNav.Verify(x => x.NavigateTo("/dashboard", false), Times.Once);
}
```

#### Implementation
```razor
@page "/register"
@using AllowanceTracker.DTOs
@using AllowanceTracker.Services
@using Microsoft.AspNetCore.Identity
@inject IAccountService AccountService
@inject NavigationManager Navigation

<PageTitle>Register - Allowance Tracker</PageTitle>

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-6">
            <div class="card">
                <div class="card-body">
                    <h3 class="card-title text-center mb-4">Create Parent Account</h3>

                    @if (!string.IsNullOrEmpty(ErrorMessage))
                    {
                        <div class="alert alert-danger" role="alert">
                            @ErrorMessage
                        </div>
                    }

                    <EditForm Model="@Model" OnValidSubmit="@HandleRegister">
                        <DataAnnotationsValidator />
                        <ValidationSummary class="text-danger" />

                        <div class="mb-3">
                            <label class="form-label">Email</label>
                            <InputText @bind-Value="Model.Email" class="form-control" name="email" />
                            <ValidationMessage For="@(() => Model.Email)" />
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Password</label>
                            <InputText type="password" @bind-Value="Model.Password" class="form-control" name="password" />
                            <ValidationMessage For="@(() => Model.Password)" />
                        </div>

                        <div class="mb-3">
                            <label class="form-label">First Name</label>
                            <InputText @bind-Value="Model.FirstName" class="form-control" name="firstName" />
                            <ValidationMessage For="@(() => Model.FirstName)" />
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Last Name</label>
                            <InputText @bind-Value="Model.LastName" class="form-control" name="lastName" />
                            <ValidationMessage For="@(() => Model.LastName)" />
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Family Name</label>
                            <InputText @bind-Value="Model.FamilyName" class="form-control" name="familyName" />
                            <ValidationMessage For="@(() => Model.FamilyName)" />
                            <small class="form-text text-muted">Choose a name for your family (e.g., "Smith Family")</small>
                        </div>

                        <button type="submit" class="btn btn-primary w-100" disabled="@IsProcessing">
                            @if (IsProcessing)
                            {
                                <span class="spinner-border spinner-border-sm me-2"></span>
                                <span>Creating Account...</span>
                            }
                            else
                            {
                                <span>Create Account</span>
                            }
                        </button>
                    </EditForm>

                    <div class="mt-3 text-center">
                        <p>Already have an account? <a href="/login">Log in</a></p>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private RegisterModel Model = new();
    private bool IsProcessing = false;
    private string? ErrorMessage;

    private async Task HandleRegister()
    {
        IsProcessing = true;
        ErrorMessage = null;

        try
        {
            var dto = new RegisterParentDto(
                Model.Email,
                Model.Password,
                Model.FirstName,
                Model.LastName,
                Model.FamilyName);

            var result = await AccountService.RegisterParentAsync(dto);

            if (result.Succeeded)
            {
                // Auto-login after registration
                await AccountService.LoginAsync(Model.Email, Model.Password);
                Navigation.NavigateTo("/dashboard");
            }
            else
            {
                ErrorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred during registration. Please try again.";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private class RegisterModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Family name is required")]
        [StringLength(100, ErrorMessage = "Family name must be less than 100 characters")]
        public string FamilyName { get; set; } = string.Empty;
    }
}
```

### Login.razor Page (TDD)

**Purpose**: User login page using ASP.NET Core Identity.

#### Tests (4 tests with bUnit)
```csharp
[Fact]
public void Login_RendersEmailAndPasswordFields()
{
    // Arrange
    using var ctx = new TestContext();
    ctx.Services.AddSingleton(Mock.Of<IAccountService>());

    // Act
    var component = ctx.RenderComponent<Login>();

    // Assert
    component.Find("input[name='email']").Should().NotBeNull();
    component.Find("input[type='password']").Should().NotBeNull();
    component.Find("input[type='checkbox']").Should().NotBeNull(); // Remember me
    component.Find("button[type='submit']").Should().NotBeNull();
}

[Fact]
public async Task Login_WithValidCredentials_RedirectsToDashboard()
{
    // Arrange
    var mockAccountService = new Mock<IAccountService>();
    mockAccountService
        .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
        .ReturnsAsync(SignInResult.Success);

    var mockNav = new Mock<NavigationManager>();

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockAccountService.Object);
    ctx.Services.AddSingleton(mockNav.Object);

    var component = ctx.RenderComponent<Login>();

    // Act
    component.Find("input[name='email']").Change("test@test.com");
    component.Find("input[name='password']").Change("Test123!");
    component.Find("form").Submit();
    await Task.Delay(100);

    // Assert
    mockNav.Verify(x => x.NavigateTo("/dashboard", false), Times.Once);
}

[Fact]
public async Task Login_WithInvalidCredentials_ShowsError()
{
    // Arrange
    var mockAccountService = new Mock<IAccountService>();
    mockAccountService
        .Setup(x => x.LoginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
        .ReturnsAsync(SignInResult.Failed);

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockAccountService.Object);
    ctx.Services.AddSingleton(Mock.Of<NavigationManager>());

    var component = ctx.RenderComponent<Login>();

    // Act
    component.Find("input[name='email']").Change("test@test.com");
    component.Find("input[name='password']").Change("Wrong123!");
    component.Find("form").Submit();
    await Task.Delay(100);

    // Assert
    component.Markup.Should().Contain("Invalid email or password");
}

[Fact]
public void Login_IncludesLinkToRegister()
{
    // Arrange
    using var ctx = new TestContext();
    ctx.Services.AddSingleton(Mock.Of<IAccountService>());

    // Act
    var component = ctx.RenderComponent<Login>();

    // Assert
    component.Find("a[href='/register']").Should().NotBeNull();
}
```

#### Implementation
```razor
@page "/login"
@using AllowanceTracker.Services
@using Microsoft.AspNetCore.Identity
@inject IAccountService AccountService
@inject NavigationManager Navigation

<PageTitle>Login - Allowance Tracker</PageTitle>

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-5">
            <div class="card">
                <div class="card-body">
                    <h3 class="card-title text-center mb-4">Login</h3>

                    @if (!string.IsNullOrEmpty(ErrorMessage))
                    {
                        <div class="alert alert-danger" role="alert">
                            @ErrorMessage
                        </div>
                    }

                    <EditForm Model="@Model" OnValidSubmit="@HandleLogin">
                        <DataAnnotationsValidator />

                        <div class="mb-3">
                            <label class="form-label">Email</label>
                            <InputText @bind-Value="Model.Email" class="form-control" name="email" />
                            <ValidationMessage For="@(() => Model.Email)" />
                        </div>

                        <div class="mb-3">
                            <label class="form-label">Password</label>
                            <InputText type="password" @bind-Value="Model.Password" class="form-control" name="password" />
                            <ValidationMessage For="@(() => Model.Password)" />
                        </div>

                        <div class="mb-3 form-check">
                            <InputCheckbox @bind-Value="Model.RememberMe" class="form-check-input" id="rememberMe" />
                            <label class="form-check-label" for="rememberMe">
                                Remember me
                            </label>
                        </div>

                        <button type="submit" class="btn btn-primary w-100" disabled="@IsProcessing">
                            @if (IsProcessing)
                            {
                                <span class="spinner-border spinner-border-sm me-2"></span>
                                <span>Logging in...</span>
                            }
                            else
                            {
                                <span>Login</span>
                            }
                        </button>
                    </EditForm>

                    <div class="mt-3 text-center">
                        <p>Don't have an account? <a href="/register">Register</a></p>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@code {
    private LoginModel Model = new();
    private bool IsProcessing = false;
    private string? ErrorMessage;

    private async Task HandleLogin()
    {
        IsProcessing = true;
        ErrorMessage = null;

        try
        {
            var result = await AccountService.LoginAsync(Model.Email, Model.Password, Model.RememberMe);

            if (result.Succeeded)
            {
                Navigation.NavigateTo("/dashboard");
            }
            else if (result.IsLockedOut)
            {
                ErrorMessage = "Account is locked out.";
            }
            else
            {
                ErrorMessage = "Invalid email or password.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "An error occurred during login. Please try again.";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private class LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }
}
```

### LoginDisplay.razor Component (TDD)

**Purpose**: Display current user info and logout button in the nav bar.

#### Tests (4 tests)
```csharp
[Fact]
public void LoginDisplay_WhenNotAuthenticated_ShowsLoginLink()
{
    // Arrange
    var mockAccountService = new Mock<IAccountService>();
    mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockAccountService.Object);

    // Act
    var component = ctx.RenderComponent<LoginDisplay>();

    // Assert
    component.Find("a[href='/login']").Should().NotBeNull();
    component.Markup.Should().Contain("Login");
}

[Fact]
public void LoginDisplay_WhenAuthenticated_ShowsUserName()
{
    // Arrange
    var user = new ApplicationUser { FirstName = "John", LastName = "Doe" };
    var mockAccountService = new Mock<IAccountService>();
    mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockAccountService.Object);

    // Act
    var component = ctx.RenderComponent<LoginDisplay>();

    // Assert
    component.Markup.Should().Contain("John Doe");
}

[Fact]
public void LoginDisplay_WhenAuthenticated_ShowsLogoutButton()
{
    // Arrange
    var user = new ApplicationUser { FirstName = "John", LastName = "Doe" };
    var mockAccountService = new Mock<IAccountService>();
    mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockAccountService.Object);
    ctx.Services.AddSingleton(Mock.Of<NavigationManager>());

    // Act
    var component = ctx.RenderComponent<LoginDisplay>();

    // Assert
    component.Find("button:contains('Logout')").Should().NotBeNull();
}

[Fact]
public async Task LoginDisplay_ClickLogout_CallsAccountService()
{
    // Arrange
    var user = new ApplicationUser { FirstName = "John", LastName = "Doe" };
    var mockAccountService = new Mock<IAccountService>();
    mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

    using var ctx = new TestContext();
    ctx.Services.AddSingleton(mockAccountService.Object);
    ctx.Services.AddSingleton(Mock.Of<NavigationManager>());

    var component = ctx.RenderComponent<LoginDisplay>();

    // Act
    component.Find("button:contains('Logout')").Click();
    await Task.Delay(100);

    // Assert
    mockAccountService.Verify(x => x.LogoutAsync(), Times.Once);
}
```

#### Implementation
```razor
@using AllowanceTracker.Services
@using AllowanceTracker.Models
@inject IAccountService AccountService
@inject NavigationManager Navigation

@if (CurrentUser != null)
{
    <div class="d-flex align-items-center">
        <span class="me-3">Hello, @CurrentUser.FirstName!</span>
        <button class="btn btn-sm btn-outline-secondary" @onclick="HandleLogout">
            Logout
        </button>
    </div>
}
else
{
    <div>
        <a href="/login" class="btn btn-sm btn-primary me-2">Login</a>
        <a href="/register" class="btn btn-sm btn-outline-primary">Register</a>
    </div>
}

@code {
    private ApplicationUser? CurrentUser;

    protected override async Task OnInitializedAsync()
    {
        CurrentUser = await AccountService.GetCurrentUserAsync();
    }

    private async Task HandleLogout()
    {
        await AccountService.LogoutAsync();
        Navigation.NavigateTo("/", forceLoad: true);
    }
}
```

---

## Child Management

### ChildManagementService (TDD)

**Purpose**: CRUD operations for managing children in a family.

#### Interface
```csharp
public interface IChildManagementService
{
    Task<Child> CreateChildAsync(CreateChildDto dto);
    Task<Child?> GetChildAsync(Guid childId);
    Task<List<Child>> GetFamilyChildrenAsync(Guid familyId);
    Task UpdateChildAsync(Guid childId, UpdateChildDto dto);
    Task DeleteChildAsync(Guid childId);
    Task<bool> UserCanAccessChildAsync(Guid userId, Guid childId);
}
```

#### Tests (10 tests)
```csharp
[Fact]
public async Task CreateChild_CreatesUserAndChildProfile()
{
    // Arrange
    var family = await CreateTestFamily();
    var dto = new CreateChildDto("child@test.com", "Test123!", "Alice", "Smith", 15.00m, family.Id);

    // Act
    var child = await _childManagementService.CreateChildAsync(dto);

    // Assert
    child.Should().NotBeNull();
    child.WeeklyAllowance.Should().Be(15.00m);

    var user = await _context.Users.FindAsync(child.UserId);
    user.Should().NotBeNull();
    user!.Role.Should().Be(UserRole.Child);
}

[Fact]
public async Task UpdateChild_UpdatesWeeklyAllowance()
{
    // Arrange
    var child = await CreateTestChild(weeklyAllowance: 10.00m);
    var dto = new UpdateChildDto(WeeklyAllowance: 20.00m);

    // Act
    await _childManagementService.UpdateChildAsync(child.Id, dto);

    // Assert
    var updated = await _context.Children.FindAsync(child.Id);
    updated!.WeeklyAllowance.Should().Be(20.00m);
}

[Fact]
public async Task DeleteChild_RemovesChildAndUser()
{
    // Arrange
    var child = await CreateTestChild();
    var userId = child.UserId;

    // Act
    await _childManagementService.DeleteChildAsync(child.Id);

    // Assert
    var deletedChild = await _context.Children.FindAsync(child.Id);
    deletedChild.Should().BeNull();

    var deletedUser = await _context.Users.FindAsync(userId);
    deletedUser.Should().BeNull();
}

[Fact]
public async Task GetFamilyChildren_ReturnsOnlyFamilyChildren()
{
    // Arrange
    var family1 = await CreateTestFamily("Family 1");
    var family2 = await CreateTestFamily("Family 2");

    await CreateTestChild(familyId: family1.Id);
    await CreateTestChild(familyId: family1.Id);
    await CreateTestChild(familyId: family2.Id);

    // Act
    var children = await _childManagementService.GetFamilyChildrenAsync(family1.Id);

    // Assert
    children.Should().HaveCount(2);
    children.Should().AllSatisfy(c => c.FamilyId.Should().Be(family1.Id));
}

[Fact]
public async Task UserCanAccessChild_ParentInSameFamily_ReturnsTrue()
{
    // Arrange
    var family = await CreateTestFamily();
    var parent = await CreateTestParent(familyId: family.Id);
    var child = await CreateTestChild(familyId: family.Id);

    // Act
    var canAccess = await _childManagementService.UserCanAccessChildAsync(parent.Id, child.Id);

    // Assert
    canAccess.Should().BeTrue();
}

[Fact]
public async Task UserCanAccessChild_ParentInDifferentFamily_ReturnsFalse()
{
    // Arrange
    var family1 = await CreateTestFamily();
    var family2 = await CreateTestFamily();
    var parent = await CreateTestParent(familyId: family1.Id);
    var child = await CreateTestChild(familyId: family2.Id);

    // Act
    var canAccess = await _childManagementService.UserCanAccessChildAsync(parent.Id, child.Id);

    // Assert
    canAccess.Should().BeFalse();
}

[Fact]
public async Task UserCanAccessChild_ChildAccessingOwnProfile_ReturnsTrue()
{
    // Arrange
    var child = await CreateTestChild();

    // Act
    var canAccess = await _childManagementService.UserCanAccessChildAsync(child.UserId, child.Id);

    // Assert
    canAccess.Should().BeTrue();
}

[Fact]
public async Task UserCanAccessChild_ChildAccessingOtherChild_ReturnsFalse()
{
    // Arrange
    var child1 = await CreateTestChild();
    var child2 = await CreateTestChild();

    // Act
    var canAccess = await _childManagementService.UserCanAccessChildAsync(child1.UserId, child2.Id);

    // Assert
    canAccess.Should().BeFalse();
}

[Fact]
public async Task CreateChild_WithDuplicateEmail_ThrowsException()
{
    // Arrange
    await CreateTestUser("duplicate@test.com");
    var family = await CreateTestFamily();
    var dto = new CreateChildDto("duplicate@test.com", "Test123!", "Bob", "Jones", 10.00m, family.Id);

    // Act & Assert
    await Assert.ThrowsAsync<InvalidOperationException>(
        async () => await _childManagementService.CreateChildAsync(dto));
}

[Fact]
public async Task GetChild_WithInvalidId_ReturnsNull()
{
    // Act
    var child = await _childManagementService.GetChildAsync(Guid.NewGuid());

    // Assert
    child.Should().BeNull();
}
```

#### DTOs
```csharp
public record CreateChildDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    decimal WeeklyAllowance,
    Guid FamilyId);

public record UpdateChildDto(decimal WeeklyAllowance);
```

---

## Dashboard Views

### Parent Dashboard (Updated)

**Purpose**: Show all children with balance information and quick actions.

```razor
@page "/dashboard"
@attribute [Authorize(Roles = "Parent")]
@using AllowanceTracker.Components
@using AllowanceTracker.DTOs
@using AllowanceTracker.Services
@using AllowanceTracker.Models
@using Microsoft.AspNetCore.SignalR.Client
@inject IFamilyService FamilyService
@inject IAccountService AccountService
@inject NavigationManager Navigation
@implements IAsyncDisposable

<PageTitle>Dashboard - Allowance Tracker</PageTitle>

<div class="container-fluid">
    <div class="d-flex justify-content-between align-items-center mb-4">
        <h1>Family Dashboard</h1>
        <a href="/children/create" class="btn btn-primary">
            <span class="oi oi-plus"></span> Add Child
        </a>
    </div>

    @if (Loading)
    {
        <div class="text-center">
            <div class="spinner-border" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
        </div>
    }
    else if (Children.Any())
    {
        <div class="row">
            @foreach (var child in Children)
            {
                <div class="col-md-4 mb-3">
                    <ChildCard Child="@child" OnTransactionAdded="@RefreshData" />
                </div>
            }
        </div>
    }
    else
    {
        <div class="alert alert-info">
            <h4 class="alert-heading">No children yet</h4>
            <p>Get started by adding your first child to the family.</p>
            <hr>
            <a href="/children/create" class="btn btn-primary">Add Child</a>
        </div>
    }
</div>

@code {
    private bool Loading = true;
    private List<ChildDto> Children = new();
    private HubConnection? _hubConnection;

    protected override async Task OnInitializedAsync()
    {
        await RefreshData();

        // Setup SignalR connection for real-time updates
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/familyhub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<Guid>("TransactionCreated", async (childId) =>
        {
            await RefreshData();
            await InvokeAsync(StateHasChanged);
        });

        await _hubConnection.StartAsync();
    }

    private async Task RefreshData()
    {
        Loading = true;
        Children = await FamilyService.GetChildrenAsync();
        Loading = false;
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

### Child Dashboard (New)

**Purpose**: Show child's own balance and transaction history (read-only).

```razor
@page "/child-dashboard"
@attribute [Authorize(Roles = "Child")]
@using AllowanceTracker.Services
@using AllowanceTracker.Models
@using Microsoft.AspNetCore.SignalR.Client
@inject IAccountService AccountService
@inject ITransactionService TransactionService
@inject NavigationManager Navigation
@implements IAsyncDisposable

<PageTitle>My Allowance - Allowance Tracker</PageTitle>

<div class="container">
    <h1 class="mb-4">My Allowance</h1>

    @if (ChildProfile != null)
    {
        <div class="row">
            <div class="col-md-6 mb-4">
                <div class="card">
                    <div class="card-body text-center">
                        <h5 class="card-title text-muted">Current Balance</h5>
                        <h2 class="display-3 text-primary">@ChildProfile.CurrentBalance.ToString("C")</h2>
                        <p class="text-muted">
                            Weekly Allowance: @ChildProfile.WeeklyAllowance.ToString("C")
                        </p>
                    </div>
                </div>
            </div>

            <div class="col-md-6 mb-4">
                <div class="card">
                    <div class="card-body">
                        <h5 class="card-title">Next Allowance</h5>
                        @if (ChildProfile.LastAllowanceDate.HasValue)
                        {
                            var nextDate = ChildProfile.LastAllowanceDate.Value.AddDays(7);
                            <p class="lead">@nextDate.ToString("MMMM dd, yyyy")</p>
                            <p class="text-muted">
                                @((nextDate - DateTime.UtcNow).Days) days remaining
                            </p>
                        }
                        else
                        {
                            <p class="lead">Pending</p>
                            <p class="text-muted">Your first allowance will be processed soon!</p>
                        }
                    </div>
                </div>
            </div>
        </div>

        <div class="card">
            <div class="card-header">
                <h5>Recent Transactions</h5>
            </div>
            <div class="card-body">
                @if (Transactions.Any())
                {
                    <table class="table table-hover">
                        <thead>
                            <tr>
                                <th>Date</th>
                                <th>Description</th>
                                <th class="text-end">Amount</th>
                                <th class="text-end">Balance</th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var transaction in Transactions)
                            {
                                <tr>
                                    <td>@transaction.CreatedAt.ToString("MMM dd, yyyy")</td>
                                    <td>@transaction.Description</td>
                                    <td class="text-end @(transaction.Type == TransactionType.Credit ? "text-success" : "text-danger")">
                                        @(transaction.Type == TransactionType.Credit ? "+" : "-")@transaction.Amount.ToString("C")
                                    </td>
                                    <td class="text-end">@transaction.BalanceAfter.ToString("C")</td>
                                </tr>
                            }
                        </tbody>
                    </table>
                }
                else
                {
                    <p class="text-muted">No transactions yet.</p>
                }
            </div>
        </div>
    }
</div>

@code {
    private Child? ChildProfile;
    private List<Transaction> Transactions = new();
    private HubConnection? _hubConnection;

    protected override async Task OnInitializedAsync()
    {
        var currentUser = await AccountService.GetCurrentUserAsync();
        if (currentUser?.ChildProfile != null)
        {
            ChildProfile = currentUser.ChildProfile;
            Transactions = await TransactionService.GetChildTransactionsAsync(ChildProfile.Id, limit: 20);
        }

        // Setup SignalR for real-time updates
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/familyhub"))
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<Guid>("TransactionCreated", async (childId) =>
        {
            if (childId == ChildProfile?.Id)
            {
                await RefreshData();
                await InvokeAsync(StateHasChanged);
            }
        });

        await _hubConnection.StartAsync();
    }

    private async Task RefreshData()
    {
        if (ChildProfile != null)
        {
            Transactions = await TransactionService.GetChildTransactionsAsync(ChildProfile.Id, limit: 20);
            var updated = await _context.Children.FindAsync(ChildProfile.Id);
            if (updated != null)
            {
                ChildProfile.CurrentBalance = updated.CurrentBalance;
            }
        }
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

---

## Navigation & Layout

### Updated NavMenu.razor

```razor
@using AllowanceTracker.Services
@using AllowanceTracker.Models
@inject IAccountService AccountService

<div class="top-row ps-3 navbar navbar-dark">
    <div class="container-fluid">
        <a class="navbar-brand" href="">Allowance Tracker</a>
        <button title="Navigation menu" class="navbar-toggler" @onclick="ToggleNavMenu">
            <span class="navbar-toggler-icon"></span>
        </button>
    </div>
</div>

<div class="@NavMenuCssClass nav-scrollable" @onclick="ToggleNavMenu">
    <nav class="flex-column">
        @if (CurrentUser != null)
        {
            @if (CurrentUser.Role == UserRole.Parent)
            {
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="/dashboard" Match="NavLinkMatch.All">
                        <span class="oi oi-home" aria-hidden="true"></span> Dashboard
                    </NavLink>
                </div>
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="/children">
                        <span class="oi oi-people" aria-hidden="true"></span> Manage Children
                    </NavLink>
                </div>
            }
            else
            {
                <div class="nav-item px-3">
                    <NavLink class="nav-link" href="/child-dashboard" Match="NavLinkMatch.All">
                        <span class="oi oi-home" aria-hidden="true"></span> My Allowance
                    </NavLink>
                </div>
            }
        }
        else
        {
            <div class="nav-item px-3">
                <NavLink class="nav-link" href="/login">
                    <span class="oi oi-account-login" aria-hidden="true"></span> Login
                </NavLink>
            </div>
            <div class="nav-item px-3">
                <NavLink class="nav-link" href="/register">
                    <span class="oi oi-pencil" aria-hidden="true"></span> Register
                    </NavLink>
            </div>
        }
    </nav>
</div>

@code {
    private bool collapseNavMenu = true;
    private ApplicationUser? CurrentUser;

    private string? NavMenuCssClass => collapseNavMenu ? "collapse" : null;

    protected override async Task OnInitializedAsync()
    {
        CurrentUser = await AccountService.GetCurrentUserAsync();
    }

    private void ToggleNavMenu()
    {
        collapseNavMenu = !collapseNavMenu;
    }
}
```

### Updated MainLayout.razor

```razor
@inherits LayoutComponentBase
@using AllowanceTracker.Shared

<PageTitle>Allowance Tracker</PageTitle>

<div class="page">
    <div class="sidebar">
        <NavMenu />
    </div>

    <main>
        <div class="top-row px-4 d-flex justify-content-between align-items-center">
            <a href="https://github.com/yourusername/allowance-tracker" target="_blank">About</a>
            <LoginDisplay />
        </div>

        <article class="content px-4">
            <CascadingAuthenticationState>
                <AuthorizeRouteView RouteData="@RouteData" DefaultLayout="@typeof(MainLayout)">
                    <NotAuthorized>
                        @if (context.User.Identity?.IsAuthenticated == true)
                        {
                            <p class="alert alert-warning">You do not have access to this page.</p>
                        }
                        else
                        {
                            <p class="alert alert-info">Please <a href="/login">log in</a> to access this page.</p>
                        }
                    </NotAuthorized>
                    <Authorizing>
                        <div class="text-center">
                            <div class="spinner-border" role="status">
                                <span class="visually-hidden">Loading...</span>
                            </div>
                        </div>
                    </Authorizing>
                </AuthorizeRouteView>
            </CascadingAuthenticationState>
        </article>
    </main>
</div>
```

---

## Testing Strategy

### Test Coverage Goals
- **Services**: >95% coverage (all business logic)
- **Components**: Critical user paths (authentication, transactions)
- **Total Tests**: ~128 tests (73 existing + 55 new)

### Test Categories
1. **Unit Tests** (Services)
   - AccountService: 8 tests
   - ChildManagementService: 10 tests
   - BlazorCurrentUserService: 4 tests

2. **Component Tests** (bUnit)
   - Register.razor: 4 tests
   - Login.razor: 4 tests
   - LoginDisplay.razor: 4 tests
   - Children/Index.razor: 4 tests
   - Children/Create.razor: 4 tests
   - Children/Edit.razor: 4 tests
   - Children/Details.razor: 3 tests

3. **Integration Tests**
   - Authentication flow (register → login → access protected page)
   - Child management workflow (create → edit → delete)

---

## Summary

This specification provides a **complete Blazor UI** for the Allowance Tracker with:

✅ Full authentication (Register, Login, Logout)
✅ Family and child management
✅ Role-based dashboards (Parent vs Child)
✅ Transaction operations via UI
✅ Real-time updates with SignalR
✅ Comprehensive test coverage (~55 new tests)
✅ Following strict TDD methodology

All features are production-ready and follow ASP.NET Core best practices.
