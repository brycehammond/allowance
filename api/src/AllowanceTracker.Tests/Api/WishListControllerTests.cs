using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

// Alias for CreateWishListItemRequest from controller
using CreateWishListItemRequest = AllowanceTracker.Api.V1.CreateWishListItemRequest;

namespace AllowanceTracker.Tests.Api;

public class WishListControllerTests
{
    private readonly Mock<IWishListService> _mockWishListService;
    private readonly WishListController _controller;

    public WishListControllerTests()
    {
        _mockWishListService = new Mock<IWishListService>();
        _controller = new WishListController(_mockWishListService.Object);
    }

    [Fact]
    public async Task GetChildWishList_ReturnsOkWithWishListItems()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var wishListItems = new List<WishListItemDto>
        {
            new(Guid.NewGuid(), childId, "Toy 1", 20m, null, null, false, null, DateTime.UtcNow, true),
            new(Guid.NewGuid(), childId, "Toy 2", 50m, null, null, false, null, DateTime.UtcNow, false)
        };

        _mockWishListService
            .Setup(x => x.GetChildWishListAsync(childId))
            .ReturnsAsync(wishListItems);

        // Act
        var result = await _controller.GetChildWishList(childId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var items = okResult.Value.Should().BeAssignableTo<List<WishListItemDto>>().Subject;
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetWishListItem_ReturnsOkWithItem()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var item = new WishListItemDto(
            itemId,
            childId,
            "Nintendo Switch",
            299.99m,
            "https://example.com",
            "Want this!",
            false,
            null,
            DateTime.UtcNow,
            false);

        _mockWishListService
            .Setup(x => x.GetWishListItemAsync(itemId))
            .ReturnsAsync(item);

        // Act
        var result = await _controller.GetWishListItem(childId, itemId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedItem = okResult.Value.Should().BeAssignableTo<WishListItemDto>().Subject;
        returnedItem.Name.Should().Be("Nintendo Switch");
    }

    [Fact]
    public async Task GetWishListItem_ReturnsNotFound_WhenItemNotFound()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        _mockWishListService
            .Setup(x => x.GetWishListItemAsync(itemId))
            .ReturnsAsync((WishListItemDto?)null);

        // Act
        var result = await _controller.GetWishListItem(childId, itemId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateWishListItem_ReturnsCreatedWithItem()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var request = new CreateWishListItemRequest(
            "PlayStation 5",
            499.99m,
            "https://example.com/ps5",
            "Saving for this");

        var createdItem = new WishListItem
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            Name = "PlayStation 5",
            Price = 499.99m,
            Url = "https://example.com/ps5",
            Notes = "Saving for this"
        };

        _mockWishListService
            .Setup(x => x.CreateWishListItemAsync(It.IsAny<CreateWishListItemDto>()))
            .ReturnsAsync(createdItem);

        // Act
        var result = await _controller.CreateWishListItem(childId, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(WishListController.GetWishListItem));
        createdResult.RouteValues!["id"].Should().Be(createdItem.Id);

        var returnedItem = createdResult.Value.Should().BeAssignableTo<WishListItem>().Subject;
        returnedItem.Name.Should().Be("PlayStation 5");
    }

    [Fact]
    public async Task UpdateWishListItem_ReturnsOkWithUpdatedItem()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var updateDto = new UpdateWishListItemDto("Updated Name", 29.99m, null, "Updated notes");
        var updatedItem = new WishListItem
        {
            Id = itemId,
            Name = "Updated Name",
            Price = 29.99m,
            Notes = "Updated notes"
        };

        _mockWishListService
            .Setup(x => x.UpdateWishListItemAsync(itemId, updateDto))
            .ReturnsAsync(updatedItem);

        // Act
        var result = await _controller.UpdateWishListItem(childId, itemId, updateDto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedItem = okResult.Value.Should().BeAssignableTo<WishListItem>().Subject;
        returnedItem.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateWishListItem_ReturnsNotFound_WhenItemNotFound()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var updateDto = new UpdateWishListItemDto("Name", 10m, null, null);

        _mockWishListService
            .Setup(x => x.UpdateWishListItemAsync(itemId, updateDto))
            .ReturnsAsync((WishListItem?)null);

        // Act
        var result = await _controller.UpdateWishListItem(childId, itemId, updateDto);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteWishListItem_ReturnsNoContent()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        _mockWishListService
            .Setup(x => x.DeleteWishListItemAsync(itemId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteWishListItem(childId, itemId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteWishListItem_ReturnsNotFound_WhenItemNotFound()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        _mockWishListService
            .Setup(x => x.DeleteWishListItemAsync(itemId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteWishListItem(childId, itemId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkAsPurchased_ReturnsOkWithUpdatedItem()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var purchasedItem = new WishListItem
        {
            Id = itemId,
            Name = "Purchased Item",
            Price = 25m,
            IsPurchased = true,
            PurchasedAt = DateTime.UtcNow
        };

        _mockWishListService
            .Setup(x => x.MarkAsPurchasedAsync(itemId))
            .ReturnsAsync(purchasedItem);

        // Act
        var result = await _controller.MarkAsPurchased(childId, itemId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedItem = okResult.Value.Should().BeAssignableTo<WishListItem>().Subject;
        returnedItem.IsPurchased.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsPurchased_ReturnsNotFound_WhenItemNotFound()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        _mockWishListService
            .Setup(x => x.MarkAsPurchasedAsync(itemId))
            .ReturnsAsync((WishListItem?)null);

        // Act
        var result = await _controller.MarkAsPurchased(childId, itemId);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task MarkAsUnpurchased_ReturnsOkWithUpdatedItem()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var unpurchasedItem = new WishListItem
        {
            Id = itemId,
            Name = "Unpurchased Item",
            Price = 25m,
            IsPurchased = false,
            PurchasedAt = null
        };

        _mockWishListService
            .Setup(x => x.MarkAsUnpurchasedAsync(itemId))
            .ReturnsAsync(unpurchasedItem);

        // Act
        var result = await _controller.MarkAsUnpurchased(childId, itemId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedItem = okResult.Value.Should().BeAssignableTo<WishListItem>().Subject;
        returnedItem.IsPurchased.Should().BeFalse();
    }
}
