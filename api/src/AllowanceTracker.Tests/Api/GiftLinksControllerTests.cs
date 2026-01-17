using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class GiftLinksControllerTests
{
    private readonly Mock<IGiftLinkService> _mockGiftLinkService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GiftLinksController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testFamilyId = Guid.NewGuid();

    public GiftLinksControllerTests()
    {
        _mockGiftLinkService = new Mock<IGiftLinkService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockCurrentUserService.Setup(u => u.UserId).Returns(_testUserId);
        _mockCurrentUserService.Setup(u => u.FamilyId).Returns(_testFamilyId);
        _mockCurrentUserService.Setup(u => u.IsParent).Returns(true);

        _controller = new GiftLinksController(_mockGiftLinkService.Object, _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task GetFamilyLinks_ReturnsOkWithLinks()
    {
        // Arrange
        var links = new List<GiftLinkDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Timmy", "token1", "Birthday Link", null,
                GiftLinkVisibility.Minimal, true, null, null, 0, null, null, null,
                DateTime.UtcNow, DateTime.UtcNow, "https://test.com/gift/token1"),
            new(Guid.NewGuid(), Guid.NewGuid(), "Sally", "token2", "Holiday Link", null,
                GiftLinkVisibility.Full, true, null, null, 5, null, null, null,
                DateTime.UtcNow, DateTime.UtcNow, "https://test.com/gift/token2")
        };

        _mockGiftLinkService
            .Setup(s => s.GetFamilyLinksAsync(_testFamilyId))
            .ReturnsAsync(links);

        // Act
        var result = await _controller.GetFamilyLinks();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLinks = okResult.Value.Should().BeAssignableTo<List<GiftLinkDto>>().Subject;
        returnedLinks.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateLink_ReturnsCreatedWithLink()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var linkDto = new GiftLinkDto(
            Guid.NewGuid(), childId, "Timmy", "generated_token", "Birthday 2024", "Share with family",
            GiftLinkVisibility.WithGoals, true, null, null, 0, 5m, 100m, GiftOccasion.Birthday,
            DateTime.UtcNow, DateTime.UtcNow, "https://test.com/gift/generated_token"
        );

        _mockGiftLinkService
            .Setup(s => s.CreateLinkAsync(It.IsAny<CreateGiftLinkDto>(), _testUserId))
            .ReturnsAsync(linkDto);

        var request = new CreateGiftLinkRequest
        {
            ChildId = childId,
            Name = "Birthday 2024",
            Description = "Share with family",
            Visibility = GiftLinkVisibility.WithGoals,
            MinAmount = 5m,
            MaxAmount = 100m,
            DefaultOccasion = GiftOccasion.Birthday
        };

        // Act
        var result = await _controller.CreateLink(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedLink = createdResult.Value.Should().BeAssignableTo<GiftLinkDto>().Subject;
        returnedLink.Name.Should().Be("Birthday 2024");
    }

    [Fact]
    public async Task GetLink_ReturnsOkWithLink()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var linkDto = new GiftLinkDto(
            linkId, Guid.NewGuid(), "Timmy", "token", "Test Link", null,
            GiftLinkVisibility.Minimal, true, null, null, 0, null, null, null,
            DateTime.UtcNow, DateTime.UtcNow, "https://test.com/gift/token"
        );

        _mockGiftLinkService
            .Setup(s => s.GetLinkByIdAsync(linkId))
            .ReturnsAsync(linkDto);

        // Act
        var result = await _controller.GetLink(linkId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLink = okResult.Value.Should().BeAssignableTo<GiftLinkDto>().Subject;
        returnedLink.Id.Should().Be(linkId);
    }

    [Fact]
    public async Task GetLink_ReturnsNotFoundWhenLinkNotFound()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        _mockGiftLinkService
            .Setup(s => s.GetLinkByIdAsync(linkId))
            .ReturnsAsync((GiftLinkDto?)null);

        // Act
        var result = await _controller.GetLink(linkId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateLink_ReturnsOkWithUpdatedLink()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var updatedLink = new GiftLinkDto(
            linkId, Guid.NewGuid(), "Timmy", "token", "Updated Name", "Updated Desc",
            GiftLinkVisibility.Full, true, null, null, 0, null, null, null,
            DateTime.UtcNow, DateTime.UtcNow, "https://test.com/gift/token"
        );

        _mockGiftLinkService
            .Setup(s => s.UpdateLinkAsync(linkId, It.IsAny<UpdateGiftLinkDto>()))
            .ReturnsAsync(updatedLink);

        var updateDto = new UpdateGiftLinkDto(Name: "Updated Name", Description: "Updated Desc");

        // Act
        var result = await _controller.UpdateLink(linkId, updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLink = okResult.Value.Should().BeAssignableTo<GiftLinkDto>().Subject;
        returnedLink.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeactivateLink_ReturnsNoContent()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        _mockGiftLinkService
            .Setup(s => s.DeactivateLinkAsync(linkId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeactivateLink(linkId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task RegenerateToken_ReturnsOkWithNewToken()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var updatedLink = new GiftLinkDto(
            linkId, Guid.NewGuid(), "Timmy", "new_token", "Test Link", null,
            GiftLinkVisibility.Minimal, true, null, null, 0, null, null, null,
            DateTime.UtcNow, DateTime.UtcNow, "https://test.com/gift/new_token"
        );

        _mockGiftLinkService
            .Setup(s => s.RegenerateTokenAsync(linkId))
            .ReturnsAsync(updatedLink);

        // Act
        var result = await _controller.RegenerateToken(linkId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedLink = okResult.Value.Should().BeAssignableTo<GiftLinkDto>().Subject;
        returnedLink.Token.Should().Be("new_token");
    }

    [Fact]
    public async Task GetLinkStats_ReturnsOkWithStats()
    {
        // Arrange
        var linkId = Guid.NewGuid();
        var stats = new GiftLinkStatsDto(
            linkId, 10, 2, 7, 1, 500m, DateTime.UtcNow
        );

        _mockGiftLinkService
            .Setup(s => s.GetLinkStatsAsync(linkId))
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetLinkStats(linkId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedStats = okResult.Value.Should().BeAssignableTo<GiftLinkStatsDto>().Subject;
        returnedStats.TotalGifts.Should().Be(10);
        returnedStats.TotalAmountReceived.Should().Be(500m);
    }
}
