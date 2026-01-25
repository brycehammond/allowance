using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Api;

public class GiftsControllerTests
{
    private readonly Mock<IGiftService> _mockGiftService;
    private readonly Mock<IThankYouNoteService> _mockThankYouNoteService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly GiftsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testFamilyId = Guid.NewGuid();
    private readonly Guid _testChildId = Guid.NewGuid();

    public GiftsControllerTests()
    {
        _mockGiftService = new Mock<IGiftService>();
        _mockThankYouNoteService = new Mock<IThankYouNoteService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _mockCurrentUserService.Setup(u => u.UserId).Returns(_testUserId);
        _mockCurrentUserService.Setup(u => u.FamilyId).Returns(_testFamilyId);
        _mockCurrentUserService.Setup(u => u.IsParent).Returns(true);

        _controller = new GiftsController(
            _mockGiftService.Object,
            _mockThankYouNoteService.Object,
            _mockCurrentUserService.Object);
    }

    [Fact]
    public async Task GetPortalData_ReturnsOkWithData()
    {
        // Arrange
        var portalData = new GiftPortalDataDto(
            "Timmy", null, 5m, 100m, GiftOccasion.Birthday, GiftLinkVisibility.Full,
            new List<PortalSavingsGoalDto>()
        );

        _mockGiftService
            .Setup(s => s.GetPortalDataAsync("valid_token"))
            .ReturnsAsync(portalData);

        // Act
        var result = await _controller.GetPortalData("valid_token");

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<GiftPortalDataDto>().Subject;
        data.ChildFirstName.Should().Be("Timmy");
    }

