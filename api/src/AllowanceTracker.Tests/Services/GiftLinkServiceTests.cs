using AllowanceTracker.Data;
using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class GiftLinkServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly GiftLinkService _service;
    private readonly Family _testFamily;
    private readonly ApplicationUser _testParent;
    private readonly Child _testChild;

    public GiftLinkServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);

        // Create configuration with base URL
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "AppSettings:BaseUrl", "https://allowance.example.com" }
        });
        var configuration = configBuilder.Build();

        _service = new GiftLinkService(_context, configuration);

        // Create test data
        _testFamily = new Family { Id = Guid.NewGuid(), Name = "Test Family" };
        _testParent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "parent@test.com",
            UserName = "parent@test.com",
            FirstName = "Test",
            LastName = "Parent",
            Role = UserRole.Parent,
            FamilyId = _testFamily.Id
        };
        _testFamily.OwnerId = _testParent.Id;

        var childUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            UserName = "child@test.com",
            FirstName = "Test",
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = _testFamily.Id
        };
        _testChild = new Child
        {
            Id = Guid.NewGuid(),
            UserId = childUser.Id,
            FamilyId = _testFamily.Id,
            CurrentBalance = 50m,
            WeeklyAllowance = 10m
        };

        _context.Families.Add(_testFamily);
        _context.Users.Add(_testParent);
        _context.Users.Add(childUser);
        _context.Children.Add(_testChild);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateLink_GeneratesSecureUrlSafeToken()
    {
        // Arrange
        var dto = new CreateGiftLinkDto(
            _testChild.Id,
            "Birthday 2024",
            "Share this link with family"
        );

        // Act
        var result = await _service.CreateLinkAsync(dto, _testParent.Id);

        // Assert
        result.Should().NotBeNull();
        result.Token.Should().NotBeNullOrEmpty();
        result.Token.Should().HaveLength(43); // Base64 URL-safe encoding of 32 bytes
        result.Token.Should().NotContain("+");
        result.Token.Should().NotContain("/");
        result.Token.Should().NotContain("=");
    }

    [Fact]
    public async Task CreateLink_SetsCorrectDefaultValues()
    {
        // Arrange
        var dto = new CreateGiftLinkDto(
            _testChild.Id,
            "Default Link"
        );

        // Act
        var result = await _service.CreateLinkAsync(dto, _testParent.Id);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Default Link");
        result.Visibility.Should().Be(GiftLinkVisibility.Minimal);
        result.IsActive.Should().BeTrue();
        result.ExpiresAt.Should().BeNull();
        result.MaxUses.Should().BeNull();
        result.UseCount.Should().Be(0);
        result.MinAmount.Should().BeNull();
        result.MaxAmount.Should().BeNull();
        result.DefaultOccasion.Should().BeNull();
    }

    [Fact]
    public async Task CreateLink_RequiresParentToOwnChild()
    {
        // Arrange
        var otherFamily = new Family { Id = Guid.NewGuid(), Name = "Other Family" };
        var otherParent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "other@test.com",
            UserName = "other@test.com",
            FirstName = "Other",
            LastName = "Parent",
            Role = UserRole.Parent,
            FamilyId = otherFamily.Id
        };
        otherFamily.OwnerId = otherParent.Id;

        _context.Families.Add(otherFamily);
        _context.Users.Add(otherParent);
        await _context.SaveChangesAsync();

        var dto = new CreateGiftLinkDto(_testChild.Id, "Unauthorized Link");

        // Act
        Func<Task> act = async () => await _service.CreateLinkAsync(dto, otherParent.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not authorized*");
    }

    [Fact]
    public async Task GetFamilyLinks_ReturnsOnlyLinksForFamily()
    {
        // Arrange
        var link1 = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "token1",
            Name = "Link 1",
            IsActive = true
        };
        var link2 = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "token2",
            Name = "Link 2",
            IsActive = true
        };

        // Create another family's link
        var otherFamily = new Family { Id = Guid.NewGuid(), Name = "Other Family" };
        var otherParent = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "other2@test.com",
            UserName = "other2@test.com",
            FirstName = "Other",
            LastName = "Parent",
            Role = UserRole.Parent,
            FamilyId = otherFamily.Id
        };
        otherFamily.OwnerId = otherParent.Id;
        var otherChildUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "otherchild@test.com",
            UserName = "otherchild@test.com",
            FirstName = "Other",
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = otherFamily.Id
        };
        var otherChild = new Child
        {
            Id = Guid.NewGuid(),
            UserId = otherChildUser.Id,
            FamilyId = otherFamily.Id,
            CurrentBalance = 0m,
            WeeklyAllowance = 5m
        };
        var otherLink = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = otherChild.Id,
            FamilyId = otherFamily.Id,
            CreatedById = otherParent.Id,
            Token = "other_token",
            Name = "Other Link",
            IsActive = true
        };

        _context.Families.Add(otherFamily);
        _context.Users.Add(otherParent);
        _context.Users.Add(otherChildUser);
        _context.Children.Add(otherChild);
        _context.GiftLinks.AddRange(link1, link2, otherLink);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFamilyLinksAsync(_testFamily.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(l => l.Name == "Link 1");
        result.Should().Contain(l => l.Name == "Link 2");
        result.Should().NotContain(l => l.Name == "Other Link");
    }

    [Fact]
    public async Task GetFamilyLinks_IncludesUsageStatistics()
    {
        // Arrange
        var link = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "stats_token",
            Name = "Stats Link",
            IsActive = true,
            UseCount = 5
        };
        _context.GiftLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetFamilyLinksAsync(_testFamily.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].UseCount.Should().Be(5);
    }

    [Fact]
    public async Task UpdateLink_UpdatesAllProvidedFields()
    {
        // Arrange
        var link = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "update_token",
            Name = "Original Name",
            Description = "Original Description",
            Visibility = GiftLinkVisibility.Minimal,
            IsActive = true
        };
        _context.GiftLinks.Add(link);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateGiftLinkDto(
            Name: "Updated Name",
            Description: "Updated Description",
            Visibility: GiftLinkVisibility.Full,
            IsActive: true,
            MinAmount: 10m,
            MaxAmount: 100m,
            DefaultOccasion: GiftOccasion.Birthday
        );

        // Act
        var result = await _service.UpdateLinkAsync(link.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Name");
        result.Description.Should().Be("Updated Description");
        result.Visibility.Should().Be(GiftLinkVisibility.Full);
        result.MinAmount.Should().Be(10m);
        result.MaxAmount.Should().Be(100m);
        result.DefaultOccasion.Should().Be(GiftOccasion.Birthday);
    }

    [Fact]
    public async Task UpdateLink_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var updateDto = new UpdateGiftLinkDto(Name: "Updated");

        // Act
        var result = await _service.UpdateLinkAsync(nonExistentId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeactivateLink_SetsIsActiveFalse()
    {
        // Arrange
        var link = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "deactivate_token",
            Name = "Deactivate Link",
            IsActive = true
        };
        _context.GiftLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateLinkAsync(link.Id);

        // Assert
        result.Should().BeTrue();
        var updatedLink = await _context.GiftLinks.FindAsync(link.Id);
        updatedLink!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RegenerateToken_CreatesNewTokenAndInvalidatesOld()
    {
        // Arrange
        var originalToken = "original_token_123";
        var link = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = originalToken,
            Name = "Regenerate Link",
            IsActive = true
        };
        _context.GiftLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.RegenerateTokenAsync(link.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Token.Should().NotBe(originalToken);
        result.Token.Should().HaveLength(43);

        // Verify old token is invalid
        var oldTokenValidation = await _service.ValidateTokenAsync(originalToken);
        oldTokenValidation.Should().BeNull();

        // Verify new token is valid
        var newTokenValidation = await _service.ValidateTokenAsync(result.Token);
        newTokenValidation.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateToken_ReturnsNull_WhenExpired()
    {
        // Arrange
        var link = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "expired_token",
            Name = "Expired Link",
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        };
        _context.GiftLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateTokenAsync("expired_token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_ReturnsNull_WhenMaxUsesExceeded()
    {
        // Arrange
        var link = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "max_uses_token",
            Name = "Max Uses Link",
            IsActive = true,
            MaxUses = 5,
            UseCount = 5 // Already at max
        };
        _context.GiftLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateTokenAsync("max_uses_token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateToken_ReturnsNull_WhenInactive()
    {
        // Arrange
        var link = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "inactive_token",
            Name = "Inactive Link",
            IsActive = false // Deactivated
        };
        _context.GiftLinks.Add(link);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateTokenAsync("inactive_token");

        // Assert
        result.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
