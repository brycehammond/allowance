using AllowanceTracker.Api.V1;
using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class AuthControllerTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly Mock<IAccountService> _mockAccountService;
    private readonly Mock<IJwtService> _mockJwtService;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);
        _mockAccountService = new Mock<IAccountService>();
        _mockJwtService = new Mock<IJwtService>();
        _controller = new AuthController(_mockAccountService.Object, _mockJwtService.Object, _context);
    }

    [Fact]
    public async Task RegisterParent_WithValidData_ReturnsCreatedWithAuthResponse()
    {
        // Arrange
        var dto = new RegisterParentDto(
            "parent@test.com",
            "Test123!",
            "John",
            "Doe",
            "Doe Family");

        var family = new Family { Id = Guid.NewGuid(), Name = "Doe Family" };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent,
            FamilyId = family.Id,
            Family = family
        };

        _mockAccountService.Setup(x => x.RegisterParentAsync(dto)).ReturnsAsync(IdentityResult.Success);
        _mockAccountService.Setup(x => x.LoginAsync(dto.Email, dto.Password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>())).Returns("test-jwt-token");

        _context.Users.Add(user);
        _context.Families.Add(family);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RegisterParent(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AuthController.GetCurrentUser));

        var authResponse = createdResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
        authResponse.Email.Should().Be("parent@test.com");
        authResponse.Token.Should().Be("test-jwt-token");
    }

    [Fact]
    public async Task RegisterParent_WhenRegistrationFails_ReturnsBadRequest()
    {
        // Arrange
        var dto = new RegisterParentDto(
            "parent@test.com",
            "Test123!",
            "John",
            "Doe",
            "Doe Family");

        var errors = new IdentityError[] { new IdentityError { Description = "Email already exists" } };
        _mockAccountService.Setup(x => x.RegisterParentAsync(dto))
            .ReturnsAsync(IdentityResult.Failed(errors));

        // Act
        var result = await _controller.RegisterParent(dto);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterChild_WithValidData_ReturnsCreatedWithUserInfo()
    {
        // Arrange
        var familyId = Guid.NewGuid();
        var family = new Family { Id = familyId, Name = "Test Family" };
        var parentUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FamilyId = familyId,
            Role = UserRole.Parent
        };

        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            FirstName = "Alice",
            LastName = "Smith",
            Role = UserRole.Child,
            FamilyId = familyId,
            Family = family
        };

        var childProfile = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            User = childUser,
            WeeklyAllowance = 10m,
            CurrentBalance = 0m
        };

        childUser.ChildProfile = childProfile;

        var dto = new RegisterChildDto("child@test.com", "Test123!", "Alice", "Smith", 10m);

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(parentUser);
        _mockAccountService.Setup(x => x.RegisterChildAsync(dto, familyId))
            .ReturnsAsync(IdentityResult.Success);

        _context.Families.Add(family);
        _context.Users.Add(childUser);
        _context.Children.Add(childProfile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RegisterChild(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(AuthController.GetCurrentUser));
        createdResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterChild_WhenParentHasNoFamily_ReturnsBadRequest()
    {
        // Arrange
        var parentUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FamilyId = null,
            Role = UserRole.Parent
        };

        var dto = new RegisterChildDto("child@test.com", "Test123!", "Alice", "Smith", 10m);

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(parentUser);

        // Act
        var result = await _controller.RegisterChild(dto);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithAuthResponse()
    {
        // Arrange
        var dto = new LoginDto("parent@test.com", "Test123!", false);
        var family = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent,
            FamilyId = family.Id,
            Family = family
        };

        _mockAccountService.Setup(x => x.LoginAsync(dto.Email, dto.Password, dto.RememberMe))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>())).Returns("test-jwt-token");

        _context.Users.Add(user);
        _context.Families.Add(family);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponseDto>().Subject;
        authResponse.Email.Should().Be("parent@test.com");
        authResponse.Token.Should().Be("test-jwt-token");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var dto = new LoginDto("parent@test.com", "WrongPassword!", false);

        _mockAccountService.Setup(x => x.LoginAsync(dto.Email, dto.Password, dto.RememberMe))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var unauthorizedResult = result.Result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        unauthorizedResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsOkWithUserInfo()
    {
        // Arrange
        var familyId = Guid.NewGuid();
        var family = new Family { Id = familyId, Name = "Test Family" };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent,
            FamilyId = familyId
        };

        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);
        _context.Families.Add(family);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var userInfo = okResult.Value.Should().BeOfType<UserInfoDto>().Subject;
        userInfo.Email.Should().Be("test@test.com");
        userInfo.FamilyId.Should().Be(familyId);
    }

    [Fact]
    public async Task GetCurrentUser_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _mockAccountService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.GetCurrentUser();

        // Assert
        result.Result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task RegisterParent_GeneratesJwtTokenAfterRegistration()
    {
        // Arrange
        var dto = new RegisterParentDto("parent@test.com", "Test123!", "John", "Doe", "Doe Family");
        var family = new Family { Id = Guid.NewGuid(), Name = "Doe Family" };
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent,
            FamilyId = family.Id,
            Family = family
        };

        _mockAccountService.Setup(x => x.RegisterParentAsync(dto)).ReturnsAsync(IdentityResult.Success);
        _mockAccountService.Setup(x => x.LoginAsync(dto.Email, dto.Password, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _mockJwtService.Setup(x => x.GenerateToken(It.IsAny<ApplicationUser>())).Returns("test-jwt-token");

        _context.Users.Add(user);
        _context.Families.Add(family);
        await _context.SaveChangesAsync();

        // Act
        await _controller.RegisterParent(dto);

        // Assert
        _mockJwtService.Verify(x => x.GenerateToken(It.IsAny<ApplicationUser>()), Times.Once);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
