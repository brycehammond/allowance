using AllowanceTracker.Api.V1;
using AllowanceTracker.DTOs;
using AllowanceTracker.Models;
using AllowanceTracker.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AllowanceTracker.Tests.Api;

public class SavingsGoalsControllerTests
{
    private readonly Mock<ISavingsGoalService> _mockService;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly SavingsGoalsController _controller;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _childId = Guid.NewGuid();
    private readonly Guid _goalId = Guid.NewGuid();

    public SavingsGoalsControllerTests()
    {
        _mockService = new Mock<ISavingsGoalService>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockCurrentUser.Setup(x => x.UserId).Returns(_userId);

        _controller = new SavingsGoalsController(_mockService.Object, _mockCurrentUser.Object);
    }

    private SavingsGoalDto CreateTestGoalDto(Guid? goalId = null, Guid? childId = null)
    {
        return new SavingsGoalDto(
            Id: goalId ?? _goalId,
            ChildId: childId ?? _childId,
            ChildName: "Test Child",
            Name: "Test Goal",
            Description: "A test goal",
            TargetAmount: 100m,
            CurrentAmount: 25m,
            RemainingAmount: 75m,
            ProgressPercentage: 25,
            ImageUrl: null,
            ProductUrl: null,
            Category: GoalCategory.Toy,
            CategoryName: "Toy",
            TargetDate: null,
            DaysRemaining: null,
            Status: GoalStatus.Active,
            StatusName: "Active",
            CompletedAt: null,
            PurchasedAt: null,
            Priority: 1,
            AutoTransferAmount: 0,
            AutoTransferType: AutoTransferType.None,
            HasMatchingRule: false,
            MatchingRule: null,
            HasActiveChallenge: false,
            ActiveChallenge: null,
            Milestones: new List<MilestoneDto>(),
            CreatedAt: DateTime.UtcNow
        );
    }

    #region GetChildGoals Tests

    [Fact]
    public async Task GetChildGoals_ReturnsOkWithGoals()
    {
        // Arrange
        var goals = new List<SavingsGoalDto> { CreateTestGoalDto(), CreateTestGoalDto(Guid.NewGuid()) };
        _mockService.Setup(x => x.GetChildGoalsAsync(_childId, null, false, _userId))
            .ReturnsAsync(goals);

        // Act
        var result = await _controller.GetChildGoals(_childId, null, false);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGoals = okResult.Value.Should().BeAssignableTo<List<SavingsGoalDto>>().Subject;
        returnedGoals.Should().HaveCount(2);
    }

    #endregion

    #region GetGoal Tests

