using AllowanceTracker.Data;
using AllowanceTracker.DTOs.Gifting;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AllowanceTracker.Tests.Services;

public class ThankYouNoteServiceTests : IDisposable
{
    private readonly AllowanceContext _context;
    private readonly ThankYouNoteService _service;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Family _testFamily;
    private readonly ApplicationUser _testParent;
    private readonly Child _testChild;
    private readonly ApplicationUser _testChildUser;
    private readonly GiftLink _testGiftLink;
    private readonly Gift _approvedGift;

    public ThankYouNoteServiceTests()
    {
        var options = new DbContextOptionsBuilder<AllowanceContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new AllowanceContext(options);
        _mockEmailService = new Mock<IEmailService>();

        _service = new ThankYouNoteService(_context, _mockEmailService.Object);

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

        _testChildUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "child@test.com",
            UserName = "child@test.com",
            FirstName = "Timmy",
            LastName = "Test",
            Role = UserRole.Child,
            FamilyId = _testFamily.Id
        };
        _testChild = new Child
        {
            Id = Guid.NewGuid(),
            UserId = _testChildUser.Id,
            FamilyId = _testFamily.Id,
            CurrentBalance = 100m,
            WeeklyAllowance = 10m
        };

        _testGiftLink = new GiftLink
        {
            Id = Guid.NewGuid(),
            ChildId = _testChild.Id,
            FamilyId = _testFamily.Id,
            CreatedById = _testParent.Id,
            Token = "test_token",
            Name = "Test Link",
            IsActive = true
        };

