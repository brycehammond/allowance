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
    private readonly Mock<IEmailService> _emailServiceMock;
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
        _emailServiceMock = new Mock<IEmailService>();

        _accountService = new AccountService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _context,
            _httpContextAccessorMock.Object,
            _emailServiceMock.Object);
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

    [Fact]
    public async Task RegisterAdditionalParent_AddsParentToExistingFamily()
    {
        // Arrange
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Smith Family",
            CreatedAt = DateTime.UtcNow
        };
        _context.Families.Add(family);
        await _context.SaveChangesAsync();

        var dto = new RegisterAdditionalParentDto(
            "parent2@test.com",
            "Test123!",
            "Jane",
            "Smith");

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) =>
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            });

        // Act
        var result = await _accountService.RegisterAdditionalParentAsync(dto, family.Id);

        // Assert
        result.Succeeded.Should().BeTrue();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Parent);
        user.FamilyId.Should().Be(family.Id);
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
    }

    [Fact]
    public async Task RegisterAdditionalParent_WithInvalidFamilyId_ReturnsFailure()
    {
        // Arrange
        var dto = new RegisterAdditionalParentDto(
            "parent2@test.com",
            "Test123!",
            "Jane",
            "Smith");
        var invalidFamilyId = Guid.NewGuid();

        // Act
        var result = await _accountService.RegisterAdditionalParentAsync(dto, invalidFamilyId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Description.Contains("Family not found"));
    }

    [Fact]
    public async Task RegisterAdditionalParent_WithFailedUserCreation_ReturnsFailure()
    {
        // Arrange
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Smith Family",
            CreatedAt = DateTime.UtcNow
        };
        _context.Families.Add(family);
        await _context.SaveChangesAsync();

        var dto = new RegisterAdditionalParentDto(
            "parent2@test.com",
            "Test123!",
            "Jane",
            "Smith");

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "User creation failed" }));

        // Act
        var result = await _accountService.RegisterAdditionalParentAsync(dto, family.Id);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Description == "User creation failed");
    }

    [Fact]
    public async Task ChangePassword_WithValidCredentials_Succeeds()
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
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, "OldPassword123!", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.ChangePasswordAsync(user.Id, "OldPassword123!", "NewPassword123!");

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WithInvalidCurrentPassword_Fails()
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
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.ChangePasswordAsync(user, "WrongPassword!", "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Incorrect password" }));

        // Act
        var result = await _accountService.ChangePasswordAsync(user.Id, "WrongPassword!", "NewPassword123!");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Description == "Incorrect password");
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_Succeeds()
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
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetToken = "valid-reset-token";

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.ResetPasswordAsync(user, resetToken, "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.ResetPasswordAsync(user.Email, resetToken, "NewPassword123!");

        // Assert
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_Fails()
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
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var invalidToken = "invalid-token";

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.ResetPasswordAsync(user, invalidToken, "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token" }));

        // Act
        var result = await _accountService.ResetPasswordAsync(user.Email, invalidToken, "NewPassword123!");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Description == "Invalid token");
    }

    [Fact]
    public async Task ResetPassword_WithNonexistentEmail_Fails()
    {
        // Arrange
        _userManagerMock
            .Setup(x => x.FindByEmailAsync("nonexistent@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _accountService.ResetPasswordAsync("nonexistent@test.com", "token", "NewPassword123!");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Description.Contains("User not found"));
    }

    [Fact]
    public async Task ForgotPassword_SendsEmailWithResetToken()
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
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var resetToken = "generated-reset-token";

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GeneratePasswordResetTokenAsync(user))
            .ReturnsAsync(resetToken);

        // Act
        var result = await _accountService.ForgotPasswordAsync(user.Email);

        // Assert
        result.Should().BeTrue();
        _emailServiceMock.Verify(
            x => x.SendPasswordResetEmailAsync(user.Email, resetToken, "John Doe"),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WithNonexistentEmail_ReturnsFalseWithoutSendingEmail()
    {
        // Arrange
        _userManagerMock
            .Setup(x => x.FindByEmailAsync("nonexistent@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _accountService.ForgotPasswordAsync("nonexistent@test.com");

        // Assert
        result.Should().BeFalse();
        _emailServiceMock.Verify(
            x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAccount_WithValidUserId_DeletesUserAndReturnsSuccess()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "test@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.DeleteAccountAsync(user.Id);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteAccount_WithNonexistentUserId_ReturnsFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(nonExistentId.ToString()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _accountService.DeleteAccountAsync(nonExistentId);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Description.Contains("User not found"));
    }

    [Fact]
    public async Task DeleteAccountByEmail_WithValidEmail_DeletesUserAndReturnsSuccess()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            UserName = "test@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(user.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.DeleteAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.DeleteAccountByEmailAsync(user.Email);

        // Assert
        result.Succeeded.Should().BeTrue();
        _userManagerMock.Verify(x => x.DeleteAsync(user), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountByEmail_WithNonexistentEmail_ReturnsFailure()
    {
        // Arrange
        _userManagerMock
            .Setup(x => x.FindByEmailAsync("nonexistent@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _accountService.DeleteAccountByEmailAsync("nonexistent@test.com");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Description.Contains("User not found"));
    }

    [Fact]
    public async Task DeleteAccount_ChildWithProfile_DeletesChildProfileAndRelatedData()
    {
        // Arrange
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test Family",
            CreatedAt = DateTime.UtcNow
        };
        _context.Families.Add(family);

        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            UserName = "child@test.com",
            FirstName = "Alice",
            LastName = "Smith",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        _context.Users.Add(childUser);

        var childProfile = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 50m,
            CreatedAt = DateTime.UtcNow
        };
        _context.Children.Add(childProfile);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            ChildId = childProfile.Id,
            Amount = 10m,
            Type = TransactionType.Credit,
            Description = "Test",
            BalanceAfter = 50m,
            CreatedAt = DateTime.UtcNow
        };
        _context.Transactions.Add(transaction);

        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(childUser.Id.ToString()))
            .ReturnsAsync(childUser);

        _userManagerMock
            .Setup(x => x.DeleteAsync(childUser))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.DeleteAccountAsync(childUser.Id);

        // Assert
        result.Succeeded.Should().BeTrue();

        // Verify child-related data was deleted
        var remainingTransactions = await _context.Transactions.Where(t => t.ChildId == childProfile.Id).ToListAsync();
        remainingTransactions.Should().BeEmpty();

        var remainingChildProfile = await _context.Children.FirstOrDefaultAsync(c => c.Id == childProfile.Id);
        remainingChildProfile.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAccount_FamilyOwner_DeletesEntireFamilyAndAllMembers()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var family = new Family
        {
            Id = Guid.NewGuid(),
            Name = "Test Family",
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
        _context.Families.Add(family);

        var ownerUser = new ApplicationUser
        {
            Id = ownerId,
            Email = "owner@test.com",
            UserName = "owner@test.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent,
            FamilyId = family.Id
        };
        _context.Users.Add(ownerUser);

        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            UserName = "child@test.com",
            FirstName = "Alice",
            LastName = "Doe",
            Role = UserRole.Child,
            FamilyId = family.Id
        };
        _context.Users.Add(childUser);

        var childProfile = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            FamilyId = family.Id,
            WeeklyAllowance = 10m,
            CurrentBalance = 0m,
            CreatedAt = DateTime.UtcNow
        };
        _context.Children.Add(childProfile);

        await _context.SaveChangesAsync();

        _userManagerMock
            .Setup(x => x.FindByIdAsync(ownerUser.Id.ToString()))
            .ReturnsAsync(ownerUser);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(childUser.Id.ToString()))
            .ReturnsAsync(childUser);

        _userManagerMock
            .Setup(x => x.DeleteAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _accountService.DeleteAccountAsync(ownerUser.Id);

        // Assert
        result.Succeeded.Should().BeTrue();

        // Verify family was deleted
        var remainingFamily = await _context.Families.FirstOrDefaultAsync(f => f.Id == family.Id);
        remainingFamily.Should().BeNull();

        // Verify child profile was deleted
        var remainingChildProfile = await _context.Children.FirstOrDefaultAsync(c => c.Id == childProfile.Id);
        remainingChildProfile.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