    [Fact]
    public async Task GetGoal_ReturnsOkWithGoal()
    {
        // Arrange
        var goal = CreateTestGoalDto();
        _mockService.Setup(x => x.GetGoalByIdAsync(_goalId, _userId))
            .ReturnsAsync(goal);

        // Act
        var result = await _controller.GetGoal(_goalId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGoal = okResult.Value.Should().BeOfType<SavingsGoalDto>().Subject;
        returnedGoal.Id.Should().Be(_goalId);
    }

    [Fact]
    public async Task GetGoal_ReturnsNotFound_WhenGoalNotFound()
    {
        // Arrange
        _mockService.Setup(x => x.GetGoalByIdAsync(_goalId, _userId))
            .ReturnsAsync((SavingsGoalDto?)null);

        // Act
        var result = await _controller.GetGoal(_goalId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateGoal Tests

    [Fact]
    public async Task CreateGoal_ReturnsCreatedWithGoal()
    {
        // Arrange
        var dto = new CreateSavingsGoalDto(
            ChildId: _childId,
            Name: "New Goal",
            Description: null,
            TargetAmount: 100m,
            ImageUrl: null,
            ProductUrl: null,
            Category: GoalCategory.Toy,
            TargetDate: null,
            Priority: 1,
            AutoTransferAmount: 0,
            AutoTransferType: AutoTransferType.None
        );
        var createdGoal = CreateTestGoalDto();
        _mockService.Setup(x => x.CreateGoalAsync(dto, _userId))
            .ReturnsAsync(createdGoal);

        // Act
        var result = await _controller.CreateGoal(dto);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(SavingsGoalsController.GetGoal));
        var returnedGoal = createdResult.Value.Should().BeOfType<SavingsGoalDto>().Subject;
        returnedGoal.Should().NotBeNull();
    }

    #endregion

    #region UpdateGoal Tests

    [Fact]
    public async Task UpdateGoal_ReturnsOkWithUpdatedGoal()
    {
        // Arrange
        var dto = new UpdateSavingsGoalDto(
            Name: "Updated Name",
            Description: null,
            TargetAmount: null,
            ImageUrl: null,
            ProductUrl: null,
            Category: null,
            TargetDate: null,
            Priority: null,
            AutoTransferAmount: null,
            AutoTransferType: null
        );
        var updatedGoal = CreateTestGoalDto();
        _mockService.Setup(x => x.UpdateGoalAsync(_goalId, dto, _userId))
            .ReturnsAsync(updatedGoal);

        // Act
        var result = await _controller.UpdateGoal(_goalId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<SavingsGoalDto>();
    }

    #endregion

    #region DeleteGoal Tests

    [Fact]
    public async Task DeleteGoal_ReturnsNoContent()
    {
        // Arrange
        _mockService.Setup(x => x.CancelGoalAsync(_goalId, _userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteGoal(_goalId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion

    #region PauseGoal Tests

    [Fact]
    public async Task PauseGoal_ReturnsOkWithPausedGoal()
    {
        // Arrange
        var pausedGoal = CreateTestGoalDto() with { Status = GoalStatus.Paused, StatusName = "Paused" };
        _mockService.Setup(x => x.PauseGoalAsync(_goalId, _userId))
            .ReturnsAsync(pausedGoal);

        // Act
        var result = await _controller.PauseGoal(_goalId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGoal = okResult.Value.Should().BeOfType<SavingsGoalDto>().Subject;
        returnedGoal.Status.Should().Be(GoalStatus.Paused);
    }

    #endregion

    #region ResumeGoal Tests

    [Fact]
    public async Task ResumeGoal_ReturnsOkWithResumedGoal()
    {
        // Arrange
        var resumedGoal = CreateTestGoalDto() with { Status = GoalStatus.Active, StatusName = "Active" };
        _mockService.Setup(x => x.ResumeGoalAsync(_goalId, _userId))
            .ReturnsAsync(resumedGoal);

        // Act
        var result = await _controller.ResumeGoal(_goalId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGoal = okResult.Value.Should().BeOfType<SavingsGoalDto>().Subject;
        returnedGoal.Status.Should().Be(GoalStatus.Active);
    }

    #endregion

    #region Contribute Tests

    [Fact]
    public async Task Contribute_ReturnsOkWithProgressEvent()
    {
        // Arrange
        var dto = new ContributeToGoalDto(Amount: 25m, Description: null);
        var progressEvent = new GoalProgressEventDto(
            GoalId: _goalId,
            GoalName: "Test Goal",
            NewAmount: 50m,
            TargetAmount: 100m,
            ProgressPercentage: 50,
            MilestoneReached: null,
            IsCompleted: false,
            MatchAmountAdded: null
        );
        _mockService.Setup(x => x.ContributeAsync(_goalId, dto, _userId))
            .ReturnsAsync(progressEvent);

        // Act
        var result = await _controller.Contribute(_goalId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedEvent = okResult.Value.Should().BeOfType<GoalProgressEventDto>().Subject;
        returnedEvent.NewAmount.Should().Be(50m);
    }

    [Fact]
    public async Task Contribute_Returns400_WhenInsufficientBalance()
    {
        // Arrange
        var dto = new ContributeToGoalDto(Amount: 1000m, Description: null);
        _mockService.Setup(x => x.ContributeAsync(_goalId, dto, _userId))
            .ThrowsAsync(new InvalidOperationException("Insufficient balance"));

        // Act
        var result = await _controller.Contribute(_goalId, dto);

        // Assert
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
    }

    #endregion

    #region Withdraw Tests

    [Fact]
    public async Task Withdraw_ReturnsOkWithContribution()
    {
        // Arrange
        var dto = new WithdrawFromGoalDto(Amount: 10m, Reason: "Need funds");
        var contribution = new ContributionDto(
            Id: Guid.NewGuid(),
            Amount: -10m,
            Type: ContributionType.Withdrawal,
            TypeName: "Withdrawal",
            GoalBalanceAfter: 15m,
            Description: "Need funds",
            CreatedAt: DateTime.UtcNow,
            CreatedByName: "Test Parent"
        );
        _mockService.Setup(x => x.WithdrawAsync(_goalId, dto, _userId))
            .ReturnsAsync(contribution);

        // Act
        var result = await _controller.Withdraw(_goalId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedContribution = okResult.Value.Should().BeOfType<ContributionDto>().Subject;
        returnedContribution.Type.Should().Be(ContributionType.Withdrawal);
    }

    #endregion

    #region GetContributions Tests

    [Fact]
    public async Task GetContributions_ReturnsOkWithHistory()
    {
        // Arrange
        var contributions = new List<ContributionDto>
        {
            new(Guid.NewGuid(), 25m, ContributionType.ChildDeposit, "ChildDeposit", 25m, null, DateTime.UtcNow, "Test Child"),
            new(Guid.NewGuid(), 10m, ContributionType.ParentMatch, "ParentMatch", 35m, null, DateTime.UtcNow, null)
        };
        _mockService.Setup(x => x.GetContributionsAsync(_goalId, null, null, null))
            .ReturnsAsync(contributions);

        // Act
        var result = await _controller.GetContributions(_goalId, null, null, null);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedContributions = okResult.Value.Should().BeAssignableTo<List<ContributionDto>>().Subject;
        returnedContributions.Should().HaveCount(2);
    }

    #endregion

    #region MarkAsPurchased Tests

    [Fact]
    public async Task MarkAsPurchased_ReturnsOkWithPurchasedGoal()
    {
        // Arrange
        var dto = new MarkGoalPurchasedDto(Notes: null);
        var purchasedGoal = CreateTestGoalDto() with
        {
            Status = GoalStatus.Purchased,
            StatusName = "Purchased",
            PurchasedAt = DateTime.UtcNow
        };
        _mockService.Setup(x => x.MarkAsPurchasedAsync(_goalId, dto, _userId))
            .ReturnsAsync(purchasedGoal);

        // Act
        var result = await _controller.MarkAsPurchased(_goalId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGoal = okResult.Value.Should().BeOfType<SavingsGoalDto>().Subject;
        returnedGoal.Status.Should().Be(GoalStatus.Purchased);
    }

    #endregion

    #region Matching Rule Tests

    [Fact]
    public async Task CreateMatching_ReturnsOkWithRule()
    {
        // Arrange
        var dto = new CreateMatchingRuleDto(
            Type: MatchingType.RatioMatch,
            MatchRatio: 0.5m,
            MaxMatchAmount: 50m,
            ExpiresAt: null
        );
        var rule = new MatchingRuleDto(
            Id: Guid.NewGuid(),
            GoalId: _goalId,
            GoalName: "Test Goal",
            Type: MatchingType.RatioMatch,
            TypeDescription: "$1 for every $2 saved",
            MatchRatio: 0.5m,
            MaxMatchAmount: 50m,
            TotalMatchedAmount: 0m,
            RemainingMatchAmount: 50m,
            IsActive: true,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: null,
            CreatedByName: "Test Parent"
        );
        _mockService.Setup(x => x.CreateMatchingRuleAsync(_goalId, dto, _userId))
            .ReturnsAsync(rule);

        // Act
        var result = await _controller.CreateMatching(_goalId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedRule = okResult.Value.Should().BeOfType<MatchingRuleDto>().Subject;
        returnedRule.MatchRatio.Should().Be(0.5m);
    }

    [Fact]
    public async Task GetMatching_ReturnsOkWithRule()
    {
        // Arrange
        var rule = new MatchingRuleDto(
            Id: Guid.NewGuid(),
            GoalId: _goalId,
            GoalName: "Test Goal",
            Type: MatchingType.RatioMatch,
            TypeDescription: "$1 for every $2 saved",
            MatchRatio: 0.5m,
            MaxMatchAmount: null,
            TotalMatchedAmount: 10m,
            RemainingMatchAmount: null,
            IsActive: true,
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: null,
            CreatedByName: "Test Parent"
        );
        _mockService.Setup(x => x.GetMatchingRuleAsync(_goalId))
            .ReturnsAsync(rule);

        // Act
        var result = await _controller.GetMatching(_goalId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<MatchingRuleDto>();
    }

    #endregion

    #region Challenge Tests

    [Fact]
    public async Task CreateChallenge_ReturnsOkWithChallenge()
    {
        // Arrange
        var dto = new CreateGoalChallengeDto(
            TargetAmount: 50m,
            EndDate: DateTime.UtcNow.AddDays(30),
            BonusAmount: 10m,
            Description: null
        );
        var challenge = new GoalChallengeDto(
            Id: Guid.NewGuid(),
            GoalId: _goalId,
            GoalName: "Test Goal",
            TargetAmount: 50m,
            CurrentProgress: 25m,
            ProgressPercentage: 50,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddDays(30),
            DaysRemaining: 30,
            BonusAmount: 10m,
            Status: ChallengeStatus.Active,
            StatusName: "Active",
            CompletedAt: null,
            Description: null,
            CreatedAt: DateTime.UtcNow,
            CreatedByName: "Test Parent"
        );
        _mockService.Setup(x => x.CreateChallengeAsync(_goalId, dto, _userId))
            .ReturnsAsync(challenge);

        // Act
        var result = await _controller.CreateChallenge(_goalId, dto);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedChallenge = okResult.Value.Should().BeOfType<GoalChallengeDto>().Subject;
        returnedChallenge.TargetAmount.Should().Be(50m);
    }

    [Fact]
    public async Task GetChallenge_ReturnsOkWithChallenge()
    {
        // Arrange
        var challenge = new GoalChallengeDto(
            Id: Guid.NewGuid(),
            GoalId: _goalId,
            GoalName: "Test Goal",
            TargetAmount: 50m,
            CurrentProgress: 25m,
            ProgressPercentage: 50,
            StartDate: DateTime.UtcNow,
            EndDate: DateTime.UtcNow.AddDays(30),
            DaysRemaining: 30,
            BonusAmount: 10m,
            Status: ChallengeStatus.Active,
            StatusName: "Active",
            CompletedAt: null,
            Description: null,
            CreatedAt: DateTime.UtcNow,
            CreatedByName: "Test Parent"
        );
        _mockService.Setup(x => x.GetActiveChallengeAsync(_goalId))
            .ReturnsAsync(challenge);

        // Act
        var result = await _controller.GetChallenge(_goalId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeOfType<GoalChallengeDto>();
    }

    [Fact]
    public async Task CancelChallenge_ReturnsNoContent()
    {
        // Arrange
        _mockService.Setup(x => x.CancelChallengeAsync(_goalId, _userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CancelChallenge(_goalId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    #endregion
}