        _approvedGift = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Grandma Betty",
            GiverEmail = "grandma@test.com",
            Amount = 50m,
            Occasion = GiftOccasion.Birthday,
            Status = GiftStatus.Approved,
            ProcessedAt = DateTime.UtcNow.AddDays(-2)
        };

        _context.Families.Add(_testFamily);
        _context.Users.Add(_testParent);
        _context.Users.Add(_testChildUser);
        _context.Children.Add(_testChild);
        _context.GiftLinks.Add(_testGiftLink);
        _context.Gifts.Add(_approvedGift);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetPendingThankYous_ReturnsApprovedGiftsWithoutNotes()
    {
        // Arrange
        var giftWithNote = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Uncle Bob",
            Amount = 25m,
            Occasion = GiftOccasion.JustBecause,
            Status = GiftStatus.Approved,
            ProcessedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Gifts.Add(giftWithNote);

        var note = new ThankYouNote
        {
            Id = Guid.NewGuid(),
            GiftId = giftWithNote.Id,
            ChildId = _testChild.Id,
            Message = "Thank you!",
            IsSent = true
        };
        _context.ThankYouNotes.Add(note);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetPendingThankYousAsync(_testChild.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].GiftId.Should().Be(_approvedGift.Id);
        result[0].GiverName.Should().Be("Grandma Betty");
        result[0].HasNote.Should().BeFalse();
    }

    [Fact]
    public async Task GetPendingThankYous_CalculatesDaysSinceReceived()
    {
        // Arrange - gift was approved 2 days ago (set in constructor)

        // Act
        var result = await _service.GetPendingThankYousAsync(_testChild.Id);

        // Assert
        result.Should().HaveCount(1);
        result[0].DaysSinceReceived.Should().BeGreaterThanOrEqualTo(2);
        result[0].DaysSinceReceived.Should().BeLessThan(4);
    }

    [Fact]
    public async Task CreateNote_CreatesForOwnGift()
    {
        // Arrange
        var dto = new CreateThankYouNoteDto("Thank you so much for the birthday money, Grandma!");

        // Act
        var result = await _service.CreateNoteAsync(_approvedGift.Id, dto, _testChild.Id);

        // Assert
        result.Should().NotBeNull();
        result.Message.Should().Be("Thank you so much for the birthday money, Grandma!");
        result.GiftId.Should().Be(_approvedGift.Id);
        result.ChildId.Should().Be(_testChild.Id);
        result.IsSent.Should().BeFalse();
    }

    [Fact]
    public async Task CreateNote_ThrowsForOtherChildsGift()
    {
        // Arrange
        var otherChildUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "other@test.com",
            UserName = "other@test.com",
            FirstName = "Other",
            LastName = "Child",
            Role = UserRole.Child,
            FamilyId = _testFamily.Id
        };
        var otherChild = new Child
        {
            Id = Guid.NewGuid(),
            UserId = otherChildUser.Id,
            FamilyId = _testFamily.Id,
            CurrentBalance = 0m,
            WeeklyAllowance = 5m
        };
        _context.Users.Add(otherChildUser);
        _context.Children.Add(otherChild);
        await _context.SaveChangesAsync();

        var dto = new CreateThankYouNoteDto("Trying to create note for someone else's gift");

        // Act
        Func<Task> act = async () => await _service.CreateNoteAsync(_approvedGift.Id, dto, otherChild.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not authorized*");
    }

    [Fact]
    public async Task UpdateNote_UpdatesUnsentNote()
    {
        // Arrange
        var note = new ThankYouNote
        {
            Id = Guid.NewGuid(),
            GiftId = _approvedGift.Id,
            ChildId = _testChild.Id,
            Message = "Original message",
            IsSent = false
        };
        _context.ThankYouNotes.Add(note);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateThankYouNoteDto(Message: "Updated thank you message!");

        // Act
        var result = await _service.UpdateNoteAsync(_approvedGift.Id, updateDto, _testChild.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Message.Should().Be("Updated thank you message!");
    }

    [Fact]
    public async Task UpdateNote_ThrowsForSentNote()
    {
        // Arrange
        var note = new ThankYouNote
        {
            Id = Guid.NewGuid(),
            GiftId = _approvedGift.Id,
            ChildId = _testChild.Id,
            Message = "Already sent",
            IsSent = true,
            SentAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.ThankYouNotes.Add(note);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateThankYouNoteDto(Message: "Trying to update sent note");

        // Act
        Func<Task> act = async () => await _service.UpdateNoteAsync(_approvedGift.Id, updateDto, _testChild.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been sent*");
    }

    [Fact]
    public async Task SendNote_SendsEmailToGiver()
    {
        // Arrange
        var note = new ThankYouNote
        {
            Id = Guid.NewGuid(),
            GiftId = _approvedGift.Id,
            ChildId = _testChild.Id,
            Message = "Thank you for the gift!",
            IsSent = false
        };
        _context.ThankYouNotes.Add(note);
        await _context.SaveChangesAsync();

        _mockEmailService
            .Setup(e => e.SendThankYouNoteEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SendNoteAsync(_approvedGift.Id, _testChild.Id);

        // Assert
        result.IsSent.Should().BeTrue();
        result.SentAt.Should().NotBeNull();

        _mockEmailService.Verify(e => e.SendThankYouNoteEmailAsync(
            "grandma@test.com",
            "Grandma Betty",
            "Timmy",
            "Thank you for the gift!",
            null
        ), Times.Once);
    }

    [Fact]
    public async Task SendNote_ThrowsWhenNoGiverEmail()
    {
        // Arrange
        var giftWithoutEmail = new Gift
        {
            Id = Guid.NewGuid(),
            GiftLinkId = _testGiftLink.Id,
            ChildId = _testChild.Id,
            GiverName = "Anonymous",
            GiverEmail = null, // No email
            Amount = 20m,
            Occasion = GiftOccasion.JustBecause,
            Status = GiftStatus.Approved
        };
        _context.Gifts.Add(giftWithoutEmail);

        var note = new ThankYouNote
        {
            Id = Guid.NewGuid(),
            GiftId = giftWithoutEmail.Id,
            ChildId = _testChild.Id,
            Message = "Thank you!",
            IsSent = false
        };
        _context.ThankYouNotes.Add(note);
        await _context.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.SendNoteAsync(giftWithoutEmail.Id, _testChild.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*no email*");
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
