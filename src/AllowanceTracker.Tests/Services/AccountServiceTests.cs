using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AllowanceTracker.Tests.Services;

public class AccountServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AccountService _accountService;

    public AccountServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);

        // Mock UserManager
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Mock SignInManager
        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);

        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _accountService = new AccountService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _context,
            _httpContextAccessorMock.Object);
    }

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

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) =>
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            });

        // Act
        var result = await _accountService.RegisterParentAsync(dto);

        // Assert
        result.Succeeded.Should().BeTrue();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Parent);
        user.FamilyId.Should().NotBeNull();

        var family = await _context.Families.FindAsync(user.FamilyId);
        family.Should().NotBeNull();
        family!.Name.Should().Be("Doe Family");
    }

    [Fact]
    public async Task RegisterParent_WithFailedUserCreation_ReturnsFailure()
    {
        // Arrange
        var dto = new RegisterParentDto(
            "parent@test.com",
            "Test123!",
            "John",
            "Doe",
            "Doe Family");

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "User creation failed" }));

        // Act
        var result = await _accountService.RegisterParentAsync(dto);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Description == "User creation failed");

        // Note: Transaction rollback behavior cannot be tested with InMemory database
        // In production with PostgreSQL, the transaction will properly roll back the family creation
    }

    [Fact]
    public async Task RegisterChild_AssociatesWithFamily()
    {
        // Arrange
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test Family",
            CreatedAt = DateTime.UtcNow
        };
        _context.Families.Add(family);
        await _context.SaveChangesAsync();

        var dto = new RegisterChildDto("child@test.com", "Test123!", "Alice", "Smith", 10.00m);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) =>
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            });

        // Act
        var result = await _accountService.RegisterChildAsync(dto, family.Id);

        // Assert
        result.Succeeded.Should().BeTrue();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Child);
        user.FamilyId.Should().Be(family.Id);

        var childProfile = await _context.Children.FirstOrDefaultAsync(c => c.UserId == user.Id);
        childProfile.Should().NotBeNull();
        childProfile!.WeeklyAllowance.Should().Be(10.00m);
    }

    [Fact]
    public async Task RegisterChild_WithInvalidFamilyId_ReturnsFailure()
    {
        // Arrange
        var dto = new RegisterChildDto("child@test.com", "Test123!", "Alice", "Smith", 10.00m);
        var invalidFamilyId = Guid.NewGuid();

        // Act
        var result = await _accountService.RegisterChildAsync(dto, invalidFamilyId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Description.Contains("Family not found"));
    }

    [Fact]
    public async Task Login_WithValidCredentials_Succeeds()
    {
        // Arrange
        var email = "test@test.com";
        var password = "Test123!";

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync(email, password, false, false))
            .ReturnsAsync(SignInResult.Success);

        // Act
        var result = await _accountService.LoginAsync(email, password);

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Fails()
    {
        // Arrange
        var email = "test@test.com";
        var password = "Wrong123!";

        _signInManagerMock
            .Setup(x => x.PasswordSignInAsync(email, password, false, false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _accountService.LoginAsync(email, password);

        // Assert
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_CallsSignInManager()
    {
        // Act
        await _accountService.LogoutAsync();

        // Assert
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ReturnsUser()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "test@test.com",
            FirstName = "John",
            LastName = "Doe"
        };

        var mockHttpContext = new Mock<HttpContext>();
        var mockClaimsPrincipal = new Mock<System.Security.Claims.ClaimsPrincipal>();

        mockHttpContext.Setup(x => x.User).Returns(mockClaimsPrincipal.Object);
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);
        _userManagerMock.Setup(x => x.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Act
        var currentUser = await _accountService.GetCurrentUserAsync();

        // Assert
        currentUser.Should().NotBeNull();
        currentUser!.Id.Should().Be(user.Id);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
