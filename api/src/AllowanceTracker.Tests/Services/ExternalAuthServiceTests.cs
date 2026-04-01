using AllowanceTracker.Data;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AllowanceTracker.Tests.Services;

public class ExternalAuthServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IExternalTokenValidator> _tokenValidatorMock;
    private readonly ExternalAuthService _service;

    public ExternalAuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenValidatorMock = new Mock<IExternalTokenValidator>();

        _service = new ExternalAuthService(
            _userManagerMock.Object,
            _context,
            _tokenValidatorMock.Object);
    }

    [Fact]
    public async Task ExternalLogin_WithExistingExternalLogin_ReturnsExistingUser()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "existing@test.com",
            UserName = "existing@test.com",
            FirstName = "Jane",
            LastName = "Doe",
            Role = UserRole.Parent,
            FamilyId = Guid.NewGuid()
        };

        _tokenValidatorMock
            .Setup(x => x.ValidateGoogleTokenAsync("valid-google-token"))
            .ReturnsAsync(new ExternalTokenInfo("google-sub-123", "existing@test.com", "Jane", "Doe"));

        _userManagerMock
            .Setup(x => x.FindByLoginAsync("Google", "google-sub-123"))
            .ReturnsAsync(existingUser);

        var dto = new ExternalLoginDto("Google", "valid-google-token");

        // Act
        var result = await _service.ExternalLoginAsync(dto);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.User.Should().Be(existingUser);
        result.IsNewUser.Should().BeFalse();
    }

    [Fact]
    public async Task ExternalLogin_WithMatchingEmail_LinksAccountAndReturnsUser()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "linked@test.com",
            UserName = "linked@test.com",
            FirstName = "John",
            LastName = "Smith",
            Role = UserRole.Parent,
            FamilyId = Guid.NewGuid()
        };

        _tokenValidatorMock
            .Setup(x => x.ValidateGoogleTokenAsync("valid-google-token"))
            .ReturnsAsync(new ExternalTokenInfo("google-sub-456", "linked@test.com", "John", "Smith"));

        _userManagerMock
            .Setup(x => x.FindByLoginAsync("Google", "google-sub-456"))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync("linked@test.com"))
            .ReturnsAsync(existingUser);

        _userManagerMock
            .Setup(x => x.AddLoginAsync(existingUser, It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new ExternalLoginDto("Google", "valid-google-token");

        // Act
        var result = await _service.ExternalLoginAsync(dto);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.User.Should().Be(existingUser);
        result.IsNewUser.Should().BeFalse();

        _userManagerMock.Verify(x => x.AddLoginAsync(
            existingUser,
            It.Is<UserLoginInfo>(info => info.LoginProvider == "Google" && info.ProviderKey == "google-sub-456")),
            Times.Once);
    }

    [Fact]
    public async Task ExternalLogin_NewUserWithFamilyName_CreatesUserAndFamily()
    {
        // Arrange
        _tokenValidatorMock
            .Setup(x => x.ValidateGoogleTokenAsync("valid-google-token"))
            .ReturnsAsync(new ExternalTokenInfo("google-sub-789", "new@test.com", "Alice", "Wonder"));

        _userManagerMock
            .Setup(x => x.FindByLoginAsync("Google", "google-sub-789"))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync("new@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(user =>
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            });

        _userManagerMock
            .Setup(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new ExternalLoginDto("Google", "valid-google-token", FamilyName: "Wonder Family");

        // Act
        var result = await _service.ExternalLoginAsync(dto);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.IsNewUser.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("new@test.com");
        result.User.FirstName.Should().Be("Alice");
        result.User.LastName.Should().Be("Wonder");
        result.User.Role.Should().Be(UserRole.Parent);

        var family = await _context.Families.FirstOrDefaultAsync(f => f.Name == "Wonder Family");
        family.Should().NotBeNull();

        _userManagerMock.Verify(x => x.CreateAsync(It.Is<ApplicationUser>(u => u.Email == "new@test.com")), Times.Once);
        _userManagerMock.Verify(x => x.AddLoginAsync(
            It.IsAny<ApplicationUser>(),
            It.Is<UserLoginInfo>(info => info.LoginProvider == "Google" && info.ProviderKey == "google-sub-789")),
            Times.Once);
    }

    [Fact]
    public async Task ExternalLogin_NewUserWithoutFamilyName_ReturnsFamilyNameRequired()
    {
        // Arrange
        _tokenValidatorMock
            .Setup(x => x.ValidateGoogleTokenAsync("valid-google-token"))
            .ReturnsAsync(new ExternalTokenInfo("google-sub-new", "brand-new@test.com", "Bob", "Builder"));

        _userManagerMock
            .Setup(x => x.FindByLoginAsync("Google", "google-sub-new"))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync("brand-new@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        var dto = new ExternalLoginDto("Google", "valid-google-token");

        // Act
        var result = await _service.ExternalLoginAsync(dto);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("FAMILY_NAME_REQUIRED");
    }

    [Fact]
    public async Task ExternalLogin_WithInvalidToken_ReturnsFailure()
    {
        // Arrange
        _tokenValidatorMock
            .Setup(x => x.ValidateGoogleTokenAsync("invalid-token"))
            .ReturnsAsync((ExternalTokenInfo?)null);

        var dto = new ExternalLoginDto("Google", "invalid-token");

        // Act
        var result = await _service.ExternalLoginAsync(dto);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("INVALID_TOKEN");
    }

    [Fact]
    public async Task ExternalLogin_WithUnsupportedProvider_ReturnsFailure()
    {
        // Arrange
        var dto = new ExternalLoginDto("Facebook", "some-token");

        // Act
        var result = await _service.ExternalLoginAsync(dto);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("UNSUPPORTED_PROVIDER");
    }

    [Fact]
    public async Task ExternalLogin_WithAppleProvider_ValidatesAppleToken()
    {
        // Arrange
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "apple@test.com",
            UserName = "apple@test.com",
            FirstName = "Apple",
            LastName = "User",
            Role = UserRole.Parent,
            FamilyId = Guid.NewGuid()
        };

        _tokenValidatorMock
            .Setup(x => x.ValidateAppleTokenAsync("valid-apple-token"))
            .ReturnsAsync(new ExternalTokenInfo("apple-sub-123", "apple@test.com", null, null));

        _userManagerMock
            .Setup(x => x.FindByLoginAsync("Apple", "apple-sub-123"))
            .ReturnsAsync(existingUser);

        var dto = new ExternalLoginDto("Apple", "valid-apple-token");

        // Act
        var result = await _service.ExternalLoginAsync(dto);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.User.Should().Be(existingUser);
        _tokenValidatorMock.Verify(x => x.ValidateAppleTokenAsync("valid-apple-token"), Times.Once);
        _tokenValidatorMock.Verify(x => x.ValidateGoogleTokenAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ExternalLogin_AppleNewUser_UsesNameFromDto()
    {
        // Arrange - Apple often returns null names after first auth, so client sends them in the DTO
        _tokenValidatorMock
            .Setup(x => x.ValidateAppleTokenAsync("valid-apple-token"))
            .ReturnsAsync(new ExternalTokenInfo("apple-sub-new", "apple-new@test.com", null, null));

        _userManagerMock
            .Setup(x => x.FindByLoginAsync("Apple", "apple-sub-new"))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync("apple-new@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser>(user =>
            {
                _context.Users.Add(user);
                _context.SaveChanges();
            });

        _userManagerMock
            .Setup(x => x.AddLoginAsync(It.IsAny<ApplicationUser>(), It.IsAny<UserLoginInfo>()))
            .ReturnsAsync(IdentityResult.Success);

        var dto = new ExternalLoginDto("Apple", "valid-apple-token",
            FamilyName: "Apple Family", FirstName: "Tim", LastName: "Apple");

        // Act
        var result = await _service.ExternalLoginAsync(dto);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.IsNewUser.Should().BeTrue();
        result.User!.FirstName.Should().Be("Tim");
        result.User.LastName.Should().Be("Apple");
    }

    [Fact]
    public async Task ExternalLogin_NewUserCreationFails_ReturnsFailure()
    {
        // Arrange
        _tokenValidatorMock
            .Setup(x => x.ValidateGoogleTokenAsync("valid-google-token"))
            .ReturnsAsync(new ExternalTokenInfo("google-sub-fail", "fail@test.com", "Fail", "User"));

        _userManagerMock
            .Setup(x => x.FindByLoginAsync("Google", "google-sub-fail"))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.FindByEmailAsync("fail@test.com"))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Creation failed" }));

        var dto = new ExternalLoginDto("Google", "valid-google-token", FamilyName: "Fail Family");

        // Act
        var result = await _service.ExternalLoginAsync(dto);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorCode.Should().Be("USER_CREATION_FAILED");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