    [Fact]
    public async Task SubmitGift_ReturnsOkWithResult()
    {
        // Arrange
        var giftId = Guid.NewGuid();
        var submissionResult = new GiftSubmissionResultDto(
            giftId, "Timmy", 50m, "Thank you for your gift!"
        );

        _mockGiftService
            .Setup(s => s.SubmitGiftAsync("valid_token", It.IsAny<SubmitGiftDto>()))
            .ReturnsAsync(submissionResult);

        var request = new SubmitGiftRequest
        {
            GiverName = "Grandma",
            GiverEmail = "grandma@test.com",
            Amount = 50m,
            Occasion = GiftOccasion.Birthday,
            Message = "Happy Birthday!"
        };

        // Act
        var result = await _controller.SubmitGift("valid_token", request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var data = okResult.Value.Should().BeAssignableTo<GiftSubmissionResultDto>().Subject;
        data.Amount.Should().Be(50m);
    }

    [Fact]
    public async Task GetPendingGifts_ReturnsOkWithGifts()
    {
        // Arrange
        var gifts = new List<GiftDto>
        {
            new(Guid.NewGuid(), _testChildId, "Timmy", "Grandma", "grandma@test.com", null,
                50m, GiftOccasion.Birthday, null, "Happy Birthday!", GiftStatus.Pending,
                null, null, null, null, null, null, DateTime.UtcNow, false)
        };

        _mockGiftService
            .Setup(s => s.GetPendingGiftsAsync(_testFamilyId))
            .ReturnsAsync(gifts);

        // Act
        var result = await _controller.GetPendingGifts();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGifts = okResult.Value.Should().BeAssignableTo<List<GiftDto>>().Subject;
        returnedGifts.Should().HaveCount(1);
    }

    [Fact]
    public async Task ApproveGift_ReturnsOkWithApprovedGift()
    {
        // Arrange
        var giftId = Guid.NewGuid();
        var approvedGift = new GiftDto(
            giftId, _testChildId, "Timmy", "Grandma", "grandma@test.com", null,
            50m, GiftOccasion.Birthday, null, null, GiftStatus.Approved,
            null, _testUserId, DateTime.UtcNow, null, null, null, DateTime.UtcNow, false
        );

        _mockGiftService
            .Setup(s => s.ApproveGiftAsync(giftId, It.IsAny<ApproveGiftDto>(), _testUserId))
            .ReturnsAsync(approvedGift);

        var request = new ApproveGiftRequest();

        // Act
        var result = await _controller.ApproveGift(giftId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var gift = okResult.Value.Should().BeAssignableTo<GiftDto>().Subject;
        gift.Status.Should().Be(GiftStatus.Approved);
    }

    [Fact]
    public async Task RejectGift_ReturnsOkWithRejectedGift()
    {
        // Arrange
        var giftId = Guid.NewGuid();
        var rejectedGift = new GiftDto(
            giftId, _testChildId, "Timmy", "Unknown", null, null,
            1000m, GiftOccasion.Other, null, null, GiftStatus.Rejected,
            "Unknown sender", _testUserId, DateTime.UtcNow, null, null, null, DateTime.UtcNow, false
        );

        _mockGiftService
            .Setup(s => s.RejectGiftAsync(giftId, It.IsAny<RejectGiftDto>(), _testUserId))
            .ReturnsAsync(rejectedGift);

        var request = new RejectGiftRequest { Reason = "Unknown sender" };

        // Act
        var result = await _controller.RejectGift(giftId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var gift = okResult.Value.Should().BeAssignableTo<GiftDto>().Subject;
        gift.Status.Should().Be(GiftStatus.Rejected);
        gift.RejectionReason.Should().Be("Unknown sender");
    }

    [Fact]
    public async Task CreateThankYouNote_ReturnsCreatedWithNote()
    {
        // Arrange
        var giftId = Guid.NewGuid();
        var note = new ThankYouNoteDto(
            Guid.NewGuid(), giftId, _testChildId, "Timmy", "Grandma",
            "Thank you so much!", null, false, null, DateTime.UtcNow, DateTime.UtcNow
        );

        _mockCurrentUserService.Setup(u => u.IsParent).Returns(false);
        _mockCurrentUserService.Setup(u => u.ChildId).Returns(_testChildId);

        _mockThankYouNoteService
            .Setup(s => s.CreateNoteAsync(giftId, It.IsAny<CreateThankYouNoteDto>(), _testChildId))
            .ReturnsAsync(note);

        var request = new CreateThankYouNoteRequest { Message = "Thank you so much!" };

        // Act
        var result = await _controller.CreateThankYouNote(giftId, request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var returnedNote = createdResult.Value.Should().BeAssignableTo<ThankYouNoteDto>().Subject;
        returnedNote.Message.Should().Be("Thank you so much!");
    }

    [Fact]
    public async Task SendThankYouNote_ReturnsOkWithSentNote()
    {
        // Arrange
        var giftId = Guid.NewGuid();
        var note = new ThankYouNoteDto(
            Guid.NewGuid(), giftId, _testChildId, "Timmy", "Grandma",
            "Thank you!", null, true, DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow
        );

        _mockCurrentUserService.Setup(u => u.IsParent).Returns(false);
        _mockCurrentUserService.Setup(u => u.ChildId).Returns(_testChildId);

        _mockThankYouNoteService
            .Setup(s => s.SendNoteAsync(giftId, _testChildId))
            .ReturnsAsync(note);

        // Act
        var result = await _controller.SendThankYouNote(giftId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedNote = okResult.Value.Should().BeAssignableTo<ThankYouNoteDto>().Subject;
        returnedNote.IsSent.Should().BeTrue();
    }

    [Fact]
    public async Task GetPendingThankYous_ReturnsOkWithPendingList()
    {
        // Arrange
        var pendingList = new List<PendingThankYouDto>
        {
            new(Guid.NewGuid(), "Grandma", "Grandmother", 50m, GiftOccasion.Birthday,
                null, DateTime.UtcNow.AddDays(-2), 2, false)
        };

        _mockCurrentUserService.Setup(u => u.IsParent).Returns(false);
        _mockCurrentUserService.Setup(u => u.ChildId).Returns(_testChildId);

        _mockThankYouNoteService
            .Setup(s => s.GetPendingThankYousAsync(_testChildId))
            .ReturnsAsync(pendingList);

        // Act
        var result = await _controller.GetPendingThankYous();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returned = okResult.Value.Should().BeAssignableTo<List<PendingThankYouDto>>().Subject;
        returned.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetChildGifts_ReturnsOkWithGifts()
    {
        // Arrange
        var gifts = new List<GiftDto>
        {
            new(Guid.NewGuid(), _testChildId, "Timmy", "Grandma", null, null,
                50m, GiftOccasion.Birthday, null, null, GiftStatus.Approved,
                null, null, null, null, null, null, DateTime.UtcNow, true)
        };

        _mockGiftService
            .Setup(s => s.GetChildGiftsAsync(_testChildId))
            .ReturnsAsync(gifts);

        // Act
        var result = await _controller.GetChildGifts(_testChildId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGifts = okResult.Value.Should().BeAssignableTo<List<GiftDto>>().Subject;
        returnedGifts.Should().HaveCount(1);
    }
}
