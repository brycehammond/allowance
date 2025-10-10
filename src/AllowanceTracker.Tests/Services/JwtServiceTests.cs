using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class JwtServiceTests
{
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;

    public JwtServiceTests()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Jwt:SecretKey", "this-is-a-test-secret-key-with-at-least-32-characters-for-hmac-sha256" },
            { "Jwt:Issuer", "AllowanceTrackerTests" },
            { "Jwt:Audience", "AllowanceTrackerTests" }
        });
        _configuration = configBuilder.Build();
        _jwtService = new JwtService(_configuration);
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsJwtToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_IncludesAllUserClaims()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var familyId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "parent@example.com",
            FirstName = "Jane",
            LastName = "Smith",
            Role = UserRole.Parent,
            FamilyId = familyId
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "parent@example.com");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.GivenName && c.Value == "Jane");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Surname && c.Value == "Smith");
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Parent");
        jwtToken.Claims.Should().Contain(c => c.Type == "FamilyId" && c.Value == familyId.ToString());
    }

    [Fact]
    public void GenerateToken_SetsExpiration()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Parent
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        jwtToken.ValidTo.Should().BeAfter(DateTime.UtcNow);
        jwtToken.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddDays(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ValidateToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent
        };
        var token = _jwtService.GenerateToken(user);

        // Act
        var isValid = _jwtService.ValidateToken(token);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var isValid = _jwtService.ValidateToken(invalidToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ReturnsFalse()
    {
        // Arrange
        var emptyToken = string.Empty;

        // Act
        var isValid = _jwtService.ValidateToken(emptyToken);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void GetPrincipalFromToken_WithValidToken_ReturnsPrincipal()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            Role = UserRole.Parent
        };
        var token = _jwtService.GenerateToken(user);

        // Act
        var principal = _jwtService.GetPrincipalFromToken(token);

        // Assert
        principal.Should().NotBeNull();
        principal!.Claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
        principal.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == userId.ToString());
    }

    [Fact]
    public void GetPrincipalFromToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.string";

        // Act
        var principal = _jwtService.GetPrincipalFromToken(invalidToken);

        // Assert
        principal.Should().BeNull();
    }

    [Fact]
    public void GenerateToken_WithChildRole_IncludesChildRole()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@example.com",
            FirstName = "Tommy",
            LastName = "Child",
            Role = UserRole.Child
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Child");
    }

    [Fact]
    public void GenerateToken_WithNoFamilyId_IncludesEmptyFamilyId()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = UserRole.Parent,
            FamilyId = null
        };

        // Act
        var token = _jwtService.GenerateToken(user);

        // Assert
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == "FamilyId" && c.Value == string.Empty);
    }
}
